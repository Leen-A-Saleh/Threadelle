using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Models;
using Threadelle.Services;
using Threadelle.ViewModels;

namespace Threadelle.Controllers
{
    public class HomeController : BaseController
    {
        private readonly IProductPricingService _pricing;

        public HomeController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IProductPricingService pricing)
            : base(db, userManager)
        {
            _pricing = pricing;
        }

        public async Task<IActionResult> Index()
        {
            var wishlisted = await GetWishlistedProductIdsAsync();

            var featured = await Db.Products
                .Where(p => !p.IsDeleted && p.IsFeatured)
                .Include(p => p.Category)
                .Include(p => p.Images)

                .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            var onePiece = await Db.Products
                .Where(p => !p.IsDeleted && p.IsOnePiece)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            var collections = await Db.Categories
                .Where(c => c.IsActive && !c.IsDeleted && c.ParentCategoryId == null)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CollectionCardViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ImageUrl = c.ImageUrl,
                    ProductCount = c.Products.Count(p => !p.IsDeleted)
                })
                .ToListAsync();

            var testimonials = await Db.Testimonials
                .Where(t => t.IsApproved)
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .Take(9)
                .ToListAsync();

            var allProducts = featured.Concat(onePiece).ToList();
            var allIds      = allProducts.Select(p => p.Id).Distinct().ToList();
            var pricing     = await _pricing.GetEffectivePricesAsync(allIds);

            var vm = new HomeViewModel
            {
                FeaturedProducts = featured.Select(p => ProductCardViewModel.FromProduct(p, wishlisted.Contains(p.Id), pricing.GetValueOrDefault(p.Id))).ToList(),
                OnePieceProducts = onePiece.Select(p => ProductCardViewModel.FromProduct(p, wishlisted.Contains(p.Id), pricing.GetValueOrDefault(p.Id))).ToList(),
                Collections = collections,
                Testimonials = testimonials
            };

            ViewData["IsUniqueTreasures"] = true;
            return View(vm);
        }

        public IActionResult About() => View();

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
