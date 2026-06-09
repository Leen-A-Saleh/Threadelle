namespace Threadelle.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string SessionId { get; set; }  = string.Empty;
        public ApplicationUser? User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
