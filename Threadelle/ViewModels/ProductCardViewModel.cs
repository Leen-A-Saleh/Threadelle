using Threadelle.Helpers;
using Threadelle.Models;
using Threadelle.Services;

namespace Threadelle.ViewModels
{
    public class ProductCardViewModel
    {
        public int     Id           { get; set; }
        public string  Name         { get; set; } = string.Empty;
        public string  Slug         { get; set; } = string.Empty;
        public string  CategoryName { get; set; } = string.Empty;

        /// <summary>Effective (discounted) price — use this everywhere for display.</summary>
        public decimal Price         { get; set; }

        /// <summary>Original price before any coupon. Same as Price when no discount.</summary>
        public decimal OriginalPrice { get; set; }

        public decimal DiscountAmount { get; set; }
        public string? DiscountBadge  { get; set; }    // e.g. "15% OFF"
        public bool    HasDiscount    => DiscountAmount > 0;

        public string? ImageUrl    { get; set; }
        public string? Badge       { get; set; }
        public bool    IsWishlisted { get; set; }
        public bool    InStock      { get; set; }
        public bool    IsFeatured   { get; set; }
        public bool    IsOnePiece   { get; set; }

        // ── Factories ────────────────────────────────────────────────────────

        /// <summary>Build from a Product entity with optional pricing data.</summary>
        public static ProductCardViewModel FromProduct(
            Product p,
            bool isWishlisted          = false,
            ProductPriceInfo? pricing  = null)
        {
            var basePrice     = p.CalculatePrice();
            var effectivePrice = pricing?.EffectivePrice ?? basePrice;

            return new ProductCardViewModel
            {
                Id            = p.Id,
                Name          = p.Name,
                Slug          = p.Slug,
                CategoryName  = p.Category?.Name ?? "",
                Price         = effectivePrice,
                OriginalPrice = basePrice,
                DiscountAmount= pricing?.DiscountAmount ?? 0m,
                DiscountBadge = pricing?.DiscountBadge,
                ImageUrl      = p.PrimaryImageUrl(),
                Badge         = p.IsFeatured ? "Featured" : (p.IsOnePiece ? "One of a kind" : null),
                IsWishlisted  = isWishlisted,
                InStock       = p.IsActive && (p.IsOnePiece ? p.Quantity >= 1 : p.Quantity > 0),
                IsFeatured    = p.IsFeatured,
                IsOnePiece    = p.IsOnePiece
            };
        }
    }
}
