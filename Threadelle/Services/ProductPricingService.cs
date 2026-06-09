using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Helpers;
using Threadelle.Models;

namespace Threadelle.Services
{
    public class ProductPricingService : IProductPricingService
    {
        private readonly ApplicationDbContext _db;

        public ProductPricingService(ApplicationDbContext db) => _db = db;

        public async Task<Dictionary<int, ProductPriceInfo>> GetEffectivePricesAsync(IEnumerable<int> productIds)
        {
            var ids = productIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, ProductPriceInfo>();

            // Load products with materials so CalculatePrice() is accurate
            var products = await _db.Products
                .Where(p => ids.Contains(p.Id))
                .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
                .ToListAsync();

            var productData = products.ToDictionary(
                p => p.Id,
                p => new { p.CategoryId, BasePrice = p.CalculatePrice() });

            // Load all active product-scope coupons (SpecificProducts OR SpecificCategories)
            var now = DateTime.UtcNow;
            var coupons = await _db.Coupons
                .Include(c => c.CouponProducts)
                .Include(c => c.CouponCategories)
                .Where(c => !c.IsDeleted
                         && c.IsActive
                         && c.ApplicationType != CouponApplicationType.EntireOrder
                         && c.DiscountType    != DiscountType.FreeShipping
                         && (c.StartDate == null || c.StartDate <= now)
                         && (c.EndDate   == null || c.EndDate   >= now)
                         && (c.UsageLimit == null || c.UsedCount < c.UsageLimit))
                .ToListAsync();

            var result = new Dictionary<int, ProductPriceInfo>();

            foreach (var id in ids)
            {
                if (!productData.TryGetValue(id, out var pd)) continue;

                var basePrice = pd.BasePrice;
                
                decimal bestAutoDiscount = 0;
                string? bestAutoBadge = null;
                
                decimal bestManualDiscount = 0;
                string? bestManualBadge = null;

                foreach (var coupon in coupons)
                {
                    bool applies = coupon.ApplicationType == CouponApplicationType.SpecificProducts
                        ? coupon.CouponProducts.Any(cp => cp.ProductId == id)
                        : coupon.CouponCategories.Any(cc => cc.CategoryId == pd.CategoryId);

                    if (!applies) continue;

                    var discount = coupon.DiscountType switch
                    {
                        DiscountType.Percentage  => Math.Round(basePrice * coupon.DiscountValue / 100m, 2),
                        DiscountType.FixedAmount => Math.Min(basePrice, coupon.DiscountValue),
                        _                        => 0m
                    };

                    if (coupon.MaxDiscountAmount.HasValue)
                        discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);

                    discount = Math.Round(discount, 2);

                    if (coupon.IsAutomaticPromotion)
                    {
                        if (discount > bestAutoDiscount)
                        {
                            bestAutoDiscount = discount;
                            bestAutoBadge = coupon.DiscountType == DiscountType.Percentage
                                ? $"{coupon.DiscountValue:0.##}% OFF"
                                : $"Save {discount:0.##} JOD";
                        }
                    }
                    else
                    {
                        if (discount > bestManualDiscount)
                        {
                            bestManualDiscount = discount;
                            bestManualBadge = coupon.DiscountType == DiscountType.Percentage
                                ? $"Save {coupon.DiscountValue:0.##}% with code {coupon.Code}"
                                : $"Save {discount:0.##} JOD with code {coupon.Code}";
                        }
                    }
                }

                if (bestAutoDiscount >= bestManualDiscount && bestAutoDiscount > 0)
                {
                    result[id] = new ProductPriceInfo
                    {
                        OriginalPrice  = basePrice,
                        DiscountAmount = bestAutoDiscount,
                        EffectivePrice = Math.Round(basePrice - bestAutoDiscount, 2),
                        DiscountBadge  = bestAutoBadge
                    };
                }
                else if (bestManualDiscount > bestAutoDiscount && bestManualDiscount > 0)
                {
                    result[id] = new ProductPriceInfo
                    {
                        OriginalPrice  = basePrice,
                        DiscountAmount = 0, // Not applied until checkout
                        EffectivePrice = basePrice,
                        DiscountBadge  = bestManualBadge
                    };
                }
                else
                {
                    result[id] = new ProductPriceInfo
                    {
                        OriginalPrice  = basePrice,
                        DiscountAmount = 0,
                        EffectivePrice = basePrice,
                        DiscountBadge  = null
                    };
                }
            }

            return result;
        }
    }
}
