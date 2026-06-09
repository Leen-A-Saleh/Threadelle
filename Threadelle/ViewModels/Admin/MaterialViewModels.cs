namespace Threadelle.ViewModels.Admin
{
    public class MaterialListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal PricePerUnit { get; set; }
        public decimal StockQuantity { get; set; }
        public int ProductCount { get; set; }   // how many products use this material
    }

    public class MaterialUpsertViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal PricePerUnit { get; set; }
        public decimal StockQuantity { get; set; }
    }
}
