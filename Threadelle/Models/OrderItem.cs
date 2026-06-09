namespace Threadelle.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int? ColorId { get; set; }
        public Color? Color { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string? ColorName { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
    }
}
