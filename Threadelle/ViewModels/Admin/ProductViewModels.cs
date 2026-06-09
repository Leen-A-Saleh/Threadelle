using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Threadelle.ViewModels.Admin
{
    // ── Index list row ────────────────────────────────────────────────────────
    public class ProductListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsOnePiece { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public Threadelle.Models.ProductLabel Label { get; set; }
    }

    // ── Upsert form (Create + Edit) ───────────────────────────────────────────
    public class ProductUpsertViewModel
    {
        public int Id { get; set; }

        // Core
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? StoryTelling { get; set; }
        public int CategoryId { get; set; }

        // Pricing (used in CalculatePrice: labour + materials)
        public decimal WorkHours { get; set; }
        public decimal HourRate { get; set; }

        // Stock
        public int Quantity { get; set; }

        // Flags
        public bool IsOnePiece { get; set; }
        public bool HasColorOptions { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true;
        public Threadelle.Models.ProductLabel Label { get; set; } = Threadelle.Models.ProductLabel.None;

        public bool ShowInGallery { get; set; }
        public bool IsGalleryFeatured { get; set; }
        public int GalleryDisplayOrder { get; set; }

        // Images
        public List<IFormFile> NewImages { get; set; } = new();
        public List<ProductImageRowViewModel> ExistingImages { get; set; } = new();

        // Color & Material associations
        public List<ProductColorRowViewModel> ColorRows { get; set; } = new();
        public List<ProductMaterialRowViewModel> MaterialRows { get; set; } = new();

        // Dropdown data (server-populated, not posted back as data)
        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<MaterialOptionViewModel> MaterialOptions { get; set; } = Enumerable.Empty<MaterialOptionViewModel>();

        // Read-only computed price displayed in the form sidebar (labour only; materials added when saved)
        public decimal LabourPrice => WorkHours * HourRate;
    }

    public class ProductColorRowViewModel
    {
        public int ColorId { get; set; }
        public string ColorName { get; set; } = string.Empty;
        public string? HexCode { get; set; }
        public bool IsSelected { get; set; }
        public int Quantity { get; set; }
    }

    public class ProductImageRowViewModel
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int DisplayOrder { get; set; }
        public string? AltText { get; set; }
    }

    public class ProductMaterialRowViewModel
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal PricePerUnit { get; set; }
        public decimal QuantityUsed { get; set; }
    }

    public class MaterialOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal PricePerUnit { get; set; }
    }

    public class ColorOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? HexCode { get; set; }
    }

    // ── Paginated index response ──────────────────────────────────────────────
    public class ProductIndexViewModel
    {
        public List<ProductListItemViewModel> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public string? Type { get; set; }
        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
