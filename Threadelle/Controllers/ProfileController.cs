using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Models;
using Threadelle.Services;
using Threadelle.ViewModels;
using Treadelle.Interfaces;
using Threadelle.Helpers;

namespace Threadelle.Controllers
{
    [Authorize]
    public class ProfileController : BaseController
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IImageService _imageService;
        private readonly ICartService _cart;

        public ProfileController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IImageService imageService,
            ICartService cart)
            : base(db, userManager)
        {
            _signInManager = signInManager;
            _imageService = imageService;
            _cart = cart;
        }

        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId!;
            var user = await UserManager.GetUserAsync(User);

            var vm = new ProfileViewModel
            {
                User = user!,
                OrderCount = await Db.Orders.CountAsync(o => o.UserId == userId),
                WishlistCount = await Db.Wishlists.CountAsync(w => w.UserId == userId),
                CustomOrderCount = await Db.CustomOrders.CountAsync(c => c.UserId == userId),
                RecentOrders = await Db.Orders
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(3)
                    .ToListAsync()
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await UserManager.GetUserAsync(User);
            var vm = new EditProfileViewModel
            {
                FullName = user!.FullName,
                PhoneNumber = user.PhoneNumber,
                BirthDate = user.BirthDate?.ToDateTime(TimeOnly.MinValue),
                Gender = user.Gender
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model, IFormFile? profileImage)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await UserManager.GetUserAsync(User);
            user!.FullName = model.FullName.Trim();
            user.PhoneNumber = model.PhoneNumber;
            user.BirthDate = model.BirthDate.HasValue ? DateOnly.FromDateTime(model.BirthDate.Value) : null;
            user.Gender = model.Gender;

            if (profileImage != null && profileImage.Length > 0)
            {
                var fileName = await _imageService.UploadImage(profileImage);
                user.ProfileImageUrl = "/img/upload/" + fileName;
            }

            await UserManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user); // refresh the FullName claim

            Flash("Your details have been updated. 💕");
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Orders()
        {
            var userId = CurrentUserId!;
            var orders = await Db.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id, bool placed = false)
        {
            var userId = CurrentUserId!;
            var order = await Db.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product).ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();

            ViewBag.JustPlaced = placed;
            return View(order);
        }

        public async Task<IActionResult> CustomOrders()
        {
            var userId = CurrentUserId!;
            var orders = await Db.CustomOrders
                .Where(c => c.UserId == userId)
                .Include(c => c.Category)
                .Include(c => c.Images)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> CustomOrderDetails(int id)
        {
            var userId = CurrentUserId!;
            var order  = await Db.CustomOrders
                .Include(c => c.Category)
                .Include(c => c.Images)
                .Include(c => c.Colors).ThenInclude(cc => cc.Color)
                .Include(c => c.StatusHistory.OrderBy(h => h.ChangedAt))
                .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

            if (order == null) return NotFound();

            var vm = new Threadelle.ViewModels.CustomOrderDetailsViewModel
            {
                Order                 = order,
                CurrentStage          = order.Status.Friendly(),
                ActiveProductionStage = GetActiveProductionStage(order.Status)
            };

            ViewData["Title"] = order.Title ?? order.CustomOrderNumber;
            return View(vm);
        }

        private string? GetActiveProductionStage(CustomOrderStatus status)
        {
            return status switch
            {
                CustomOrderStatus.Designing => "Design",
                CustomOrderStatus.InProduction => "Materials",
                CustomOrderStatus.InProgress => "Crafting",
                CustomOrderStatus.QualityCheck => "Quality Check",
                CustomOrderStatus.Packaging => "Packaging",
                CustomOrderStatus.Completed => "Completed",
                _ => null
            };
        }

        /// <summary>Decline a quote the admin sent (AwaitingPayment → Rejected).</summary>
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineQuote(int id)
        {
            var userId = CurrentUserId!;
            var co     = await Db.CustomOrders.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
            if (co == null) return NotFound();

            if (co.Status != CustomOrderStatus.AwaitingPayment)
            {
                Flash("This quote cannot be declined right now.", "danger");
                return RedirectToAction("CustomOrders");
            }

            var oldStatus = co.Status;
            co.Status     = CustomOrderStatus.Rejected;
            co.UpdatedAt  = DateTime.UtcNow;

            Db.CustomOrderStatusHistories.Add(new CustomOrderStatusHistory
            {
                CustomOrderId = id,
                FromStatus    = oldStatus,
                ToStatus      = CustomOrderStatus.Rejected,
                Note          = "Customer declined the quote.",
                ChangedBy     = User.Identity?.Name ?? userId,
                ChangedAt     = DateTime.UtcNow
            });

            await Db.SaveChangesAsync();
            Flash("You've declined this quote. Feel free to start a new custom request anytime.", "info");
            return RedirectToAction("CustomOrders");
        }

        // Keep for backward compat — now delegates to CustomOrderController.Pay
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptQuote(int id)
            => RedirectToAction("Pay", "CustomOrder", new { id });

        // Keep for backward compat
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectQuote(int id)
            => await DeclineQuote(id);

        public async Task<IActionResult> Testimonials()
        {
            var userId = CurrentUserId!;
            var testimonials = await Db.Testimonials
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return View(testimonials);
        }

        public async Task<IActionResult> Notifications()
        {
            var userId = CurrentUserId!;
            var notifications = await Db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // mark them read on viewing the page
            var unread = notifications.Where(n => !n.IsRead).ToList();
            if (unread.Count > 0)
            {
                unread.ForEach(n => n.IsRead = true);
                await Db.SaveChangesAsync();
            }
            return View(notifications);
        }
    }
}
