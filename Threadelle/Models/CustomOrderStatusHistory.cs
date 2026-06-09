namespace Threadelle.Models
{
    public class CustomOrderStatusHistory
    {
        public int Id { get; set; }
        public int CustomOrderId { get; set; }
        public CustomOrder CustomOrder { get; set; } = null!;

        public CustomOrderStatus FromStatus { get; set; }
        public CustomOrderStatus ToStatus { get; set; }

        public string? Note { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
