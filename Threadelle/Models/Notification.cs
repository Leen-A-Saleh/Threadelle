namespace Threadelle.Models
{
    public enum NotificationType
    {
        // ── User notifications (0–9) ──────────────────────────
        OrderUpdate = 0,
        CustomOrder = 1,
        Promotion = 2,
        System = 3,
        OrderStatusChanged = 4,
        PaymentSuccess = 5,

        // ── Admin notifications (10+) ─────────────────────────
        AdminCreate = 10,
        AdminUpdate = 11,
        AdminDelete = 12,
        NewOrder = 13,
        NewCustomOrder = 14,
        NewReview = 15,
        NewTestimonial = 16,
        NewUserRegistered = 17
    }
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; }

        public string? RelatedUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
