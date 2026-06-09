using System.ComponentModel.DataAnnotations;

namespace Threadelle.Models
{
    public class Category
    {
        public int Id { get; set; }

        public int? ParentCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } =  string.Empty;

        public string? ImageUrl { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }

        public string? DeletedBy { get; set; }

        public Category? ParentCategory { get; set; }

        public ICollection<Category> Children { get; set; } = new List<Category>();

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}