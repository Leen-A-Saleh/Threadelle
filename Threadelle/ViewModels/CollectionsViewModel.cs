using Threadelle.Models;

namespace Threadelle.ViewModels
{
    public class CollectionsViewModel
    {
        public List<ProductCardViewModel> Products { get; set; } = new();

        // Filter sources
        public List<Category> Categories { get; set; } = new();
        public List<Material>  Materials   { get; set; } = new();

        // Active filters
        public string?      Search      { get; set; }
        public List<int>    CategoryIds { get; set; } = new();
        public List<int>    MaterialIds { get; set; } = new();
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string       Sort        { get; set; } = "newest";

        // Pagination
        public int Page      { get; set; } = 1;
        public int PageSize  { get; set; } = 9;
        public int TotalCount{ get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(Search) ||
            CategoryIds.Any() ||
            MaterialIds.Any() ||
            MinPrice.HasValue ||
            MaxPrice.HasValue;

        // Keeps all active filter state for a given page number (used by pagination links via JS)
        public Dictionary<string, string?> RouteValues(int? page = null) => new()
        {
            ["search"]   = Search,
            ["sort"]     = Sort,
            ["page"]     = (page ?? Page).ToString()
            // categoryIds, materialIds, priceRanges handled by JS form submit
        };
    }
}
