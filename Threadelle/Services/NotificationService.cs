using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Hubs;
using Threadelle.Models;

namespace Threadelle.Services
{
    /// <summary>EF Core implementation of <see cref="INotificationService"/>.</summary>
    public class NotificationService : INotificationService
    {
        public const string AdminRole = "Admin";

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<AdminNotificationHub> _hub;

        public NotificationService(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IHubContext<AdminNotificationHub> hub)
        {
            _db = db;
            _userManager = userManager;
            _hub = hub;
        }

        public async Task NotifyUserAsync(string userId, string title, string message, NotificationType type, string? url = null)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            _db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                RelatedUrl = url,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        public async Task NotifyAdminsAsync(string title, string message, NotificationType type, string? url = null)
        {
            var admins = await _userManager.GetUsersInRoleAsync(AdminRole);
            if (admins.Count == 0) return;

            foreach (var admin in admins)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    RelatedUrl = url,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();

            // Push real-time update to all connected admin clients
            await _hub.Clients.Group("Admins").SendAsync("NewNotification");
        }

        public Task<int> GetUnreadCountAsync(string userId) =>
            _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

        public async Task<IReadOnlyList<Notification>> GetRecentAsync(string userId, int take = 8)
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task MarkReadAsync(string userId, int notificationId)
        {
            var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId);
            if (n != null && !n.IsRead)
            {
                n.IsRead = true;
                await _db.SaveChangesAsync();
            }
        }

        public async Task MarkAllReadAsync(string userId)
        {
            var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            if (unread.Count == 0) return;
            unread.ForEach(n => n.IsRead = true);
            await _db.SaveChangesAsync();
        }
    }
}
