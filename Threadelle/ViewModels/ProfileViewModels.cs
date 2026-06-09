using System.ComponentModel.DataAnnotations;
using Threadelle.Models;

namespace Threadelle.ViewModels
{
    public class ProfileViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public int OrderCount { get; set; }
        public int WishlistCount { get; set; }
        public int CustomOrderCount { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(80)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Birth date")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "Gender")]
        public ApplicationUserGender Gender { get; set; }
    }
}
