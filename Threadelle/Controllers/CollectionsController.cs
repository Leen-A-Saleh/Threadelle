using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Helpers;
using Threadelle.Models;
using Threadelle.Services;
using Threadelle.ViewModels;

namespace Threadelle.Controllers
{
    public class CollectionsController : BaseController
    {
        private readonly IProductPricingService _pricing;

        public CollectionsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IProductPricingService pricing)
            : base(db, userManager)
        {
            _pricing = pricing;
        }

        public async Task<IActionResult> Index(
            string? search,
            List<int>? categoryIds,
            List<int>? materialIds,
            decimal? minPrice,
            decimal? maxPrice,
            string? category,
            string sort = "newest",
            int page = 1)
        {
            if (page < 1) page = 1;
            const int pageSize = 9;

            if (!string.IsNullOrWhiteSpace(category))
            {
                var cat = await Db.Categories.FirstOrDefaultAsync(c => c.Slug == category);
                if (cat != null)
                {
                    categoryIds ??= new List<int>();
                    if (!categoryIds.Contains(cat.Id))
                        categoryIds.Add(cat.Id);
                }
            }

            var query = Db.Products
                .Where(p => !p.IsDeleted)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
                .AsQueryable();



            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p => p.Name.Contains(term)
                    || (p.StoryTelling != null && p.StoryTelling.Contains(term))
                    || p.Category.Name.Contains(term));
            }

            if (categoryIds != null && categoryIds.Any())
                query = query.Where(p => categoryIds.Contains(p.CategoryId));

            if (materialIds != null && materialIds.Any())
                query = query.Where(p => p.ProductMaterials.Any(pm => materialIds.Contains(pm.MaterialId)));

            // Price filtering and price sorting both require the computed price — materialise first.
            bool needsMemoryOps = sort is "price_asc" or "price_desc" || minPrice.HasValue || maxPrice.HasValue;

            List<Product> products;
            int totalCount;

            if (needsMemoryOps)
            {
                var all = await query.ToListAsync();

                if (minPrice.HasValue)
                    all = all.Where(p => p.CalculatePrice() >= minPrice.Value).ToList();

                if (maxPrice.HasValue)
                    all = all.Where(p => p.CalculatePrice() <= maxPrice.Value).ToList();

                var ordered = sort switch
                {
                    "price_asc"  => all.OrderBy(p => p.CalculatePrice()),
                    "price_desc" => all.OrderByDescending(p => p.CalculatePrice()),
                    "name"       => all.OrderBy(p => p.Name).AsEnumerable(),
                    "oldest"     => all.OrderBy(p => p.CreatedAt).AsEnumerable(),
                    _            => all.OrderByDescending(p => p.CreatedAt).AsEnumerable()
                };

                totalCount = all.Count;
                products   = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            }
            else
            {
                query = sort switch
                {
                    "name"   => query.OrderBy(p => p.Name),
                    "oldest" => query.OrderBy(p => p.CreatedAt),
                    _        => query.OrderByDescending(p => p.CreatedAt)
                };

                totalCount = await query.CountAsync();
                products   = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            }

            var wishlisted = await GetWishlistedProductIdsAsync();
            var pricing    = await _pricing.GetEffectivePricesAsync(products.Select(p => p.Id));

            var vm = new CollectionsViewModel
            {
                Products    = products.Select(p => ProductCardViewModel.FromProduct(p, wishlisted.Contains(p.Id), pricing.GetValueOrDefault(p.Id))).ToList(),
                Categories  = await Db.Categories.Where(c => c.IsActive && !c.IsDeleted).OrderBy(c => c.DisplayOrder).ToListAsync(),
                Materials   = await Db.Materials.OrderBy(m => m.Name).ToListAsync(),
                Search      = search,
                CategoryIds = categoryIds ?? new List<int>(),
                MaterialIds = materialIds ?? new List<int>(),
                MinPrice    = minPrice,
                MaxPrice    = maxPrice,
                Sort        = sort,
                Page        = page,
                PageSize    = pageSize,
                TotalCount  = totalCount
            };

            return View(vm);
        }

        [HttpGet]
        [Route("Collections/Details/{slug}")]
        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();

            var product = await Db.Products
                .Where(p => p.Slug == slug && !p.IsDeleted)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.ProductColors).ThenInclude(pc => pc.Color)
                .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
                .FirstOrDefaultAsync();

            if (product == null) return NotFound();

            var wishlisted    = await GetWishlistedProductIdsAsync();

            var related = await Db.Products
                .Where(p => !p.IsDeleted && p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
                .OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            var reviews = await Db.ProductReviews
                .Include(r => r.User)
                .Where(r => r.ProductId == product.Id && r.IsApproved && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            var hasReviewed = CurrentUserId != null && await Db.ProductReviews.AnyAsync(r => r.ProductId == product.Id && r.UserId == CurrentUserId && !r.IsDeleted);

            // A user can review if they are signed in, have purchased the item, and haven't reviewed it yet
            var canReview = false;
            if (CurrentUserId != null && !hasReviewed)
            {
                canReview = await Db.OrderItems
                    .Include(oi => oi.Order)
                    .AnyAsync(oi => oi.ProductId == product.Id && oi.Order.UserId == CurrentUserId && oi.Order.Status == OrderStatus.Delivered);
            }

            // Pricing — main product + related products
            var allIds     = new[] { product.Id }.Concat(related.Select(p => p.Id));
            var pricing    = await _pricing.GetEffectivePricesAsync(allIds);
            var mainPricing = pricing.GetValueOrDefault(product.Id);

            var vm = new ProductDetailsViewModel
            {
                Product        = product,
                Price          = mainPricing?.EffectivePrice ?? product.CalculatePrice(),
                OriginalPrice  = product.CalculatePrice(),
                DiscountAmount = mainPricing?.DiscountAmount ?? 0m,
                DiscountBadge  = mainPricing?.DiscountBadge,
                Gallery    = product.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).ToList(),
                Colors     = product.ProductColors.Where(pc => pc.Color.IsActive).Select(pc => new Threadelle.ViewModels.ProductDetailsColorViewModel { Id = pc.Color.Id, Name = pc.Color.Name, HexCode = pc.Color.HexCode, Quantity = pc.Quantity }).ToList(),
                Materials  = product.ProductMaterials.Select(pm => pm.Material).ToList(),
                RelatedProducts = related.Select(p => ProductCardViewModel.FromProduct(p, wishlisted.Contains(p.Id), pricing.GetValueOrDefault(p.Id))).ToList(),
                InStock = product.IsActive && (product.IsOnePiece ? product.Quantity >= 1 : product.Quantity > 0),
                IsWishlisted = wishlisted.Contains(product.Id),
                Reviews = reviews,
                AverageRating = averageRating,
                HasReviewed = hasReviewed,
                CanReview = canReview
            };

            return View(vm);
        }
    }
}
