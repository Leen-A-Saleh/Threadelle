using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Threadelle.ViewModels.Admin
{
    /// <summary>Row in the Categories index/list.</summary>
    public class CategoryListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ParentName { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public int ProductCount { get; set; }
        public int ChildCount { get; set; }
        public bool IsMain => string.IsNullOrEmpty(ParentName);
    }

    /// <summary>Single Create + Edit (upsert) form model.</summary>
    public class CategoryUpsertViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public IFormFile? ImageFile { get; set; }
        public string? ExistingImageUrl { get; set; }

        // Populated by the service for the parent dropdown (not validated / not posted back as data).
        public IEnumerable<SelectListItem> ParentOptions { get; set; } = new List<SelectListItem>();
    }

    /// <summary>Category details page: info + child categories + products.</summary>
    public class CategoryDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public string? ParentName { get; set; }
        public int ProductCount { get; set; }
        public int ChildCount { get; set; }

        public List<CategoryListItemViewModel> Children { get; set; } = new();
        public List<CategoryProductRow> Products { get; set; } = new();
    }

    public class CategoryProductRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsActive { get; set; }
        public string? ImageUrl { get; set; }
    }
}
