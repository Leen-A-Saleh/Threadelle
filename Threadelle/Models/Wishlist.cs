namespace Threadelle.Models
{
    public class Wishlist
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
