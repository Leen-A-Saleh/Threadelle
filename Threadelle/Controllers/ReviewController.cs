using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Models;
using Threadelle.Services;

namespace Threadelle.Controllers
{
    [Authorize]
    public class ReviewController : BaseController
    {
        private readonly INotificationService _notifications;

        public ReviewController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            INotificationService notifications)
            : base(db, userManager)
        {
            _notifications = notifications;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId, string title, string body, int rating)
        {
            if (rating < 1 || rating > 5)
                return Json(new { ok = false, message = "Please select a rating." });

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(body))
                return Json(new { ok = false, message = "Please fill in the title and review." });

            var userId = CurrentUserId!;

            var alreadyReviewed = await Db.ProductReviews.AnyAsync(r => r.ProductId == productId && r.UserId == userId && !r.IsDeleted);
            if (alreadyReviewed)
                return Json(new { ok = false, message = "You have already reviewed this piece." });

            var hasPurchased = await Db.Orders.AnyAsync(o => o.UserId == userId && o.OrderItems.Any(oi => oi.ProductId == productId));
            if (!hasPurchased)
                return Json(new { ok = false, message = "You can only review pieces you have purchased." });

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                Title = title.Trim(),
                Body = body.Trim(),
                Rating = (byte)rating,
                IsApproved = false
            };

            Db.ProductReviews.Add(review);
            await Db.SaveChangesAsync();

            // Notify admins of new review pending approval
            var product = await Db.Products.FindAsync(productId);
            var reviewer = await UserManager.FindByIdAsync(userId);
            _ = _notifications.NotifyAdminsAsync(
                $"New review pending approval",
                $"{reviewer?.FullName ?? "A customer"} left a {rating}-star review for \"{product?.Name ?? "a product"}\".",
                NotificationType.NewReview,
                "/Admin/Products");

            return Json(new { ok = true, message = "Thank you for your review! It will appear after approval. 💕" });
        }
    }
}
