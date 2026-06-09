using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Models;

namespace Threadelle.Services
{
    /// <summary>
    /// Handles the shopping cart for both guests and signed-in users.
    /// Guests are tracked by a cookie-stored session id; once they log in,
    /// their guest cart is merged into their account cart automatically on
    /// the first cart access of the authenticated session.
    /// </summary>
    public class CartService : ICartService
    {
        private const string CartCookie = "te_cart";

        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _http;
        private readonly UserManager<ApplicationUser> _userManager;

        // Per-request guards (CartService is scoped — one instance per request).
        private bool _mergeChecked;
        private string? _ensuredGuestSession;

        public CartService(ApplicationDbContext db, IHttpContextAccessor http, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _http = http;
            _userManager = userManager;
        }

        private HttpContext Context => _http.HttpContext!;

        private string? CurrentUserId =>
            Context.User?.Identity?.IsAuthenticated == true
                ? _userManager.GetUserId(Context.User)
                : null;

        private IQueryable<Cart> CartQuery(bool includeItems)
        {
            IQueryable<Cart> q = _db.Carts;
            if (includeItems)
            {
                q = q.Include(c => c.CartItems).ThenInclude(ci => ci.Product).ThenInclude(p => p.Images)
                     .Include(c => c.CartItems).ThenInclude(ci => ci.Product).ThenInclude(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
                     .Include(c => c.CartItems).ThenInclude(ci => ci.Product).ThenInclude(p => p.ProductColors)
                     .Include(c => c.CartItems).ThenInclude(ci => ci.Color);
            }
            return q;
        }

        // ── Public API ───────────────────────────────────────────────────────

        public async Task<Cart> GetOrCreateCartAsync(bool includeItems = true)
        {
            await MergeGuestCartIfNeededAsync();

            var userId = CurrentUserId;
            if (userId != null)
            {
                var userCart = await CartQuery(includeItems).FirstOrDefaultAsync(c => c.UserId == userId);
                if (userCart == null)
                {
                    userCart = new Cart { UserId = userId, SessionId = Guid.NewGuid().ToString("N") };
                    _db.Carts.Add(userCart);
                    await _db.SaveChangesAsync();
                }
                return userCart;
            }

            // Guest flow — ensure a session id exists (idempotent within the request).
            var sessionId = EnsureGuestSession();

            var cart = await CartQuery(includeItems).FirstOrDefaultAsync(c => c.SessionId == sessionId && c.UserId == null);
            if (cart == null)
            {
                cart = new Cart { SessionId = sessionId };
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync();
            }
            return cart;
        }

        public async Task<Cart?> GetCartAsync(bool includeItems = true)
        {
            // Folding a leftover guest cart into the account must happen here too:
            // checkout, the cart page, the sidebar and the count badge all read
            // through this method, and any of them may be the first authenticated hit.
            await MergeGuestCartIfNeededAsync();

            var userId = CurrentUserId;
            if (userId != null)
                return await CartQuery(includeItems).FirstOrDefaultAsync(c => c.UserId == userId);

            var sessionId = Context.Request.Cookies[CartCookie];
            if (string.IsNullOrEmpty(sessionId)) return null;
            return await CartQuery(includeItems).FirstOrDefaultAsync(c => c.SessionId == sessionId && c.UserId == null);
        }

        public async Task<int> GetItemCountAsync()
        {
            var cart = await GetCartAsync(includeItems: false);
            if (cart == null) return 0;
            return await _db.CartItems.Where(ci => ci.CartId == cart.Id).SumAsync(ci => (int?)ci.Quantity) ?? 0;
        }

        public async Task<int> AddAsync(int productId, int? colorId, int quantity)
        {
            if (quantity < 1) quantity = 1;

            var product = await _db.Products.Include(p => p.ProductColors).FirstOrDefaultAsync(p => p.Id == productId && p.IsActive && !p.IsDeleted)
                ?? throw new InvalidOperationException("Sorry, this piece is no longer available.");

            var cart = await GetOrCreateCartAsync(includeItems: false);

            var existing = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId && ci.ColorId == colorId);

            if (existing != null)
                existing.Quantity += quantity;
            else
                _db.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    ColorId = colorId,
                    Quantity = quantity
                });

            await _db.SaveChangesAsync();

            // Respect one-piece / stock limits, then report the authoritative count.
            ClampToStock(product, cart.Id);
            await _db.SaveChangesAsync();

