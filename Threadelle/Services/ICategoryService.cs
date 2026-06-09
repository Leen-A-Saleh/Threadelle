using Microsoft.AspNetCore.Mvc.Rendering;
using Threadelle.ViewModels.Admin;

namespace Threadelle.Services
{
    public interface ICategoryService
    {
        Task<List<CategoryListItemViewModel>> GetListAsync();
        Task<CategoryUpsertViewModel> BuildCreateAsync();
        Task<CategoryUpsertViewModel?> GetForEditAsync(int id);
        Task<CategoryDetailsViewModel?> GetDetailsAsync(int id);
        Task<IEnumerable<SelectListItem>> GetParentOptionsAsync(int? excludeId);

        /// <summary>Creates (Id == 0) or updates a category. Returns the category id.</summary>
        Task<int> UpsertAsync(CategoryUpsertViewModel vm);

        /// <summary>Soft-deletes a category. Returns success + a user-facing message.</summary>
        Task<(bool ok, string message)> DeleteAsync(int id, string? deletedBy = null);
    }
}
