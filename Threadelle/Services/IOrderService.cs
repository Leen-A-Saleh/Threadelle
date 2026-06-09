using Threadelle.Models;
using Threadelle.ViewModels.Admin;

namespace Threadelle.Services
{
    public interface IOrderService
    {
        Task<OrderIndexViewModel> GetIndexAsync(string? status, string? search, int page);
        Task<OrderDetailsViewModel?> GetDetailsAsync(int id);
        Task<bool> UpdateStatusAsync(int id, OrderStatus status);
        Task<bool> UpdatePaymentStatusAsync(int id, OrderPaymentStatus paymentStatus);
        Task<bool> UpdateAdminNoteAsync(int id, string? note);
    }
}
