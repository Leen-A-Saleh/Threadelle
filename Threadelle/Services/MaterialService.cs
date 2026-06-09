using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Threadelle.Data;
using Threadelle.Models;
using Threadelle.ViewModels.Admin;

namespace Threadelle.Services
{
    public class MaterialService : IMaterialService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public MaterialService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<List<MaterialListItemViewModel>> GetListAsync()
        {
            return await _db.Materials
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.Name)
                .ProjectTo<MaterialListItemViewModel>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<MaterialUpsertViewModel?> GetForEditAsync(int id)
        {
            var material = await _db.Materials.FirstOrDefaultAsync(m => m.Id == id);
            return material == null ? null : _mapper.Map<MaterialUpsertViewModel>(material);
        }

        public async Task<int> UpsertAsync(MaterialUpsertViewModel vm)
        {
            if (vm.Id == 0)
            {
                var entity = _mapper.Map<Material>(vm);
                _db.Materials.Add(entity);
                await _db.SaveChangesAsync();
                return entity.Id;
            }

            var existing = await _db.Materials.FirstOrDefaultAsync(m => m.Id == vm.Id)
                           ?? throw new InvalidOperationException("Material not found.");
            _mapper.Map(vm, existing);
            await _db.SaveChangesAsync();
            return existing.Id;
        }

        public async Task<(bool ok, string message)> DeleteAsync(int id, string? deletedBy = null)
        {
            var material = await _db.Materials.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (material == null) return (false, "Material not found.");

            material.IsDeleted = true;
            material.DeletedAt = DateTime.UtcNow;
            material.DeletedBy = deletedBy;
            await _db.SaveChangesAsync();
            return (true, "Material moved to recycle bin.");
        }

        public async Task<(bool ok, string message)> RestoreAsync(int id)
        {
            var material = await _db.Materials.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == id && m.IsDeleted);
            if (material == null) return (false, "Material not found in recycle bin.");
            material.IsDeleted = false;
            material.DeletedAt = null;
            material.DeletedBy = null;
            await _db.SaveChangesAsync();
            return (true, "Material restored.");
        }

        public async Task<(bool ok, string message)> PermanentDeleteAsync(int id)
        {
            var material = await _db.Materials.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == id && m.IsDeleted);
            if (material == null) return (false, "Material not found in recycle bin.");

            var inUse = await _db.ProductMaterials.AnyAsync(pm => pm.MaterialId == id);
            if (inUse) return (false, "This material is used by products and cannot be permanently deleted.");

            _db.Materials.Remove(material);
            await _db.SaveChangesAsync();
            return (true, "Material permanently deleted.");
        }
    }
}
