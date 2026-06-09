namespace Threadelle.Models
{
    public class CouponProduct
    {
        public int CouponId { get; set; }
        public Coupon Coupon { get; set; } = null!;
        
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
