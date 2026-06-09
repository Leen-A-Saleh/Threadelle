using System.ComponentModel.DataAnnotations;
using Threadelle.Models;

namespace Threadelle.ViewModels.Admin
{
    public class CouponUpsertViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DiscountType DiscountType { get; set; }

        [Required]
        [Range(0, 999999)]
        public decimal DiscountValue { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int? UsageLimit { get; set; }
        public int? UsageLimitPerUser { get; set; }

        public decimal? MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }

        public bool IsActive { get; set; } = true;
        
        public bool IsAutomaticPromotion { get; set; } = false;

        [Required]
        public CouponApplicationType ApplicationType { get; set; } = CouponApplicationType.EntireOrder;

        public List<int> SelectedProductIds { get; set; } = new();
        public List<int> SelectedCategoryIds { get; set; } = new();
    }
}
