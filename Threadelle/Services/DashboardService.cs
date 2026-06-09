using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Models;
using Threadelle.ViewModels.Admin;

namespace Threadelle.Services
{
    public class DashboardService : IDashboardService
    {
        private const int RecentTake = 6;

        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public DashboardService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _db = db;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<AdminDashboardViewModel> GetDashboardAsync()
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // Build last-7-days chart data
            var chartLabels = new List<string>();
            var chartValues = new List<decimal>();
            for (int i = 6; i >= 0; i--)
            {
                var day = now.Date.AddDays(-i);
                var dayEnd = day.AddDays(1);
                var rev = await _db.Orders
                    .Where(o => o.CreatedAt >= day && o.CreatedAt < dayEnd
                             && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m;
                chartLabels.Add(day.ToString("ddd d"));
                chartValues.Add(rev);
            }

            var topProducts = await _db.OrderItems
                .Where(oi => oi.Order.Status != OrderStatus.Cancelled)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new TopProductRow
                {
                    Id = g.Key,
                    Name = g.First().Product.Name,
                    ImageUrl = g.First().Product.Images
                        .OrderBy(img => img.IsPrimary ? 0 : 1).Select(img => img.ImageUrl).FirstOrDefault(),
                    OrderCount = g.Count(),
                    Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                })
                .OrderByDescending(r => r.Revenue)
                .Take(5)
                .ToListAsync();

            return new AdminDashboardViewModel
            {
                // ── Headline metrics ────────────────────────────────────────
                TotalUsers          = await _userManager.Users.CountAsync(),
                TotalOrders         = await _db.Orders.CountAsync(),
                TotalRevenue        = await _db.Orders
                                          .Where(o => o.Status != OrderStatus.Cancelled)
                                          .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m,
                TotalProducts       = await _db.Products.CountAsync(p => !p.IsDeleted),
                TotalCategories     = await _db.Categories.CountAsync(c => !c.IsDeleted),
                PendingTestimonials = await _db.Testimonials.CountAsync(t => !t.IsApproved && !t.IsDeleted),
                PendingCustomOrders = await _db.CustomOrders.CountAsync(c =>
                                          !c.IsDeleted && (c.Status == CustomOrderStatus.Pending ||
                                          c.Status == CustomOrderStatus.UnderReview)),
                ActiveCoupons       = await _db.Coupons.CountAsync(c => c.IsActive && !c.IsDeleted),
                // ── Additional metrics ──────────────────────────────────────
                MonthRevenue        = await _db.Orders
                                          .Where(o => o.CreatedAt >= monthStart && o.Status != OrderStatus.Cancelled)
                                          .SumAsync(o => (decimal?)o.TotalPrice) ?? 0m,
                MonthOrders         = await _db.Orders.CountAsync(o => o.CreatedAt >= monthStart),
                PendingOrders       = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
                LowStockProducts    = await _db.Products.CountAsync(p => !p.IsDeleted && !p.IsOnePiece && p.Quantity <= 3 && p.Quantity > 0),

                // ── Chart data ──────────────────────────────────────────────
                RevenueChartLabels  = chartLabels,
                RevenueChartValues  = chartValues,

                // ── Top products ────────────────────────────────────────────
                TopProducts         = topProducts,

                // ── Recent activity (efficient SQL projections via AutoMapper) ─
                RecentOrders = await _db.Orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(RecentTake)
                    .ProjectTo<RecentOrderRow>(_mapper.ConfigurationProvider)
                    .ToListAsync(),

                RecentTestimonials = await _db.Testimonials
                    .Where(t => !t.IsDeleted)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(RecentTake)
                    .ProjectTo<RecentTestimonialRow>(_mapper.ConfigurationProvider)
                    .ToListAsync(),

                RecentCustomOrders = await _db.CustomOrders
                    .Where(c => !c.IsDeleted)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(RecentTake)
                    .ProjectTo<RecentCustomOrderRow>(_mapper.ConfigurationProvider)
                    .ToListAsync(),

                RecentUsers = await _userManager.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(RecentTake)
                    .ProjectTo<RecentUserRow>(_mapper.ConfigurationProvider)
                    .ToListAsync()
            };
        }
    }
}
