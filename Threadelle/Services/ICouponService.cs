using Threadelle.Models;

namespace Threadelle.Services
{
    public class CouponValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Coupon? Coupon { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public interface ICouponService
    {
        Task<CouponValidationResult> ValidateCouponAsync(string code, string? userId, Cart cart);
        Task<CouponValidationResult> ValidateCouponForOrderAsync(string code, string? userId, List<OrderItem> orderItems, decimal subTotal);
        Task RecordCouponUsageAsync(string code, string userId, int orderId);
        decimal CalculateDiscount(Coupon coupon, List<OrderItem> orderItems, decimal subTotal);
        decimal CalculateDiscountForCart(Coupon coupon, Cart cart, Dictionary<int, ProductPriceInfo>? prices = null);
    }
}
