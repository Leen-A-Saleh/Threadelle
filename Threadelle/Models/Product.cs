namespace Threadelle.Models
{
    public enum ProductLabel { None = 0, New = 1, Bestseller = 2, Limited = 3 }

    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? StoryTelling { get; set; }
        public string? ProductStory { get; set; }
        public string? ProductType { get; set; }
        public string? SoldOutDisplayMode { get; set; }
        public string? SoldOutLabel { get; set; }

        public decimal WorkHours { get; set; }
        public decimal HourRate { get; set; }
        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public bool IsOnePiece { get; set; }
        public bool HasColorOptions { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true;
        public ProductLabel Label { get; set; } = ProductLabel.None;

        public bool ShowInGallery { get; set; }
        public bool IsGalleryFeatured { get; set; }
        public int GalleryDisplayOrder { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductColor> ProductColors { get; set; } = new List<ProductColor>();
        public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();
    }
}
