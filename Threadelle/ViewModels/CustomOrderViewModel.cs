using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Threadelle.Models;

namespace Threadelle.ViewModels
{
    public class CustomOrderFormViewModel
    {
        // Step 1 — The dream
        [Required(ErrorMessage = "Please give your idea a title.")]
        [StringLength(120)]
        [Display(Name = "What are we creating?")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Which collection inspires it?")]
        public int? CategoryId { get; set; }

        // Step 2 — The details

        [Display(Name = "Favourite colours")]
        public List<int> ColorIds { get; set; } = new();

        // Step 3 — Timing & budget
        [Range(0, 100000, ErrorMessage = "Please enter a realistic budget.")]
        [Display(Name = "Budget (optional)")]
        public decimal? Budget { get; set; }

        [Display(Name = "Needed by (optional)")]
        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; }

        // Step 4 — Inspiration
        [Display(Name = "Inspiration photos")]
        public List<IFormFile>? Images { get; set; }

        // Sources for selects
        public List<Category> Categories { get; set; } = new();
        public List<Color> Colors { get; set; } = new();

        public List<CustomOrder> CompletedOrders { get; set; } = new();
    }
}
