using Threadelle.Models;

namespace Threadelle.Services
{
    /// <summary>
    /// Central notification engine for ThreadElle. Every important action in the
    /// application routes through here so that customers and admins receive an
    /// in-app notification (bell + sidebar + unread badge) consistently.
    /// </summary>
    public interface INotificationService
    {
        // ── Writing ──────────────────────────────────────────────────────────
        /// <summary>Sends a notification to a single user.</summary>
        Task NotifyUserAsync(string userId, string title, string message, NotificationType type, string? url = null);

        /// <summary>Fans a notification out to every user in the Admin role.</summary>
        Task NotifyAdminsAsync(string title, string message, NotificationType type, string? url = null);

        // ── Reading ──────────────────────────────────────────────────────────
        Task<int> GetUnreadCountAsync(string userId);
        Task<IReadOnlyList<Notification>> GetRecentAsync(string userId, int take = 8);

        // ── State ────────────────────────────────────────────────────────────
        Task MarkReadAsync(string userId, int notificationId);
        Task MarkAllReadAsync(string userId);
    }
}
