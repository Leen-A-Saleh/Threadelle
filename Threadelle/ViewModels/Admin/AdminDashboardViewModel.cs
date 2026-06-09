using Threadelle.Models;

namespace Threadelle.ViewModels.Admin
{
    /// <summary>Everything the Admin dashboard landing renders: headline metrics + recent activity.</summary>
    public class AdminDashboardViewModel
    {
        // ── Stat cards ───────────────────────────────────────────────────────
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int PendingTestimonials { get; set; }
        public int PendingCustomOrders { get; set; }
        public int ActiveCoupons { get; set; }

        // ── Additional metrics ───────────────────────────────────────────────
        public decimal MonthRevenue { get; set; }
        public int MonthOrders { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockProducts { get; set; }

        // ── Chart data (last 7 days revenue — labels + values) ───────────────
        public List<string> RevenueChartLabels { get; set; } = new();
        public List<decimal> RevenueChartValues { get; set; } = new();

        // ── Top products ─────────────────────────────────────────────────────
        public List<TopProductRow> TopProducts { get; set; } = new();

        // ── Recent activity ──────────────────────────────────────────────────
        public List<RecentOrderRow> RecentOrders { get; set; } = new();
        public List<RecentUserRow> RecentUsers { get; set; } = new();
        public List<RecentTestimonialRow> RecentTestimonials { get; set; } = new();
        public List<RecentCustomOrderRow> RecentCustomOrders { get; set; } = new();
    }

    public class TopProductRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RecentOrderRow
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RecentUserRow
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RecentTestimonialRow
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public byte Rating { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RecentCustomOrderRow
    {
        public int Id { get; set; }
        public string CustomOrderNumber { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string UserName { get; set; } = string.Empty;
        public CustomOrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
