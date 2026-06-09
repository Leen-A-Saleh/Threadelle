using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Models;
using Threadelle.Services;
using Threadelle.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace Threadelle.Controllers
{
    public class CheckoutController : BaseController
    {
        private readonly ICartService            _cart;
        private readonly IStripeService          _stripe;
        private readonly ICouponService          _couponService;
        private readonly INotificationService    _notifications;
        private readonly IProductPricingService  _pricing;

        public CheckoutController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ICartService cart,
            IStripeService stripe,
            ICouponService couponService,
            INotificationService notifications,
            IProductPricingService pricing)
            : base(db, userManager)
        {
            _cart          = cart;
            _stripe        = stripe;
            _couponService = couponService;
            _notifications = notifications;
            _pricing       = pricing;
        }

        private async Task<CartViewModel> BuildCartVmAsync(Threadelle.Models.Cart? cart)
        {
            if (cart == null) return new CartViewModel();
            var ids     = cart.CartItems.Select(ci => ci.ProductId).Distinct();
            var pricing = await _pricing.GetEffectivePricesAsync(ids);
            return CartViewModel.FromCart(cart, pricing);
        }

        public async Task<IActionResult> Index()
        {
            var cart   = await _cart.GetCartAsync(includeItems: true);
            var cartVm = await BuildCartVmAsync(cart);
            if (cartVm.IsEmpty)
            {
                Flash("Your bag is empty — let's find something lovely first.", "info");
                return RedirectToAction("Index", "Collections");
            }

            var user = User.Identity?.IsAuthenticated == true ? await UserManager.GetUserAsync(User) : null;
            var vm = new CheckoutViewModel
            {
                Cart = cartVm,
                FullName = user?.FullName ?? "",
                Phone = user?.PhoneNumber ?? "",
                PaymentMethod = "Cash on Delivery"
            };

            // Pre-generate a Payment Intent for the total
            try
            {
                vm.StripePublishableKey = _stripe.PublishableKey;
                // If total is 0, we don't need a payment intent, but normally carts are > 0.
                if (cartVm.Total > 0)
                {
                    vm.StripeClientSecret = await _stripe.CreatePaymentIntentAsync(cartVm.Total);
                }
            }
            catch (Exception ex)
            {
                // If Stripe fails to load, we can still load the page and just let them use COD or show error later.
                ModelState.AddModelError("", "Stripe failed to load: " + ex.Message);
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var cart   = await _cart.GetCartAsync(includeItems: true);
            var cartVm = await BuildCartVmAsync(cart);

            if (cartVm.IsEmpty)
            {
                Flash("Your bag is empty.", "info");
                return RedirectToAction("Index", "Collections");
            }

            model.Cart = cartVm;

            if (!string.IsNullOrWhiteSpace(model.AppliedCouponCode))
            {
                var validation = await _couponService.ValidateCouponAsync(model.AppliedCouponCode, CurrentUserId, cart!);
                if (validation.IsValid)
                {
                    model.DiscountAmount = validation.DiscountAmount;
                }
                else
                {
                    model.AppliedCouponCode = null; // invalid, clear it
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            // Re-validate every line against the live catalogue before committing
            var productIds = cartVm.Lines.Select(l => l.ProductId).Distinct().ToList();
            var products = await Db.Products
                .Include(p => p.ProductColors)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var line in cartVm.Lines)
            {
                if (!products.TryGetValue(line.ProductId, out var prod) || !prod.IsActive || prod.IsDeleted)
                {
                    Flash($"“{line.Name}” is no longer available, so we removed it from your bag.", "info");
                    await _cart.RemoveAsync(line.CartItemId);
                    return RedirectToAction("Index", "Cart");
                }

                var available = prod.Quantity;
                if (line.ColorId.HasValue)
                {
                    var pc = prod.ProductColors.FirstOrDefault(x => x.ColorId == line.ColorId.Value);
                    if (pc != null) available = pc.Quantity;
                }
                if (prod.IsOnePiece) available = Math.Min(1, available);
                if (line.Quantity > available)
                {
                    Flash(available <= 0
                        ? $"“{line.Name}” just sold out. Please adjust your bag."
                        : $"Only {available} of “{line.Name}” remain. Please adjust your bag.", "info");
                    return RedirectToAction("Index", "Cart");
                }
            }

            var userId = CurrentUserId; // Null for guest checkout

            if (model.PaymentMethod == "Visa Card")
            {
                if (string.IsNullOrWhiteSpace(model.PaymentIntentId))
                {
                    ModelState.AddModelError("", "Payment verification failed. Please try again.");
                    return View(model);
                }

                // If not in simulation mode, we could verify the PaymentIntent status here via Stripe API.
                // For now, we trust the frontend confirmation, as it won't submit unless stripe.confirmPayment succeeds.
                
                var order = await CreateOrderFromCheckoutAsync(model, cart!.Id, userId, model.PaymentIntentId);
                Flash("Your handmade creation has found its home.", "success");
                return RedirectToAction("Confirmation", new { id = order.Id });
            }
            else // Cash on Delivery
            {
                var order = await CreateOrderFromCheckoutAsync(model, cart!.Id, userId, "COD");
                Flash("Your handmade creation has found its home.", "success");
                return RedirectToAction("Confirmation", new { id = order.Id });
            }
        }

        private async Task<Order> CreateOrderFromCheckoutAsync(CheckoutViewModel model, int cartId, string? userId, string transactionId)
        {
            var cart = await Db.Carts
                .Include(c => c.CartItems).ThenInclude(ci => ci.Product).ThenInclude(p => p.Category)
                .Include(c => c.CartItems).ThenInclude(ci => ci.Color)
                .FirstOrDefaultAsync(c => c.Id == cartId);
                
            if (cart == null) throw new Exception("Cart not found.");

            // Apply product-coupon discounts to cart prices
            var productIds = cart.CartItems.Select(ci => ci.ProductId).Distinct();
            var productPricing = await _pricing.GetEffectivePricesAsync(productIds);
            var cartVm = CartViewModel.FromCart(cart, productPricing);

            var order = new Order
            {
                OrderNumber = await GenerateOrderNumberAsync(),
                UserId = string.IsNullOrWhiteSpace(userId) ? null : userId,
                SubTotal = cartVm.SubTotal,
                DiscountAmount = 0m,
                TotalPrice = cartVm.Total,
                Status = transactionId == "COD" ? OrderStatus.Pending : OrderStatus.Confirmed,
                PaymentStatus = transactionId == "COD" ? OrderPaymentStatus.Pending : OrderPaymentStatus.Paid,
                PaymentMethod = model.PaymentMethod,
                CustomerNote = BuildNote(model),
                TransactionId = transactionId == "COD" ? null : transactionId
            };

            if (!string.IsNullOrWhiteSpace(model.AppliedCouponCode) && cart != null)
            {
                var validation = await _couponService.ValidateCouponAsync(model.AppliedCouponCode, userId, cart);
                if (validation.IsValid)
                {
                    var actualDiscount = validation.Coupon!.DiscountType == DiscountType.FreeShipping ? cartVm.Shipping : validation.DiscountAmount;

                    order.CouponId = validation.Coupon.Id;
                    order.CouponCode = validation.Coupon.Code;
                    order.DiscountAmount = actualDiscount;
                    order.TotalPrice -= order.DiscountAmount;
                    if (order.TotalPrice < 0) order.TotalPrice = 0;

                    // Coupon Tracking Metadata
                    order.CouponType = validation.Coupon.ApplicationType.ToString();
                    order.DiscountType = validation.Coupon.DiscountType.ToString();
                    order.DiscountPercentage = validation.Coupon.DiscountType == DiscountType.Percentage ? validation.Coupon.DiscountValue : null;
                    order.IsAutomaticPromotion = validation.Coupon.IsAutomaticPromotion;
                    order.FreeShippingApplied = validation.Coupon.DiscountType == DiscountType.FreeShipping;

                    if (validation.Coupon.ApplicationType == CouponApplicationType.SpecificProducts || validation.Coupon.ApplicationType == CouponApplicationType.SpecificCategories)
                    {
                        var applicableProductIds = cartVm.Lines.Select(l => l.ProductId).Distinct().ToList();
                        order.DiscountedProducts = string.Join(",", applicableProductIds);
                    }
                }
            }

            var cartProductIds = cartVm.Lines.Select(l => l.ProductId).Distinct().ToList();
            var products = await Db.Products.Include(p => p.ProductColors).Where(p => cartProductIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            foreach (var line in cartVm.Lines)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = line.ProductId,
                    ColorId = line.ColorId,
                    ProductName = line.Name,
                    ColorName = line.ColorName,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                });

                // Reduce stock now
                if (products.TryGetValue(line.ProductId, out var product))
                {
                    if (line.ColorId.HasValue)
                    {
                        var pc = product.ProductColors.FirstOrDefault(x => x.ColorId == line.ColorId.Value);
                        if (pc != null)
                        {
                            pc.Quantity = Math.Max(0, pc.Quantity - line.Quantity);
                            product.Quantity = product.ProductColors.Sum(c => c.Quantity);
                        }
                        else
                        {
                            product.Quantity = Math.Max(0, product.Quantity - line.Quantity);
                        }
                    }
                    else
                    {
                        product.Quantity = Math.Max(0, product.Quantity - line.Quantity);
                    }

                    if (product.IsOnePiece && product.Quantity == 0) product.IsActive = false;
                }
            }

            Db.Orders.Add(order);
            
            // Clear Cart explicitly from DB since we might be in a webhook without session
            if (cart.CartItems != null) Db.CartItems.RemoveRange(cart.CartItems);
            Db.Carts.Remove(cart);

            if (order.UserId != null)
            {
                Db.Notifications.Add(new Notification
                {
                    UserId = order.UserId,
                    Title = transactionId == "COD" ? "Order received 💌" : "Payment success 💖",
                    Message = transactionId == "COD" ? $"Thank you! Your order {order.OrderNumber} has been received and is being lovingly prepared." : $"Your payment of ${order.TotalPrice:0.00} for order {order.OrderNumber} was successful. We are now preparing your gift.",
                    Type = NotificationType.OrderUpdate,
                    RelatedUrl = Url.Action("OrderDetails", "Profile", new { id = order.Id }) // We don't have ID yet, but EF Core might assign it or we need to SaveChanges first.
                });
            }

            await Db.SaveChangesAsync(); 

            // Fix RelatedUrl if we need the ID
            if (order.UserId != null)
            {
                var notif = await Db.Notifications.OrderByDescending(n => n.Id).FirstOrDefaultAsync(n => n.UserId == order.UserId && n.Type == NotificationType.OrderUpdate);
                if (notif != null && notif.RelatedUrl != null && notif.RelatedUrl.EndsWith("/0"))
                {
                    notif.RelatedUrl = Url.Action("OrderDetails", "Profile", new { id = order.Id });
                    await Db.SaveChangesAsync();
                }
            }

            if (!string.IsNullOrWhiteSpace(model.AppliedCouponCode))
            {
                await _couponService.RecordCouponUsageAsync(model.AppliedCouponCode, userId ?? "", order.Id);
            }

            // ── If order contains a custom-order product, advance the custom order status ──
            foreach (var item in order.OrderItems)
            {
                var product = await Db.Products.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId && p.Slug != null && p.Slug.StartsWith("custom-order-"));
                if (product == null) continue;

                var customOrderNumber = product.Slug!.Replace("custom-order-", "").ToUpperInvariant().Replace("-", "");
                var customOrder = await Db.CustomOrders
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(co =>
                        co.CustomOrderNumber.Replace("-", "").ToUpper() == customOrderNumber
                        && co.Status == CustomOrderStatus.AwaitingPayment);

                if (customOrder != null)
                {
                    var prevStatus = customOrder.Status;
                    customOrder.Status    = CustomOrderStatus.InProduction;
                    customOrder.UpdatedAt = DateTime.UtcNow;

                    Db.CustomOrderStatusHistories.Add(new CustomOrderStatusHistory
                    {
                        CustomOrderId = customOrder.Id,
                        FromStatus    = prevStatus,
                        ToStatus      = CustomOrderStatus.InProduction,
                        Note          = $"Payment received — Order #{order.OrderNumber}. Production begins.",
                        ChangedBy     = "System",
                        ChangedAt     = DateTime.UtcNow
                    });

                    // Notify customer
                    Db.Notifications.Add(new Notification
                    {
                        UserId     = customOrder.UserId,
                        Title      = "🧶 Payment received — your custom piece is now being crafted!",
                        Message    = $"We've received your payment for \"{customOrder.Title ?? customOrder.CustomOrderNumber}\". Leen has started working on it!",
                        Type       = NotificationType.CustomOrder,
                        RelatedUrl = "/Profile/CustomOrders",
                        IsRead     = false,
                        CreatedAt  = DateTime.UtcNow
                    });

                    await Db.SaveChangesAsync();
                }
            }

            // Notify all admins about the new order
            _ = _notifications.NotifyAdminsAsync(
                $"New order received: {order.OrderNumber}",
                $"Order {order.OrderNumber} placed for {order.TotalPrice:C} via {order.PaymentMethod}.",
                NotificationType.NewOrder,
                $"/Admin/Orders/Details/{order.Id}");

            return order;
        }



        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await Db.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product).ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (order.UserId != null && CurrentUserId != null && order.UserId != CurrentUserId)
            {
                return Forbid();
            }

            return View(order);
        }

        private static string BuildNote(CheckoutViewModel m)
        {
            var note = $"Deliver to: {m.FullName}, {m.Address}, {m.City}. Phone: {m.Phone}.";
            if (!string.IsNullOrWhiteSpace(m.Note))
                note += $" Note: {m.Note}";
            return note;
        }

        private async Task<string> GenerateOrderNumberAsync()
        {
            string number;
            do
            {
                number = $"TE-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}";
            }
            while (await Db.Orders.AnyAsync(o => o.OrderNumber == number));
            return number;
        }

        [HttpPost]
        public async Task<IActionResult> ApplyCoupon([FromBody] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { success = false, message = "Please enter a promo code." });

            var cart = await _cart.GetCartAsync(includeItems: true);
            if (cart == null || !cart.CartItems.Any())
                return Json(new { success = false, message = "Your cart is empty." });

            var userId     = CurrentUserId;
            var validation = await _couponService.ValidateCouponAsync(code.Trim().ToUpperInvariant(), userId, cart);

            if (!validation.IsValid)
                return Json(new { success = false, message = validation.ErrorMessage });

            if (validation.Coupon!.IsAutomaticPromotion && validation.Coupon.ApplicationType != CouponApplicationType.EntireOrder)
                return Json(new
                {
                    success = false,
                    message = "This discount is automatically applied to product prices - no code needed!"
                });

            // Calculate cart total to find the shipping amount for free shipping
            var productIds = cart.CartItems.Select(ci => ci.ProductId).Distinct();
            var productPricing = await _pricing.GetEffectivePricesAsync(productIds);
            var cartVm = CartViewModel.FromCart(cart, productPricing);

            decimal actualDiscount = validation.Coupon.DiscountType == DiscountType.FreeShipping ? cartVm.Shipping : validation.DiscountAmount;

            return Json(new
            {
                success        = true,
                discountAmount = actualDiscount,
                couponCode     = validation.Coupon.Code,
                type           = validation.Coupon.DiscountType.ToString(),
                message        = $"Code <strong>{validation.Coupon.Code}</strong> applied — you save {actualDiscount:0.00} JOD!"
            });
        }
    }
}
