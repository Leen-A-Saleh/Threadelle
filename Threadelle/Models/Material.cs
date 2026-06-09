namespace Threadelle.Models
{
    public class Material
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public decimal PricePerUnit { get; set; }

        public string Unit { get; set; } = string.Empty;

        public decimal StockQuantity { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();
    }
}
