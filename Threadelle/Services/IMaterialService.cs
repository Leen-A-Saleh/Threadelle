using Threadelle.ViewModels.Admin;

namespace Threadelle.Services
{
    public interface IMaterialService
    {
        Task<List<MaterialListItemViewModel>> GetListAsync();
        Task<MaterialUpsertViewModel?> GetForEditAsync(int id);
        Task<int> UpsertAsync(MaterialUpsertViewModel vm);
        Task<(bool ok, string message)> DeleteAsync(int id, string? deletedBy = null);
        Task<(bool ok, string message)> RestoreAsync(int id);
        Task<(bool ok, string message)> PermanentDeleteAsync(int id);
    }
}
