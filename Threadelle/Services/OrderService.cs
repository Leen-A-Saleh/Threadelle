using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Helpers;
using Threadelle.Models;
using Threadelle.ViewModels.Admin;

namespace Threadelle.Services
{
    public class OrderService : IOrderService
    {
        private const int PageSize = 15;

        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly INotificationService _notifications;

        public OrderService(ApplicationDbContext db, IMapper mapper, INotificationService notifications)
        {
            _db = db;
            _mapper = mapper;
            _notifications = notifications;
        }

        public async Task<OrderIndexViewModel> GetIndexAsync(string? status, string? search, int page)
        {
            if (page < 1) page = 1;

            var q = _db.Orders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, out var parsed))
                q = q.Where(o => o.Status == parsed);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(o => o.OrderNumber.Contains(search) || o.User.FullName.Contains(search));

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * PageSize).Take(PageSize)
                .ProjectTo<OrderListItemViewModel>(_mapper.ConfigurationProvider)
                .ToListAsync();

            return new OrderIndexViewModel
            {
                Items = items,
                TotalCount = total,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(total / (double)PageSize),
                Status = status,
                Search = search
            };
        }

        public async Task<OrderDetailsViewModel?> GetDetailsAsync(int id)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product).ThenInclude(p => p.Images)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Color)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return null;

            return new OrderDetailsViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                CustomerName = order.User?.FullName ?? "Unknown",
                CustomerEmail = order.User?.Email,
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                TotalPrice = order.TotalPrice,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                Status = order.Status,
                CustomerNote = order.CustomerNote,
                AdminNote = order.AdminNote,
                CreatedAt = order.CreatedAt,
                Items = order.OrderItems.Select(oi => new OrderItemRowViewModel
                {
                    ProductName = oi.ProductName,
                    ColorName = oi.ColorName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    ImageUrl = oi.Product != null ? oi.Product.PrimaryImageUrl() : null
                }).ToList()
            };
        }

        public async Task<bool> UpdateStatusAsync(int id, OrderStatus status)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return false;

            var oldStatus = order.Status;
            order.Status = status;
            await _db.SaveChangesAsync();

            if (oldStatus != status && !string.IsNullOrEmpty(order.UserId))
            {
                await _notifications.NotifyUserAsync(
                    order.UserId,
                    "Order update 📦",
                    $"Your order {order.OrderNumber} is now \"{status.Friendly()}\".",
                    NotificationType.OrderStatusChanged,
                    $"/Profile/OrderDetails/{order.Id}");
            }
            return true;
        }

        public async Task<bool> UpdatePaymentStatusAsync(int id, OrderPaymentStatus paymentStatus)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return false;

            var wasUnpaid = order.PaymentStatus != OrderPaymentStatus.Paid;
            order.PaymentStatus = paymentStatus;
            await _db.SaveChangesAsync();

            // Notify the customer when payment is newly marked as received.
            if (wasUnpaid && paymentStatus == OrderPaymentStatus.Paid && !string.IsNullOrEmpty(order.UserId))
            {
                await _notifications.NotifyUserAsync(
                    order.UserId,
                    "Payment received 💛",
                    $"We've received payment for order {order.OrderNumber}. Thank you!",
                    NotificationType.PaymentSuccess,
                    $"/Profile/OrderDetails/{order.Id}");
            }
            return true;
        }

        public async Task<bool> UpdateAdminNoteAsync(int id, string? note)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return false;

            order.AdminNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
