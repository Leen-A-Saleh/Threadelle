namespace Threadelle.Models
{
    public class CouponCategory
    {
        public int CouponId { get; set; }
        public Coupon Coupon { get; set; } = null!;
        
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }
}
