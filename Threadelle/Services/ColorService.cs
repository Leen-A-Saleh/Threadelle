using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Models;
using Threadelle.ViewModels.Admin;

namespace Threadelle.Services
{
    public class ColorService : IColorService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public ColorService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<ColorListItemViewModel>> GetListAsync() =>
            await _db.Colors
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ProjectTo<ColorListItemViewModel>(_mapper.ConfigurationProvider)
                .ToListAsync();

        public async Task<ColorUpsertViewModel?> GetForEditAsync(int id)
        {
            var color = await _db.Colors.FindAsync(id);
            return color == null ? null : _mapper.Map<ColorUpsertViewModel>(color);
        }

        public async Task<int> UpsertAsync(ColorUpsertViewModel vm)
        {
            // Normalise hex to uppercase
            if (!string.IsNullOrWhiteSpace(vm.HexCode))
                vm.HexCode = vm.HexCode.Trim().ToUpperInvariant();

            if (vm.Id == 0)
            {
                var entity = _mapper.Map<Color>(vm);
                _db.Colors.Add(entity);
                await _db.SaveChangesAsync();
                return entity.Id;
            }

            var existing = await _db.Colors.FindAsync(vm.Id)
                           ?? throw new InvalidOperationException("Color not found.");
            _mapper.Map(vm, existing);
            await _db.SaveChangesAsync();
            return existing.Id;
        }

        public async Task<(bool ok, string message)> DeleteAsync(int id, string? deletedBy = null)
        {
            var color = await _db.Colors.FindAsync(id);
            if (color == null) return (false, "Color not found.");

            color.IsDeleted = true;
            color.DeletedAt = DateTime.UtcNow;
            color.DeletedBy = deletedBy;
            await _db.SaveChangesAsync();
            return (true, "Color moved to recycle bin.");
        }

        public async Task<(bool ok, string message)> RestoreAsync(int id)
        {
            var color = await _db.Colors.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted);
            if (color == null) return (false, "Color not found in recycle bin.");
            color.IsDeleted = false;
            color.DeletedAt = null;
            color.DeletedBy = null;
            await _db.SaveChangesAsync();
            return (true, "Color restored.");
        }

        public async Task<(bool ok, string message)> PermanentDeleteAsync(int id)
        {
            var color = await _db.Colors.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id && c.IsDeleted);
            if (color == null) return (false, "Color not found in recycle bin.");

            var usedByProduct = await _db.ProductColors.AnyAsync(pc => pc.ColorId == id);
            if (usedByProduct) return (false, "This color is assigned to one or more products and cannot be permanently deleted.");
            var usedByCustom = await _db.CustomOrderColors.AnyAsync(cc => cc.ColorId == id);
            if (usedByCustom) return (false, "This color is referenced in custom orders and cannot be permanently deleted.");

            _db.Colors.Remove(color);
            await _db.SaveChangesAsync();
            return (true, "Color permanently deleted.");
        }
    }
}
