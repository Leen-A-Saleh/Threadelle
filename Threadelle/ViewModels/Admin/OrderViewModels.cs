using Threadelle.Models;

namespace Threadelle.ViewModels.Admin
{
    public class OrderListItemViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public OrderStatus Status { get; set; }
        public OrderPaymentStatus PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderIndexViewModel
    {
        public List<OrderListItemViewModel> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }
    }

    public class OrderItemRowViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string? ColorName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? ImageUrl { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }

    public class OrderDetailsViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;
        public OrderPaymentStatus PaymentStatus { get; set; }
        public OrderStatus Status { get; set; }

        public string? CustomerNote { get; set; }
        public string? AdminNote { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<OrderItemRowViewModel> Items { get; set; } = new();
    }
}
