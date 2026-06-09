namespace Threadelle.Models
{
    public enum DiscountType { Percentage, FixedAmount, FreeShipping }
    public enum CouponApplicationType { EntireOrder, SpecificProducts, SpecificCategories }

    public class Coupon
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int? UsageLimit { get; set; }
        public int? UsageLimitPerUser { get; set; }
        public int UsedCount { get; set; } = 0;

        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }

        public bool IsActive { get; set; } = true;
        
        public CouponApplicationType ApplicationType { get; set; } = CouponApplicationType.EntireOrder;
        
        public bool IsAutomaticPromotion { get; set; } = false;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CouponProduct> CouponProducts { get; set; } = new List<CouponProduct>();
        public ICollection<CouponCategory> CouponCategories { get; set; } = new List<CouponCategory>();
        public ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
