namespace Threadelle.ViewModels.Admin
{
    public enum RecycleBinEntityType
    {
        Product, Category, Color, Material, Testimonial, Review, CustomOrder, Coupon
    }

    public class RecycleBinItemViewModel
    {
        public int Id { get; set; }
        public RecycleBinEntityType EntityType { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    public class RecycleBinIndexViewModel
    {
        public List<RecycleBinItemViewModel> Items { get; set; } = new();
        public string? Search { get; set; }
        public string? EntityTypeFilter { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
