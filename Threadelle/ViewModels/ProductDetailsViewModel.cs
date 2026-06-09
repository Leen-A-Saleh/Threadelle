using Threadelle.Models;

namespace Threadelle.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product            Product       { get; set; } = null!;

        /// <summary>Effective (discounted) price shown to the customer.</summary>
        public decimal            Price         { get; set; }

        /// <summary>Original price before any product coupon. Same as Price when no discount.</summary>
        public decimal            OriginalPrice { get; set; }

        public decimal            DiscountAmount { get; set; }
        public string?            DiscountBadge  { get; set; }
        public bool               HasDiscount    => DiscountAmount > 0;

        public List<ProductImage>        Gallery         { get; set; } = new();
        public List<ProductDetailsColorViewModel> Colors { get; set; } = new();
        public List<Material>            Materials       { get; set; } = new();
        public List<ProductCardViewModel> RelatedProducts { get; set; } = new();
        public bool               InStock      { get; set; }
        public bool               IsWishlisted { get; set; }
        public List<ProductReview> Reviews     { get; set; } = new();
        public bool               CanReview    { get; set; }
        public bool               HasReviewed  { get; set; }
        public double             AverageRating { get; set; }
    }

    public class ProductDetailsColorViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? HexCode { get; set; }
        public int Quantity { get; set; }
    }
}
