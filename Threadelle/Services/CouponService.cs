using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Models;

using Microsoft.Extensions.Logging;

namespace Threadelle.Services
{
    public class CouponService : ICouponService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CouponService> _logger;
        private readonly IProductPricingService _pricing;

        public CouponService(ApplicationDbContext db, ILogger<CouponService> logger, IProductPricingService pricing)
        {
            _db = db;
            _logger = logger;
            _pricing = pricing;
        }

        public async Task<CouponValidationResult> ValidateCouponAsync(string code, string? userId, Cart cart)
        {
            var result = await ValidateCouponBaseAsync(code, userId);
            if (!result.IsValid) return result;

            var coupon = result.Coupon!;

            var productIds = cart.CartItems.Select(ci => ci.ProductId).Distinct().ToList();
            var prices = await _pricing.GetEffectivePricesAsync(productIds);

            // Calculate subTotal to check MinOrderAmount (ignoring shipping/taxes)
            decimal subTotal = cart.CartItems.Sum(ci => (prices.GetValueOrDefault(ci.ProductId)?.EffectivePrice ?? ci.Product.Price) * ci.Quantity);

            if (coupon.MinOrderAmount.HasValue && subTotal < coupon.MinOrderAmount.Value)
            {
                _logger.LogWarning("Coupon validation failed: Code={Code}, SubTotal={SubTotal}, MinAmount={MinAmount}, Result=Failed", coupon.Code, subTotal, coupon.MinOrderAmount.Value);
                return new CouponValidationResult { IsValid = false, ErrorMessage = $"Minimum order amount of {coupon.MinOrderAmount.Value:C} required." };
            }
            
            _logger.LogInformation("Coupon validation passed: Code={Code}, SubTotal={SubTotal}, MinAmount={MinAmount}, Result=Success", coupon.Code, subTotal, coupon.MinOrderAmount.HasValue ? coupon.MinOrderAmount.Value : 0);

            var discount = CalculateDiscountForCart(coupon, cart, prices);
            if (discount <= 0 && coupon.DiscountType != DiscountType.FreeShipping)
            {
                return new CouponValidationResult { IsValid = false, ErrorMessage = "This coupon does not apply to any products in your cart, or a better automatic promotion is already applied." };
            }

            result.DiscountAmount = discount;
            return result;
        }

        public async Task<CouponValidationResult> ValidateCouponForOrderAsync(string code, string? userId, List<OrderItem> orderItems, decimal subTotal)
        {
            var result = await ValidateCouponBaseAsync(code, userId);
            if (!result.IsValid) return result;

            var coupon = result.Coupon!;

            if (coupon.MinOrderAmount.HasValue && subTotal < coupon.MinOrderAmount.Value)
            {
                _logger.LogWarning("Coupon validation failed: Code={Code}, SubTotal={SubTotal}, MinAmount={MinAmount}, Result=Failed", coupon.Code, subTotal, coupon.MinOrderAmount.Value);
                return new CouponValidationResult { IsValid = false, ErrorMessage = $"Minimum order amount of {coupon.MinOrderAmount.Value:C} required." };
            }

            _logger.LogInformation("Coupon validation passed: Code={Code}, SubTotal={SubTotal}, MinAmount={MinAmount}, Result=Success", coupon.Code, subTotal, coupon.MinOrderAmount.HasValue ? coupon.MinOrderAmount.Value : 0);

            var discount = CalculateDiscount(coupon, orderItems, subTotal);
            if (discount <= 0 && coupon.DiscountType != DiscountType.FreeShipping)
            {
                return new CouponValidationResult { IsValid = false, ErrorMessage = "This coupon does not apply to the selected products." };
            }

            result.DiscountAmount = discount;
            return result;
        }

        private async Task<CouponValidationResult> ValidateCouponBaseAsync(string code, string? userId)
        {
            if (string.IsNullOrWhiteSpace(code))
                return new CouponValidationResult { IsValid = false, ErrorMessage = "Coupon code is empty." };

            var coupon = await _db.Coupons
                .Include(c => c.CouponProducts)
                .Include(c => c.CouponCategories)
                .FirstOrDefaultAsync(c => c.Code == code && !c.IsDeleted);

            if (coupon == null)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "Invalid coupon code." };

