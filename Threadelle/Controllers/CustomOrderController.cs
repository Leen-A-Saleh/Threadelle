using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Models;
using Threadelle.Services;
using Threadelle.ViewModels;
using Treadelle.Interfaces;

namespace Threadelle.Controllers
{
    public class CustomOrderController : BaseController
    {
        private static readonly string[] AllowedImageTypes = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxImageBytes = 5 * 1024 * 1024;

        private readonly IImageService _imageService;
        private readonly INotificationService _notifications;
        private readonly ICartService _cart;

        public CustomOrderController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IImageService imageService,
            INotificationService notifications,
            ICartService cart)
            : base(db, userManager)
        {
            _imageService  = imageService;
            _notifications = notifications;
            _cart          = cart;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new CustomOrderFormViewModel
            {
                Categories = await Db.Categories.Where(c => c.IsActive && !c.IsDeleted).OrderBy(c => c.DisplayOrder).ToListAsync(),
                Colors = await Db.Colors.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(),
                CompletedOrders = await Db.CustomOrders
                    .Include(c => c.User)
                    .Include(c => c.Images)
                    .Include(c => c.Category)
                    .Where(c => (c.Status == CustomOrderStatus.Completed || c.Status == CustomOrderStatus.Delivered) && c.ShowInGallery && !c.IsDeleted)
                    .OrderByDescending(c => c.GalleryPublishedAt)
                    .ToListAsync()
            };
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(30 * 1024 * 1024)]
        public async Task<IActionResult> Create(CustomOrderFormViewModel model)
        {
            ValidateImages(model.Images);

            if (!ModelState.IsValid)
            {
                model.Categories = await Db.Categories.Where(c => c.IsActive && !c.IsDeleted).OrderBy(c => c.DisplayOrder).ToListAsync();
                model.Colors = await Db.Colors.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
                return View(model);
            }

            var userId = CurrentUserId!;

            var order = new CustomOrder
            {
                CustomOrderNumber = await GenerateNumberAsync(),
                UserId = userId,
                CategoryId = model.CategoryId,
                Title = model.Title.Trim(),
                Budget = model.Budget,
                Deadline = model.Deadline,
                Status = CustomOrderStatus.Pending
            };

            // colours
            if (model.ColorIds != null)
            {
                foreach (var colorId in model.ColorIds.Distinct())
                    if (await Db.Colors.AnyAsync(c => c.Id == colorId))
                        order.Colors.Add(new CustomOrderColor { ColorId = colorId });
            }

            // inspiration images
            if (model.Images != null)
            {
                foreach (var image in model.Images.Where(i => i.Length > 0))
                {
                    var fileName = await _imageService.UploadImage(image);
                    order.Images.Add(new CustomOrderImage { ImageUrl = "/img/upload/" + fileName });
                }
            }

            Db.CustomOrders.Add(order);

            Db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = "Custom request received ✨",
                Message = $"We've received your custom request \"{order.Title}\" ({order.CustomOrderNumber}). Leen will review it personally and get back to you soon.",
                Type = NotificationType.CustomOrder,
                RelatedUrl = Url.Action("CustomOrders", "Profile")
            });

            await Db.SaveChangesAsync();

            // Notify admins about the new custom order
            var userName = (await UserManager.FindByIdAsync(userId))?.FullName ?? "A customer";
            _ = _notifications.NotifyAdminsAsync(
                $"New custom order: {order.CustomOrderNumber}",
                $"{userName} submitted a custom request for \"{order.Title}\".",
                NotificationType.NewCustomOrder,
                $"/Admin/CustomOrders/Details/{order.Id}");

            Flash("<strong>✨ Custom Request Sent</strong><br/>Thank you for sharing your idea with us. We will review it and send you a personalized quote soon.", "success");

            return RedirectToAction("Success", new { number = order.CustomOrderNumber });
        }

        [Authorize]
        public async Task<IActionResult> Success(string number)
        {
            var order = await Db.CustomOrders
                .FirstOrDefaultAsync(c => c.CustomOrderNumber == number && c.UserId == CurrentUserId);
            if (order == null) return RedirectToAction("Create");
            return View(order);
        }

        // ── PAY NOW ──────────────────────────────────────────────────────────
        /// <summary>
        /// Customer clicks "Pay Now" from their profile.
        /// Adds the approved custom-order product to cart and redirects to Checkout.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Pay(int id)
        {
            var userId = CurrentUserId!;
            var co = await Db.CustomOrders
                .Include(c => c.Images)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted);

            if (co == null) return NotFound();

            if (co.Status != CustomOrderStatus.AwaitingPayment || !co.EstimatedPrice.HasValue)
            {
                Flash("This custom order is not ready for payment.", "warning");
                return RedirectToAction("CustomOrders", "Profile");
            }

            // Find the product stub the admin created at approval time
            var slug    = $"custom-order-{co.CustomOrderNumber.ToLowerInvariant()}";
            var product = await Db.Products.FirstOrDefaultAsync(p => p.Slug == slug);

            if (product == null)
            {
                // Fallback: create product stub if admin somehow didn't
                var categoryId = co.CategoryId
                    ?? (await Db.Categories.FirstOrDefaultAsync(c => c.IsActive && !c.IsDeleted))?.Id
                    ?? 1;

                product = new Product
                {
                    CategoryId   = categoryId,
                    Name         = co.Title ?? $"Custom Order {co.CustomOrderNumber}",
                    Slug         = slug,
                    StoryTelling = "Lovingly handcrafted exclusively for you.",
                    WorkHours    = 1m,
                    HourRate     = co.EstimatedPrice.Value,
                    Quantity     = 1,
                    IsOnePiece   = true,
                    HasColorOptions = false,
                    IsFeatured   = false,
                    IsActive     = true
                };
                var idx = 0;
                foreach (var img in co.Images)
                    product.Images.Add(new ProductImage { ImageUrl = img.ImageUrl, IsPrimary = idx == 0, DisplayOrder = idx++ });

                Db.Products.Add(product);
                await Db.SaveChangesAsync();
            }

            // Add to cart (user can mix with other items or clear first — we keep it simple)
            await _cart.AddAsync(product.Id, null, 1);

            Flash("Your custom piece has been added to your bag. Complete your order below. 💕", "success");
            return RedirectToAction("Index", "Checkout");
        }

        // ── RESUBMIT (when ChangesRequested) ─────────────────────────────────
        [Authorize, HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = CurrentUserId!;
            var co = await Db.CustomOrders
                .Include(c => c.Category)
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted);

            if (co == null) return NotFound();

            if (co.Status != CustomOrderStatus.ChangesRequested)
            {
                Flash("This custom order cannot be edited right now.", "warning");
                return RedirectToAction("CustomOrders", "Profile");
            }

            ViewData["Title"] = "Update Your Request";
            return View(co);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string? additionalNote)
        {
            var userId = CurrentUserId!;
            var co = await Db.CustomOrders
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted);

            if (co == null) return NotFound();

            if (co.Status != CustomOrderStatus.ChangesRequested)
            {
                Flash("This custom order cannot be edited right now.", "warning");
                return RedirectToAction("CustomOrders", "Profile");
            }

            var oldStatus   = co.Status;
            co.Status       = CustomOrderStatus.Pending;
            co.UpdatedAt    = DateTime.UtcNow;
            co.AdminNotes   = null; // clear old note — admin will provide a new one

            Db.CustomOrderStatusHistories.Add(new CustomOrderStatusHistory
            {
                CustomOrderId = id,
                FromStatus    = oldStatus,
                ToStatus      = CustomOrderStatus.Pending,
                Note          = string.IsNullOrWhiteSpace(additionalNote)
                                ? "Customer updated and resubmitted."
                                : $"Customer resubmitted: {additionalNote.Trim()}",
                ChangedBy     = User.Identity?.Name ?? userId,
                ChangedAt     = DateTime.UtcNow
            });

            await Db.SaveChangesAsync();

            // Notify admins
            var userName = (await UserManager.FindByIdAsync(userId))?.FullName ?? "Customer";
            _ = _notifications.NotifyAdminsAsync(
                $"Custom order resubmitted: {co.CustomOrderNumber}",
                $"{userName} updated and resubmitted their custom request \"{co.Title}\".",
                NotificationType.NewCustomOrder,
                $"/Admin/CustomOrders/Details/{id}");

            Flash("<strong>✨ Request Updated!</strong><br/>Your updated request has been sent to Leen for review.", "success");
            return RedirectToAction("CustomOrders", "Profile");
        }

        private void ValidateImages(List<IFormFile>? images)
        {
            var real = images?.Where(i => i.Length > 0).ToList() ?? new List<IFormFile>();
            if (real.Count == 0)
            {
                ModelState.AddModelError(nameof(CustomOrderFormViewModel.Images), "Please upload at least one reference image so we can understand your idea.");
                return;
            }

            if (real.Count > 5)
                ModelState.AddModelError(nameof(CustomOrderFormViewModel.Images), "Please upload up to 5 photos.");

            foreach (var image in real)
            {
                var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!AllowedImageTypes.Contains(ext))
                    ModelState.AddModelError(nameof(CustomOrderFormViewModel.Images), $"\"{image.FileName}\" isn't a supported image type.");
                else if (image.Length > MaxImageBytes)
                    ModelState.AddModelError(nameof(CustomOrderFormViewModel.Images), $"\"{image.FileName}\" is larger than 5MB.");
            }
        }

        private async Task<string> GenerateNumberAsync()
        {
            string number;
            do
            {
                number = $"CO-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}";
            }
            while (await Db.CustomOrders.AnyAsync(c => c.CustomOrderNumber == number));
            return number;
        }
        [HttpGet]
        public async Task<IActionResult> GetGalleryImages(int id)
        {
            var order = await Db.CustomOrders
                .Include(o => o.Images)
                .FirstOrDefaultAsync(o => o.Id == id && o.ShowInGallery && !o.IsDeleted && (o.Status == CustomOrderStatus.Delivered || o.Status == CustomOrderStatus.Completed));

            if (order == null) return NotFound();

            var images = order.Images
                .Where(i => i.IsFinishedProduct && !i.IsDeleted)
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new GalleryImageVM
                {
                    Id = i.Id,
                    ThumbnailUrl = i.ImageUrl.Replace("/img/upload/", "/img/upload/thumb_"),
                    MediumUrl = i.ImageUrl.Replace("/img/upload/", "/img/upload/med_"),
                    OriginalUrl = i.ImageUrl
                }).ToList();

            // Fire and forget view count update (can be more sophisticated if needed)
            order.GalleryViewCount++;
            order.GalleryLastViewedAt = DateTime.UtcNow;
            await Db.SaveChangesAsync();

            return Json(new { success = true, images = images });
        }
    }
}
