using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Helpers;
using Threadelle.Models;
using Threadelle.ViewModels.Admin;
using Treadelle.Interfaces;

namespace Threadelle.Services
{
    public class CategoryService : ICategoryService
    {
        private const string UploadUrlPrefix = "/img/upload/";

        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IImageService _images;

        public CategoryService(ApplicationDbContext db, IMapper mapper, IImageService images)
        {
            _db = db;
            _mapper = mapper;
            _images = images;
        }

        public async Task<List<CategoryListItemViewModel>> GetListAsync()
        {
            return await _db.Categories
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.ParentCategoryId == null)   // main categories first
                .ThenBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ProjectTo<CategoryListItemViewModel>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<CategoryUpsertViewModel> BuildCreateAsync()
        {
            return new CategoryUpsertViewModel
            {
                IsActive = true,
                ParentOptions = await GetParentOptionsAsync(null)
            };
        }

        public async Task<CategoryUpsertViewModel?> GetForEditAsync(int id)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (category == null) return null;

            var vm = _mapper.Map<CategoryUpsertViewModel>(category);
            vm.ParentOptions = await GetParentOptionsAsync(id);
            return vm;
        }

        public async Task<CategoryDetailsViewModel?> GetDetailsAsync(int id)
        {
            var category = await _db.Categories
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (category == null) return null;

            var children = await _db.Categories
                .Where(c => c.ParentCategoryId == id && !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
                .ProjectTo<CategoryListItemViewModel>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var products = await _db.Products
                .Where(p => p.CategoryId == id && !p.IsDeleted)
                .Include(p => p.ProductMaterials).ThenInclude(pm => pm.Material)
                .Include(p => p.Images)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return new CategoryDetailsViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                ImageUrl = category.ImageUrl,
                IsActive = category.IsActive,
                ParentName = category.ParentCategory?.Name,
                ChildCount = children.Count,
                ProductCount = products.Count,
                Children = children,
                Products = products.Select(p => new CategoryProductRow
                {
                    Id = p.Id,
                    Name = p.Name,
                    Slug = p.Slug,
                    Price = p.CalculatePrice(),
                    Quantity = p.Quantity,
                    IsActive = p.IsActive,
                    ImageUrl = p.PrimaryImageUrl()
                }).ToList()
            };
        }

        public async Task<IEnumerable<SelectListItem>> GetParentOptionsAsync(int? excludeId)
        {
            return await _db.Categories
                .Where(c => !c.IsDeleted && c.ParentCategoryId == null && (excludeId == null || c.Id != excludeId))
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();
        }

        public async Task<int> UpsertAsync(CategoryUpsertViewModel vm)
        {
            // A sub-category cannot itself be a parent (we offer a flat main → sub hierarchy).
            if (vm.ParentCategoryId.HasValue)
            {
                var parentIsMain = await _db.Categories
                    .AnyAsync(c => c.Id == vm.ParentCategoryId.Value && c.ParentCategoryId == null && !c.IsDeleted);
                if (!parentIsMain) vm.ParentCategoryId = null;
            }

            if (vm.Id == 0)
            {
                var entity = _mapper.Map<Category>(vm);
                entity.Slug = await UniqueSlugAsync(vm.Name, 0);
                entity.ImageUrl = await SaveImageAsync(vm.ImageFile) ?? null;

                _db.Categories.Add(entity);
                await _db.SaveChangesAsync();
                return entity.Id;
            }
            else
            {
                var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == vm.Id && !c.IsDeleted)
                             ?? throw new InvalidOperationException("Category not found.");

                _mapper.Map(vm, entity);                          // copies scalar fields onto the tracked entity
                entity.Slug = await UniqueSlugAsync(vm.Name, entity.Id);

                var newImage = await SaveImageAsync(vm.ImageFile);
                if (newImage != null)
                {
                    DeleteImageFile(entity.ImageUrl);
                    entity.ImageUrl = newImage;
                }

                await _db.SaveChangesAsync();
                return entity.Id;
            }
        }

        public async Task<(bool ok, string message)> DeleteAsync(int id, string? deletedBy = null)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (category == null) return (false, "Category not found.");

            var hasChildren = await _db.Categories.AnyAsync(c => c.ParentCategoryId == id && !c.IsDeleted);
            if (hasChildren) return (false, "Remove or move its sub-categories first.");

            var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id && !p.IsDeleted);
            if (hasProducts) return (false, "This category still has products. Move or remove them first.");

            category.IsDeleted = true;
            category.DeletedAt = DateTime.UtcNow;
            category.DeletedBy = deletedBy;
            await _db.SaveChangesAsync();
            return (true, "Category moved to recycle bin.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private async Task<string> UniqueSlugAsync(string name, int excludeId)
        {
            var baseSlug = SlugHelper.Slugify(name);
            var slug = baseSlug;
            var i = 2;
            // Check ALL rows (the unique index spans soft-deleted rows too).
            while (await _db.Categories.AnyAsync(c => c.Slug == slug && c.Id != excludeId))
                slug = $"{baseSlug}-{i++}";
            return slug;
        }

        private async Task<string?> SaveImageAsync(Microsoft.AspNetCore.Http.IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            var fileName = await _images.UploadImage(file);
            return UploadUrlPrefix + fileName;
        }

        private void DeleteImageFile(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            _images.DeleteImage(Path.GetFileName(imageUrl));
        }
    }
}
