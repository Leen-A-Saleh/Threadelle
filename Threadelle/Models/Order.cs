namespace Threadelle.Models
{
    public enum OrderPaymentStatus {Pending ,Paid,Refunded}
    public enum OrderStatus
    {
        Pending,
        Confirmed,
        InProgress,
        Ready,
        Delivered,
        Cancelled
    }
    public class Order
    {
        public int Id { get; set; }

        public string OrderNumber { get; set; } = string.Empty;
        public string? UserId { get; set; }

        public decimal SubTotal { get; set; }

        public decimal DiscountAmount { get; set; }
        
        public int? CouponId { get; set; }
        public Coupon? Coupon { get; set; }
        public string? CouponCode { get; set; }
        
        // Coupon Tracking Metadata
        public string? CouponType { get; set; }
        public string? DiscountType { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public string? DiscountedProducts { get; set; }
        public bool FreeShippingApplied { get; set; }
        public bool IsAutomaticPromotion { get; set; }

        public decimal TotalPrice { get; set; }

        public OrderStatus Status { get; set; }

        public OrderPaymentStatus PaymentStatus { get; set; }

        public string PaymentMethod { get; set; } = null!;

        public string? CustomerNote { get; set; }

        public string? TransactionId { get; set; }
        public string? AdminNote { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; } = null!;

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
