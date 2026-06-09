using Threadelle.ViewModels.Admin;

namespace Threadelle.Services
{
    public interface IColorService
    {
        Task<List<ColorListItemViewModel>> GetListAsync();
        Task<ColorUpsertViewModel?> GetForEditAsync(int id);
        Task<int> UpsertAsync(ColorUpsertViewModel vm);
        Task<(bool ok, string message)> DeleteAsync(int id, string? deletedBy = null);
        Task<(bool ok, string message)> RestoreAsync(int id);
        Task<(bool ok, string message)> PermanentDeleteAsync(int id);
    }
}
