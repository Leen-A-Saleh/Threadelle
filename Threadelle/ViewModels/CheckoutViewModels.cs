using System.ComponentModel.DataAnnotations;

namespace Threadelle.ViewModels
{
    public class CheckoutViewModel
    {
        public CartViewModel Cart { get; set; } = new();

        [Required(ErrorMessage = "Please tell us who this order is for.")]
        [Display(Name = "Full name")]
        [StringLength(80)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "We need a phone number for delivery.")]
        [Phone]
        [Display(Name = "Phone number")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your delivery address.")]
        [Display(Name = "Delivery address")]
        [StringLength(250)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [Display(Name = "City")]
        [StringLength(80)]
        public string City { get; set; } = string.Empty;

        [Display(Name = "A little note (optional)")]
        [StringLength(500)]
        public string? Note { get; set; }

        [Required]
        [Display(Name = "Payment method")]
        public string PaymentMethod { get; set; } = "Cash on Delivery";

        public string? AppliedCouponCode { get; set; }
        public decimal DiscountAmount { get; set; }

        public string? StripeClientSecret { get; set; }
        public string? StripePublishableKey { get; set; }
        public string? PaymentIntentId { get; set; }
    }
}
