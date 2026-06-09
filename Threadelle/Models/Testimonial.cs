namespace Threadelle.Models
{
    public class Testimonial
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public string Message { get; set; } = string.Empty;

        public byte Rating { get; set; }
        
        public string? ImageUrl { get; set; }

        public bool IsApproved { get; set; } = false;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
