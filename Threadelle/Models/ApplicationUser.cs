using Microsoft.AspNetCore.Identity;

namespace Threadelle.Models
{
    public enum ApplicationUserGender { Male, Female}
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string? ProfileImageUrl { get; set; }
        public DateOnly? BirthDate { get; set; }

        public ApplicationUserGender Gender { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<CustomOrder> CustomOrders { get; set; } = new List<CustomOrder>();
    }
}