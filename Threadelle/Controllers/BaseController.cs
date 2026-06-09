using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Models;

namespace Threadelle.Controllers
{
    /// <summary>Small conveniences shared by the storefront controllers.</summary>
    public abstract class BaseController : Controller
    {
        protected readonly ApplicationDbContext Db;
        protected readonly UserManager<ApplicationUser> UserManager;

        protected BaseController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            Db = db;
            UserManager = userManager;
        }

        protected string? CurrentUserId =>
            User.Identity?.IsAuthenticated == true ? UserManager.GetUserId(User) : null;

        /// <summary>Product ids the current user has wishlisted (empty for guests) — used to fill in heart icons.</summary>
        protected async Task<HashSet<int>> GetWishlistedProductIdsAsync()
        {
            var userId = CurrentUserId;
            if (userId == null) return new HashSet<int>();
            var ids = await Db.Wishlists.Where(w => w.UserId == userId).Select(w => w.ProductId).ToListAsync();
            return ids.ToHashSet();
        }

        protected void Flash(string message, string type = "success")
        {
            TempData["FlashMessage"] = message;
            TempData["FlashType"] = type;
        }
    }
}
