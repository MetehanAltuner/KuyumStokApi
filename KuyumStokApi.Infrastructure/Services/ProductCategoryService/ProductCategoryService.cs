using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using KuyumStokApi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KuyumStokApi.Application.DTOs.ProductCategory;

namespace KuyumStokApi.Infrastructure.Services.ProductCategoryService
{
    public sealed class ProductCategoryService : IProductCategoryService
    {
        private readonly AppDbContext _db;
        public ProductCategoryService(AppDbContext db) => _db = db;

        // Paged + filtreli liste
        public async Task<ApiResult<PagedResult<ProductCategoryDto>>> GetPagedAsync(
            ProductCategoryFilter filter,
            CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            IQueryable<Domain.Entities.ProductCategories> q = _db.ProductCategories.AsNoTracking();

            if (filter.IncludeDeleted)
                q = q.IgnoreQueryFilters();

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                q = q.Where(x => EF.Functions.ILike(x.Name!, $"%{qstr}%"));
            }

            if (filter.IsActive is not null)
                q = q.Where(x => x.IsActive == filter.IsActive);

            if (filter.UpdatedFromUtc is not null)
                q = q.Where(x => x.UpdatedAt == null || x.UpdatedAt >= filter.UpdatedFromUtc);

            if (filter.UpdatedToUtc is not null)
                q = q.Where(x => x.UpdatedAt == null || x.UpdatedAt <= filter.UpdatedToUtc);

            var total = await q.LongCountAsync(ct);

            var items = await q
                .OrderByDescending(x => x.UpdatedAt ?? DateTime.MinValue)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ProductCategoryDto
                {
                    Id = x.Id,
                    Name = x.Name!,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    IsActive = x.IsActive,
                    IsDeleted = x.IsDeleted
                })
                .ToListAsync(ct);

            return ApiResult<PagedResult<ProductCategoryDto>>.Ok(
                new PagedResult<ProductCategoryDto>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = total
                }, "Liste", 200);
        }

        public async Task<ApiResult<ProductCategoryDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var x = await _db.ProductCategories.AsNoTracking().IgnoreQueryFilters()
                                               .FirstOrDefaultAsync(p => p.Id == id, ct);
            if (x is null)
                return ApiResult<ProductCategoryDto>.Fail("Kategori bulunamadı", statusCode: 404);

            var dto = new ProductCategoryDto
            {
                Id = x.Id,
                Name = x.Name!,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted
            };
            return ApiResult<ProductCategoryDto>.Ok(dto, "Bulundu", 200);
        }

        public async Task<ApiResult<ProductCategoryDto>> CreateAsync(ProductCategoryCreateDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            // örnek: aynı isimden varsa engelle
            var exists = await _db.ProductCategories.AnyAsync(x => x.Name == dto.Name, ct);
            if (exists) return ApiResult<ProductCategoryDto>.Fail("Bu isimde kategori zaten var.");

            var entity = new Domain.Entities.ProductCategories
            {
                Name = dto.Name.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            };

            _db.ProductCategories.Add(entity);
            await _db.SaveChangesAsync(ct);

            var created = new ProductCategoryDto
            {
                Id = entity.Id,
                Name = entity.Name!,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                IsActive = entity.IsActive,
                IsDeleted = entity.IsDeleted
            };
            return ApiResult<ProductCategoryDto>.Ok(created, "Oluşturuldu", 201);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, ProductCategoryUpdateDto dto, CancellationToken ct = default)
        {
            var entity = await _db.ProductCategories.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Kategori bulunamadı", statusCode: 404);

            entity.Name = dto.Name.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        // Soft delete (DbContext hook devreye girer)
        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.ProductCategories.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Kategori bulunamadı", statusCode: 404);

            _db.ProductCategories.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi (soft)", 200);
        }

        // Kalıcı silme
        public async Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default)
        {
            var affected = await _db.ProductCategories.IgnoreQueryFilters()
                               .Where(x => x.Id == id)
                               .ExecuteDeleteAsync(ct);
            return affected == 1
                ? ApiResult<bool>.Ok(true, "Kalıcı silindi", 200)
                : ApiResult<bool>.Fail("Kategori bulunamadı", statusCode: 404);
        }

        public async Task<ApiResult<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
        {
            var entity = await _db.ProductCategories.IgnoreQueryFilters()
                              .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Kategori bulunamadı", statusCode: 404);

            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, isActive ? "Aktif edildi" : "Pasif edildi", 200);
        }
    }
}
