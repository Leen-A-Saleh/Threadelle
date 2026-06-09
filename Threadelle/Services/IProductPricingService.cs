namespace Threadelle.Services
{
    /// <summary>
    /// Encapsulates a single product's effective price after the best
    /// active product-scope coupon has been applied.
    /// </summary>
    public class ProductPriceInfo
    {
        public decimal OriginalPrice  { get; set; }
        public decimal EffectivePrice { get; set; }
        public decimal DiscountAmount { get; set; }
        /// <summary>Human-readable badge, e.g. "15% OFF" or "Save 5.00 JOD".</summary>
        public string? DiscountBadge  { get; set; }
        public bool    HasDiscount    => DiscountAmount > 0;
    }

    /// <summary>
    /// Computes effective display prices for products by applying the highest valid
    /// product-scope coupon automatically (no code required by the customer).
    /// EntireOrder coupons are never considered here.
    /// </summary>
    public interface IProductPricingService
    {
        /// <summary>
        /// Returns a pricing map for every requested product ID.
        /// Missing IDs (not found in DB) are silently omitted.
        /// </summary>
        Task<Dictionary<int, ProductPriceInfo>> GetEffectivePricesAsync(IEnumerable<int> productIds);
    }
}
