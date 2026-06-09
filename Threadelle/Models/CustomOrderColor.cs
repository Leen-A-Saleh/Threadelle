namespace Threadelle.Models
{
    public class CustomOrderColor
    {
        public int Id { get; set; }

        public int CustomOrderId { get; set; }
        public CustomOrder CustomOrder { get; set; } = null!;
        public int ColorId { get; set; }

        public Color Color { get; set; } = null!;
    }
}
