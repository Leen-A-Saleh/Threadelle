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
    public class TestimonialController : BaseController
    {
        private readonly INotificationService _notifications;

        public TestimonialController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            INotificationService notifications)
            : base(db, userManager)
        {
            _notifications = notifications;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string message, byte rating, IFormFile? image)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Trim().Length < 10)
                return Json(new { ok = false, message = "Please share a little more — at least a sentence." });
            if (rating < 1 || rating > 5)
                return Json(new { ok = false, message = "Please choose a rating between 1 and 5." });

            var userId = CurrentUserId!;

            // keep it to one pending/active testimonial at a time to avoid spam
            var hasPending = await Db.Testimonials.AnyAsync(t => t.UserId == userId && !t.IsApproved);
            if (hasPending)
                return Json(new { ok = false, message = "You already have a note waiting to be approved — thank you!" });

            string? imageUrl = null;
            if (image != null && image.Length > 0)
            {
                if (image.Length > 5 * 1024 * 1024)
                    return Json(new { ok = false, message = "Image size cannot exceed 5MB." });
                var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
                    return Json(new { ok = false, message = "Only JPG, PNG, and WEBP images are allowed." });

                try
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "testimonials");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(image.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }
                    imageUrl = "/img/testimonials/" + uniqueFileName;
                }
                catch (Exception)
                {
                    // soft fail
                }
            }

            var testimonial = new Testimonial
            {
                UserId = userId,
                Message = message.Trim(),
                Rating = rating,
                ImageUrl = imageUrl,
                IsApproved = false
            };
            Db.Testimonials.Add(testimonial);
            await Db.SaveChangesAsync();

            // Notify admins of new testimonial pending approval
            var author = await UserManager.FindByIdAsync(userId);
            _ = _notifications.NotifyAdminsAsync(
                "New testimonial pending approval",
                $"{author?.FullName ?? "A customer"} submitted a {rating}-star testimonial.",
                NotificationType.NewTestimonial,
                "/Admin/Testimonials");

            var authorName = author?.FullName ?? "A customer";
            var initial = string.IsNullOrEmpty(authorName) ? "♥" : authorName.Substring(0, 1).ToUpper();

            return Json(new { 
                ok = true, 
                message = "Thank you for sharing your experience. Your review is pending approval.",
                testimonial = new {
                    id = testimonial.Id,
                    message = testimonial.Message,
                    rating = testimonial.Rating,
                    imageUrl = testimonial.ImageUrl,
                    authorName = authorName,
                    initial = initial
                }
            });
        }
    }
}
