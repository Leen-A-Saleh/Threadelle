using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Helpers;
using Threadelle.Models;
using Threadelle.ViewModels.Admin;
using Treadelle.Interfaces;

namespace Threadelle.Services
{
    public class ProductService : IProductService
    {
        private const int PageSize = 12;
        private const string UploadUrlPrefix = "/img/upload/";

        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IImageService _images;

        public ProductService(ApplicationDbContext db, IMapper mapper, IImageService images)
        {
            _db = db;
            _mapper = mapper;
            _images = images;
        }

        // ── Index ─────────────────────────────────────────────────────────────
        public async Task<ProductIndexViewModel> GetIndexAsync(string? search, int? categoryId, string? type, int page)
        {
            if (page < 1) page = 1;

            var q = _db.Products.Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(p => p.Name.Contains(search));
            if (categoryId.HasValue)
                q = q.Where(p => p.CategoryId == categoryId.Value);
            if (type == "onepiece")
                q = q.Where(p => p.IsOnePiece);
            else if (type == "standard")
                q = q.Where(p => !p.IsOnePiece);

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * PageSize).Take(PageSize)
                .Select(p => new ProductListItemViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    CategoryName = p.Category.Name,
                    Price = p.WorkHours * p.HourRate
                            + p.ProductMaterials.Sum(pm => pm.QuantityUsed * pm.Material.PricePerUnit),
                    Quantity = p.Quantity,
                    IsOnePiece = p.IsOnePiece,
                    IsFeatured = p.IsFeatured,
                    IsActive = p.IsActive,
                    PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                                      ?? p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault(),
                    CreatedAt = p.CreatedAt,
                    Label = p.Label
                })
                .ToListAsync();

