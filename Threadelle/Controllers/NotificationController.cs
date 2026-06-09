using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Helpers;
using Threadelle.Models;

namespace Threadelle.Controllers
{
    [Authorize]
    public class NotificationController : BaseController
    {
        public NotificationController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
            : base(db, userManager) { }

        [HttpGet]
        public async Task<IActionResult> Recent()
        {
            var userId = CurrentUserId!;
            var items = await Db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(8)
                .ToListAsync();

            var unread = await Db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

            return Json(new
            {
                unread,
                items = items.Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.IsRead,
                    url = n.RelatedUrl,
                    type = n.Type.ToString(),
                    timeAgo = TimeAgo.From(n.CreatedAt)
                })
            });
        }

        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var userId = CurrentUserId!;
            return Json(new { unread = await Db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = CurrentUserId!;
            var n = await Db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (n != null && !n.IsRead)
            {
                n.IsRead = true;
                await Db.SaveChangesAsync();
            }
            return Json(new { ok = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = CurrentUserId!;
            await Db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
            return Json(new { ok = true });
        }
    }
}
