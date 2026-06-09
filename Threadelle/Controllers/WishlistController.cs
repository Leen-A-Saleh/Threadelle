using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Threadelle.Data;
using Threadelle.Models;
using Threadelle.ViewModels;

namespace Threadelle.Controllers
{
    public class WishlistController : BaseController
    {
        private const string GuestKey = "TE_GuestWishlist";

        public WishlistController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
            : base(db, userManager) { }

        // ── Guest session helpers ────────────────────────────
        private List<int> GuestList()
        {
            var json = HttpContext.Session.GetString(GuestKey);
            return string.IsNullOrEmpty(json)
                ? new List<int>()
                : JsonSerializer.Deserialize<List<int>>(json) ?? new();
        }

        private void SaveGuest(List<int> ids) =>
            HttpContext.Session.SetString(GuestKey, JsonSerializer.Serialize(ids));

        // ── Load products by ID list ─────────────────────────
        private async Task<List<ProductCardViewModel>> LoadByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids.ToList();
            if (!idList.Any()) return new();

            var products = await Db.Products
                .Where(p => idList.Contains(p.Id) && !p.IsDeleted)
                .Include(p => p.Category)
                .Include(p => p.Images)

                .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
                .ToListAsync();

            return products
                .OrderBy(p => idList.IndexOf(p.Id))
                .Select(p => ProductCardViewModel.FromProduct(p, isWishlisted: true))
                .ToList();
        }

        private async Task<List<ProductCardViewModel>> LoadUserItemsAsync(string userId)
        {
            var ids = await Db.Wishlists
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .Select(w => w.ProductId)
                .ToListAsync();
            return await LoadByIdsAsync(ids);
        }

        // ── Merge guest session wishlist → user DB on login ──
        private async Task MergeGuestAsync(string userId)
        {
            var guestIds = GuestList();
            if (!guestIds.Any()) return;

            var existingIds = await Db.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync();

            var toAdd = guestIds.Except(existingIds).ToList();
            if (toAdd.Any())
            {
                var validIds = await Db.Products
                    .Where(p => toAdd.Contains(p.Id) && !p.IsDeleted)
                    .Select(p => p.Id)
                    .ToListAsync();

                foreach (var id in validIds)
                    Db.Wishlists.Add(new Wishlist { UserId = userId, ProductId = id });

                await Db.SaveChangesAsync();
            }

            HttpContext.Session.Remove(GuestKey);
        }

        // ── Full-page wishlist ───────────────────────────────
        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId;
            if (userId != null)
            {
                await MergeGuestAsync(userId);
                return View(await LoadUserItemsAsync(userId));
            }
            return View(await LoadByIdsAsync(GuestList()));
        }

        // ── Offcanvas sidebar (AJAX) ─────────────────────────
        [HttpGet]
        public async Task<IActionResult> Sidebar()
        {
            var userId = CurrentUserId;
            if (userId != null)
            {
                await MergeGuestAsync(userId);
                return PartialView("_WishlistSidebar", await LoadUserItemsAsync(userId));
            }
            return PartialView("_WishlistSidebar", await LoadByIdsAsync(GuestList()));
        }

        // ── Toggle (guest + authenticated) ──────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = CurrentUserId;

            if (userId == null)
            {
                var ids = GuestList();
                bool added;
                if (ids.Contains(productId)) { ids.Remove(productId); added = false; }
                else { ids.Add(productId); added = true; }
                SaveGuest(ids);
                return Json(new { ok = true, added, count = ids.Count, message = added ? "Saved to your wishlist 💕" : "Removed from wishlist" });
            }

            var existing = await Db.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
            bool dbAdded;
            if (existing != null)
            {
                Db.Wishlists.Remove(existing);
                dbAdded = false;
            }
            else
            {
                var product = await Db.Products.FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
                if (product == null) return Json(new { ok = false, message = "Product not found." });
                Db.Wishlists.Add(new Wishlist { UserId = userId, ProductId = productId });
                dbAdded = true;
            }
            await Db.SaveChangesAsync();
            var count = await Db.Wishlists.CountAsync(w => w.UserId == userId);
            return Json(new { ok = true, added = dbAdded, count, message = dbAdded ? "Saved to your wishlist 💕" : "Removed from wishlist" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int productId)
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                var ids = GuestList();
                ids.Remove(productId);
                SaveGuest(ids);
                return Json(new { ok = true, count = ids.Count });
            }
            var existing = await Db.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
            if (existing != null) { Db.Wishlists.Remove(existing); await Db.SaveChangesAsync(); }
            var count = await Db.Wishlists.CountAsync(w => w.UserId == userId);
            return Json(new { ok = true, count });
        }

        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var userId = CurrentUserId;
            var count = userId != null
                ? await Db.Wishlists.CountAsync(w => w.UserId == userId)
                : GuestList().Count;
            return Json(new { count });
        }
    }
}