            return await _db.CartItems.Where(ci => ci.CartId == cart.Id).SumAsync(ci => (int?)ci.Quantity) ?? 0;
        }

        public async Task UpdateQuantityAsync(int cartItemId, int quantity)
        {
            var item = await _db.CartItems.Include(ci => ci.Product).ThenInclude(p => p.ProductColors).FirstOrDefaultAsync(ci => ci.Id == cartItemId);
            if (item == null) return;
            if (!await OwnsCartAsync(item.CartId)) return;

            if (quantity < 1)
            {
                _db.CartItems.Remove(item);
            }
            else
            {
                var max = GetMaxQuantity(item.Product, item.ColorId);
                item.Quantity = Math.Min(quantity, Math.Max(1, max));
            }
            await _db.SaveChangesAsync();
        }

        public async Task RemoveAsync(int cartItemId)
        {
            var item = await _db.CartItems.FirstOrDefaultAsync(ci => ci.Id == cartItemId);
            if (item == null) return;
            if (!await OwnsCartAsync(item.CartId)) return;
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }

        public async Task ClearAsync()
        {
            var cart = await GetCartAsync(includeItems: true);
            if (cart == null) return;
            _db.CartItems.RemoveRange(cart.CartItems);
            await _db.SaveChangesAsync();
        }

        // ── Guest → user merge ───────────────────────────────────────────────

        /// <summary>
        /// Once per request, if the visitor is now authenticated but still carries a
        /// guest cart cookie, fold that guest cart into their account cart. Quantities,
        /// colours and variants are preserved; duplicate lines are merged by summing and
        /// re-clamped to current stock. The guest cart and cookie are then removed.
        /// </summary>
        private async Task MergeGuestCartIfNeededAsync()
        {
            if (_mergeChecked) return;

            var userId = CurrentUserId;
            if (userId == null) return;            // guest — nothing to merge (re-check cheap next call)
            _mergeChecked = true;

            var sessionId = Context.Request.Cookies[CartCookie];
            if (string.IsNullOrEmpty(sessionId)) return;

            var guestCart = await CartQuery(true)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.UserId == null);

            // Always clear the stale cookie so we don't re-attempt every request.
            Context.Response.Cookies.Delete(CartCookie);

            if (guestCart == null || guestCart.CartItems.Count == 0)
            {
                if (guestCart != null) _db.Carts.Remove(guestCart);
                try { await _db.SaveChangesAsync(); } catch (DbUpdateException) { _db.ChangeTracker.Clear(); }
                return;
            }

            var userCart = await CartQuery(true).FirstOrDefaultAsync(c => c.UserId == userId);

            if (userCart == null)
            {
                // No account cart yet — adopt the guest cart wholesale (keeps every line).
                guestCart.UserId = userId;
                ReclampCart(guestCart);
            }
            else
            {
                MergeItems(from: guestCart, into: userCart);
                _db.Carts.Remove(guestCart);
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // A parallel request already merged this guest cart — discard our
                // in-memory copy so later reads return the committed account cart.
                _db.ChangeTracker.Clear();
            }
        }

        private void MergeItems(Cart from, Cart into)
        {
            foreach (var item in from.CartItems)
            {
                var max = item.Product != null ? GetMaxQuantity(item.Product, item.ColorId) : item.Quantity;

                var match = into.CartItems.FirstOrDefault(i => i.ProductId == item.ProductId && i.ColorId == item.ColorId);
                if (match != null)
                    match.Quantity = Math.Min(match.Quantity + item.Quantity, max);   // merge duplicate by summing
                else
                    into.CartItems.Add(new CartItem
                    {
                        ProductId = item.ProductId,
                        ColorId = item.ColorId,                                        // preserve colour / variant
                        Quantity = Math.Min(item.Quantity, max)
                    });
            }
        }

        private void ReclampCart(Cart cart)
        {
            foreach (var i in cart.CartItems)
            {
                if (i.Product == null) continue;
                var max = GetMaxQuantity(i.Product, i.ColorId);
                if (i.Quantity > max) i.Quantity = Math.Max(1, max);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private string EnsureGuestSession()
        {
            var sessionId = _ensuredGuestSession ?? Context.Request.Cookies[CartCookie];
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString("N");
                Context.Response.Cookies.Append(CartCookie, sessionId, new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    SameSite = SameSiteMode.Lax
                });
            }
            _ensuredGuestSession = sessionId;   // reuse across calls within this request
            return sessionId;
        }

        private void ClampToStock(Product product, int cartId)
        {
            var items = _db.CartItems.Where(ci => ci.CartId == cartId && ci.ProductId == product.Id).ToList();
            var max = GetMaxQuantity(product, items.FirstOrDefault()?.ColorId);
            foreach (var i in items)
            {
                var currentMax = GetMaxQuantity(product, i.ColorId);
                if (i.Quantity > currentMax) i.Quantity = Math.Max(1, currentMax);
            }
        }

        private int GetMaxQuantity(Product? product, int? colorId)
        {
            if (product == null) return 99;
            int max = product.Quantity;
            if (colorId.HasValue && product.ProductColors != null)
            {
                var pc = product.ProductColors.FirstOrDefault(c => c.ColorId == colorId.Value);
                if (pc != null) max = pc.Quantity;
            }
            return product.IsOnePiece ? Math.Min(1, max) : Math.Max(0, max);
        }

        private async Task<bool> OwnsCartAsync(int cartId)
        {
            var cart = await GetCartAsync(includeItems: false);
            return cart != null && cart.Id == cartId;
        }
    }
}
