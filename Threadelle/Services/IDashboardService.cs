using Threadelle.ViewModels.Admin;

namespace Threadelle.Services
{
    /// <summary>Builds the Admin dashboard snapshot (headline metrics + recent activity).</summary>
    public interface IDashboardService
    {
        Task<AdminDashboardViewModel> GetDashboardAsync();
    }
}