            return new ProductIndexViewModel
            {
                Items = items,
                TotalCount = total,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(total / (double)PageSize),
                Search = search,
                CategoryId = categoryId,
                Type = type,
                CategoryOptions = await CategoryOptionsAsync(categoryId)
            };
        }

        // ── Build / Edit ──────────────────────────────────────────────────────
        public async Task<ProductUpsertViewModel> BuildCreateAsync()
        {
            var vm = new ProductUpsertViewModel { IsActive = true, Quantity = 1 };
            await PopulateOptionsAsync(vm);
            return vm;
        }

        public async Task<ProductUpsertViewModel?> GetForEditAsync(int id)
        {
            var product = await _db.Products
                .Include(p => p.Images)
                .Include(p => p.ProductColors)
                .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (product == null) return null;

            var vm = _mapper.Map<ProductUpsertViewModel>(product);

            vm.ExistingImages = product.Images
                .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder)
                .Select(i => new ProductImageRowViewModel
                {
                    Id = i.Id, ImageUrl = i.ImageUrl, IsPrimary = i.IsPrimary, DisplayOrder = i.DisplayOrder, AltText = i.AltText
                }).ToList();

            vm.MaterialRows = product.ProductMaterials.Select(pm => new ProductMaterialRowViewModel
            {
                MaterialId = pm.MaterialId,
                MaterialName = pm.Material.Name,
                Unit = pm.Material.Unit,
                PricePerUnit = pm.Material.PricePerUnit,
                QuantityUsed = pm.QuantityUsed
            }).ToList();

            await PopulateOptionsAsync(vm);

            // Populate initial quantities for existing colors
            foreach (var row in vm.ColorRows)
            {
                var pc = product.ProductColors.FirstOrDefault(p => p.ColorId == row.ColorId);
                if (pc != null)
                {
                    row.IsSelected = true;
                    row.Quantity = pc.Quantity;
                }
            }

            return vm;
        }

        public async Task PopulateOptionsAsync(ProductUpsertViewModel vm)
        {
            vm.CategoryOptions = await CategoryOptionsAsync(vm.CategoryId);

            var allColors = await _db.Colors
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToListAsync();

            if (vm.ColorRows == null || !vm.ColorRows.Any())
            {
                vm.ColorRows = allColors.Select(c => new ProductColorRowViewModel
                {
                    ColorId = c.Id, ColorName = c.Name, HexCode = c.HexCode, IsSelected = false, Quantity = 0
                }).ToList();
            }
            else
            {
                foreach (var row in vm.ColorRows)
                {
                    var c = allColors.FirstOrDefault(x => x.Id == row.ColorId);
                    if (c != null) { row.ColorName = c.Name; row.HexCode = c.HexCode; }
                }
            }

            vm.MaterialOptions = await _db.Materials
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.Name)
                .Select(m => new MaterialOptionViewModel { Id = m.Id, Name = m.Name, Unit = m.Unit, PricePerUnit = m.PricePerUnit })
                .ToListAsync();
        }

        // ── Upsert ────────────────────────────────────────────────────────────
        public async Task<int> UpsertAsync(ProductUpsertViewModel vm)
        {
            Product product;

            if (vm.Id == 0)
            {
                product = _mapper.Map<Product>(vm);
                product.Slug = await UniqueSlugAsync(vm.Slug, vm.Name, 0);
                product.CreatedAt = DateTime.UtcNow;
                _db.Products.Add(product);
                await _db.SaveChangesAsync();   // need the Id for child rows
            }
            else
            {
                product = await _db.Products
                    .Include(p => p.ProductColors)
                    .Include(p => p.ProductMaterials)
                    .FirstOrDefaultAsync(p => p.Id == vm.Id && !p.IsDeleted)
                    ?? throw new InvalidOperationException("Product not found.");

                _mapper.Map(vm, product);
                product.Slug = await UniqueSlugAsync(vm.Slug, vm.Name, product.Id);

                // Replace colors & materials wholesale
                _db.ProductColors.RemoveRange(product.ProductColors);
                _db.ProductMaterials.RemoveRange(product.ProductMaterials);
                await _db.SaveChangesAsync();
            }

            // Colors
            foreach (var colorRow in vm.ColorRows.Where(c => c.IsSelected))
                _db.ProductColors.Add(new ProductColor { ProductId = product.Id, ColorId = colorRow.ColorId, Quantity = colorRow.Quantity });

            if (vm.HasColorOptions)
            {
                product.Quantity = vm.ColorRows.Where(c => c.IsSelected).Sum(c => c.Quantity);
            }

            // Materials (only rows with a positive quantity)
            foreach (var row in vm.MaterialRows.Where(r => r.MaterialId > 0 && r.QuantityUsed > 0))
                _db.ProductMaterials.Add(new ProductMaterial
                {
                    ProductId = product.Id,
                    MaterialId = row.MaterialId,
                    QuantityUsed = row.QuantityUsed
                });

            // Existing images updates
            if (vm.ExistingImages != null && vm.ExistingImages.Any())
            {
                var existingDbImages = await _db.ProductImages.Where(pi => pi.ProductId == product.Id).ToListAsync();
                foreach (var dbImg in existingDbImages)
                {
                    var updated = vm.ExistingImages.FirstOrDefault(ei => ei.Id == dbImg.Id);
                    if (updated != null)
                    {
                        dbImg.DisplayOrder = updated.DisplayOrder;
                        dbImg.AltText = updated.AltText;
                    }
                }
            }

            // New images
            await SaveNewImagesAsync(product.Id, vm.NewImages);

            await _db.SaveChangesAsync();
            return product.Id;
        }

        private async Task SaveNewImagesAsync(int productId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0) return;

            var hasPrimary = await _db.ProductImages.AnyAsync(pi => pi.ProductId == productId && pi.IsPrimary);
            var nextOrder = (await _db.ProductImages.Where(pi => pi.ProductId == productId)
                                .MaxAsync(pi => (int?)pi.DisplayOrder) ?? -1) + 1;

            foreach (var file in files)
            {
                if (file == null || file.Length == 0) continue;
                var fileName = await _images.UploadImage(file);
                _db.ProductImages.Add(new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = UploadUrlPrefix + fileName,
                    IsPrimary = !hasPrimary,
                    DisplayOrder = nextOrder++
                });
                hasPrimary = true;
            }
        }

        // ── Delete / image ops / toggles ──────────────────────────────────────
        public async Task<(bool ok, string message)> SoftDeleteAsync(int id, string? deletedBy = null)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
            if (product == null) return (false, "Product not found.");

            product.IsDeleted = true;
            product.DeletedAt = DateTime.UtcNow;
            product.DeletedBy = deletedBy;
            product.IsActive = false;
            await _db.SaveChangesAsync();
            return (true, "Product moved to recycle bin.");
        }

        public async Task<(bool ok, string message)> DeleteImageAsync(int imageId, int productId)
        {
            var img = await _db.ProductImages.FirstOrDefaultAsync(pi => pi.Id == imageId && pi.ProductId == productId);
            if (img == null) return (false, "Image not found.");

            if (img.IsPrimary)
            {
                var next = await _db.ProductImages
                    .Where(pi => pi.ProductId == productId && pi.Id != imageId)
                    .OrderBy(pi => pi.DisplayOrder).FirstOrDefaultAsync();
                if (next != null) next.IsPrimary = true;
            }

            _images.DeleteImage(Path.GetFileName(img.ImageUrl));
            _db.ProductImages.Remove(img);
            await _db.SaveChangesAsync();
            return (true, "Image removed.");
        }

        public async Task<(bool ok, string message)> SetPrimaryImageAsync(int imageId, int productId)
        {
            var images = await _db.ProductImages.Where(pi => pi.ProductId == productId).ToListAsync();
            if (!images.Any(i => i.Id == imageId)) return (false, "Image not found.");
            foreach (var img in images) img.IsPrimary = img.Id == imageId;
            await _db.SaveChangesAsync();
            return (true, "Primary image updated.");
        }

        public async Task<bool?> ToggleFeaturedAsync(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null || product.IsDeleted) return null;
            product.IsFeatured = !product.IsFeatured;
            await _db.SaveChangesAsync();
            return product.IsFeatured;
        }

        public async Task<bool?> ToggleActiveAsync(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null || product.IsDeleted) return null;
            product.IsActive = !product.IsActive;
            await _db.SaveChangesAsync();
            return product.IsActive;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private async Task<IEnumerable<SelectListItem>> CategoryOptionsAsync(int? selected)
        {
            return await _db.Categories
                .Where(c => !c.IsDeleted && c.IsActive)
                .OrderByDescending(c => c.ParentCategoryId == null).ThenBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.ParentCategory != null ? c.ParentCategory.Name + " › " + c.Name : c.Name,
                    Selected = selected.HasValue && c.Id == selected.Value
                })
                .ToListAsync();
        }

        private async Task<string> UniqueSlugAsync(string? provided, string name, int excludeId)
        {
            var baseSlug = SlugHelper.Slugify(string.IsNullOrWhiteSpace(provided) ? name : provided);
            var slug = baseSlug;
            var i = 2;
            while (await _db.Products.AnyAsync(p => p.Slug == slug && p.Id != excludeId))
                slug = $"{baseSlug}-{i++}";
            return slug;
        }
    }
}
