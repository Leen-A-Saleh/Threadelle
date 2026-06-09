namespace Threadelle.Models
{
    public enum CustomOrderStatus
    {
        Pending          = 0,   // customer submitted, waiting for admin
        Reviewing        = 1,   // admin is actively reviewing
        Designing        = 2,   // accepted, designer working
        Rejected         = 3,   // declined, end of line
        InProgress       = 4,   // being crafted
        Delivered        = 5,
        Completed        = 6,
        UnderReview      = 7,
        Accepted         = 8,
        InProduction     = 9,
        AwaitingPayment  = 10,  // admin approved + set price → waiting for customer to pay
        ChangesRequested = 11,  // admin wants adjustments → customer can resubmit
        QualityCheck     = 12,
        Packaging        = 13
    }

    public class CustomOrder
    {
        public int Id { get; set; }
        public string CustomOrderNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public string? Title { get; set; }
        public decimal? Budget { get; set; }
        public DateTime? Deadline { get; set; }
        public decimal? EstimatedPrice { get; set; }
        public int? EstimatedDays { get; set; }
        public string? AdminNotes { get; set; }
        public CustomOrderStatus Status { get; set; }

        public bool ShowInGallery { get; set; }
        public bool IsGalleryFeatured { get; set; }
        public int GalleryDisplayOrder { get; set; }
        public DateTime? GalleryPublishedAt { get; set; }
        public int GalleryViewCount { get; set; } = 0;
        public DateTime? GalleryLastViewedAt { get; set; }
        public string? GalleryDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        public ICollection<CustomOrderImage> Images { get; set; } = new List<CustomOrderImage>();
        public ICollection<CustomOrderColor> Colors { get; set; } = new List<CustomOrderColor>();
        public ICollection<CustomOrderStatusHistory> StatusHistory { get; set; } = new List<CustomOrderStatusHistory>();
    }
}
