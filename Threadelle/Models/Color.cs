using System.ComponentModel.DataAnnotations;

namespace Threadelle.Models
{
    public class Color
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string? HexCode { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        public ICollection<ProductColor> ProductColors { get; set; } = new List<ProductColor>();

        public ICollection<CustomOrderColor> CustomOrderColors { get; set; } = new List<CustomOrderColor>();
    }
}
