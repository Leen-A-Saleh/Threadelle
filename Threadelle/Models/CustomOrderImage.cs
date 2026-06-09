namespace Threadelle.Models
{
    public class CustomOrderImage
    {
        public int Id { get; set; }

        public int CustomOrderId { get; set; }
        public CustomOrder CustomOrder { get; set; } = null!;
        public string ImageUrl { get; set; } = string.Empty;

        public string? Caption { get; set; }
        public string? AltText { get; set; }
        
        public bool IsFinishedProduct { get; set; }
        public bool IsCoverImage { get; set; }
        public bool IsDeleted { get; set; }
        public int DisplayOrder { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    }
}
