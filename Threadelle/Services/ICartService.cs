using Threadelle.Models;

namespace Threadelle.Services
{
    public interface ICartService
    {
        /// <summary>Gets the current cart (guest or user), creating one if needed. Merges a guest cart into the user cart after login.</summary>
        Task<Cart> GetOrCreateCartAsync(bool includeItems = true);

        /// <summary>Returns the current cart without creating one. Null when empty.</summary>
        Task<Cart?> GetCartAsync(bool includeItems = true);

        Task<int> GetItemCountAsync();

        /// <summary>Adds an item and returns the cart's new total item count.</summary>
        Task<int> AddAsync(int productId, int? colorId, int quantity);

        Task UpdateQuantityAsync(int cartItemId, int quantity);

        Task RemoveAsync(int cartItemId);

        Task ClearAsync();
    }
}