            if (!coupon.IsActive)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "This coupon is currently inactive." };

            var now = DateTime.UtcNow;
            if (coupon.StartDate.HasValue && now < coupon.StartDate.Value)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "This coupon is not yet valid." };

            if (coupon.EndDate.HasValue && now > coupon.EndDate.Value)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "This coupon has expired." };

            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "This coupon's usage limit has been reached." };

            if (coupon.UsageLimitPerUser.HasValue && !string.IsNullOrEmpty(userId))
            {
                var userUsage = await _db.CouponUsages.CountAsync(cu => cu.CouponId == coupon.Id && cu.UserId == userId);
                if (userUsage >= coupon.UsageLimitPerUser.Value)
                {
                    return new CouponValidationResult { IsValid = false, ErrorMessage = "You have reached your usage limit for this coupon." };
                }
            }

            return new CouponValidationResult { IsValid = true, Coupon = coupon };
        }

        public decimal CalculateDiscount(Coupon coupon, List<OrderItem> orderItems, decimal subTotal)
        {
            decimal applicableAmount = 0;

            if (coupon.ApplicationType == CouponApplicationType.EntireOrder)
            {
                applicableAmount = subTotal;
            }
            else
            {
                foreach (var item in orderItems)
                {
                    bool applies = false;
                    if (coupon.ApplicationType == CouponApplicationType.SpecificProducts)
                    {
                        applies = coupon.CouponProducts.Any(cp => cp.ProductId == item.ProductId);
                    }
                    else if (coupon.ApplicationType == CouponApplicationType.SpecificCategories)
                    {
                        var productCategoryId = _db.Products.Where(p => p.Id == item.ProductId).Select(p => p.CategoryId).FirstOrDefault();
                        applies = coupon.CouponCategories.Any(cc => cc.CategoryId == productCategoryId);
                    }

                    if (applies)
                    {
                        applicableAmount += item.UnitPrice * item.Quantity;
                    }
                }
            }

            decimal discount = 0;
            switch (coupon.DiscountType)
            {
                case DiscountType.Percentage:
                    discount = applicableAmount * (coupon.DiscountValue / 100m);
                    break;
                case DiscountType.FixedAmount:
                    // If fixed amount, it applies to the applicable subtotal, up to the applicable subtotal
                    discount = Math.Min(applicableAmount, coupon.DiscountValue);
                    break;
                case DiscountType.FreeShipping:
                    discount = 0; // Free shipping is handled separately by nullifying shipping cost
                    break;
            }

            if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount.Value)
            {
                discount = coupon.MaxDiscountAmount.Value;
            }

            return Math.Round(discount, 2);
        }

        public decimal CalculateDiscountForCart(Coupon coupon, Cart cart, Dictionary<int, ProductPriceInfo>? prices = null)
        {
            var orderItems = new List<OrderItem>();
            
            foreach (var ci in cart.CartItems)
            {
                var priceInfo = prices?.GetValueOrDefault(ci.ProductId);
                
                // If a product already has an automatic promotion discount, and the manual coupon is product-specific, we skip it (prevent stacking)
                if (priceInfo != null && priceInfo.DiscountAmount > 0 && coupon.ApplicationType != CouponApplicationType.EntireOrder)
                {
                    continue; 
                }

                orderItems.Add(new OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = priceInfo?.EffectivePrice ?? ci.Product.Price 
                });
            }

            decimal subTotal = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity);
            return CalculateDiscount(coupon, orderItems, subTotal);
        }

        public async Task RecordCouponUsageAsync(string code, string userId, int orderId)
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(userId)) return;

            var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code && !c.IsDeleted);
            if (coupon != null)
            {
                coupon.UsedCount++;
                _db.CouponUsages.Add(new CouponUsage
                {
                    CouponId = coupon.Id,
                    UserId = userId,
                    OrderId = orderId,
                    UsedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
        }
    }
}
