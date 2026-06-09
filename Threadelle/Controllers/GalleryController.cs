using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Models;
using Threadelle.ViewModels;

namespace Threadelle.Controllers
{
    public class GalleryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public GalleryController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var items = new List<GalleryItemVM>();

            // Get Products marked for Gallery
            var products = await _db.Products
                .Include(p => p.Images)
                .Where(p => !p.IsDeleted && p.IsActive && p.ShowInGallery)
                .ToListAsync();

            foreach (var p in products)
            {
                var primaryImage = p.Images.FirstOrDefault(i => i.IsPrimary) 
                                   ?? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault();
                                   
                if (primaryImage != null)
                {
                    items.Add(new GalleryItemVM
                    {
                        Id = p.Id,
                        Title = p.Name,
                        ImageUrl = primaryImage.ImageUrl,
                        AltText = primaryImage.AltText ?? p.Name,
                        IsFeatured = p.IsGalleryFeatured,
                        DisplayOrder = p.GalleryDisplayOrder
                    });
                }
            }

            var vm = new GalleryPageVM
            {
                FeaturedItems = items.Where(i => i.IsFeatured).OrderBy(i => i.DisplayOrder).ToList(),
                MasonryItems = items.OrderBy(i => i.DisplayOrder).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetGalleryItemDetails(int id)
        {
            var product = await _db.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted && p.IsActive && p.ShowInGallery);

            if (product == null) return NotFound();

            var vm = new GalleryItemDetailsVM
            {
                Id = product.Id,
                Title = product.Name,
                Description = product.StoryTelling ?? product.ProductStory,
                Images = product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder)
                            .Select(i => new GalleryLightboxImageVM { ImageUrl = i.ImageUrl, AltText = i.AltText }).ToList()
            };
            
            return Json(vm);
        }
    }
}
