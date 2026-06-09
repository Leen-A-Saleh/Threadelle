using Threadelle.ViewModels.Admin;

namespace Threadelle.Services
{
    public interface IProductService
    {
        Task<ProductIndexViewModel> GetIndexAsync(string? search, int? categoryId, string? type, int page);
        Task<ProductUpsertViewModel> BuildCreateAsync();
        Task<ProductUpsertViewModel?> GetForEditAsync(int id);

        /// <summary>Refills dropdown/option data after a validation failure.</summary>
        Task PopulateOptionsAsync(ProductUpsertViewModel vm);

        /// <summary>Creates (Id == 0) or updates a product, including images, colors and materials.</summary>
        Task<int> UpsertAsync(ProductUpsertViewModel vm);

        Task<(bool ok, string message)> SoftDeleteAsync(int id, string? deletedBy = null);
        Task<(bool ok, string message)> DeleteImageAsync(int imageId, int productId);
        Task<(bool ok, string message)> SetPrimaryImageAsync(int imageId, int productId);
        Task<bool?> ToggleFeaturedAsync(int id);
        Task<bool?> ToggleActiveAsync(int id);
    }
}
