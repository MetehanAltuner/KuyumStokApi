using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.ProductType;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.ProductTypService
{
    public sealed class ProductTypeService : IProductTypeService
    {
        private readonly AppDbContext _db;
        public ProductTypeService(AppDbContext db) => _db = db;

        public async Task<ApiResult<PagedResult<ProductTypeDto>>> GetPagedAsync(ProductTypeFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            IQueryable<Domain.Entities.ProductTypes> q = _db.ProductTypes.AsNoTracking();

            if (filter.IncludeDeleted)
                q = q.IgnoreQueryFilters();

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                q = q.Where(t => EF.Functions.ILike(t.Name!, $"%{qstr}%"));
            }

            if (filter.CategoryId is not null)
                q = q.Where(t => t.CategoryId == filter.CategoryId);

            if (filter.IsActive is not null)
                q = q.Where(t => t.IsActive == filter.IsActive);

            if (filter.UpdatedFromUtc is not null)
                q = q.Where(t => t.UpdatedAt == null || t.UpdatedAt >= filter.UpdatedFromUtc);

            if (filter.UpdatedToUtc is not null)
                q = q.Where(t => t.UpdatedAt == null || t.UpdatedAt <= filter.UpdatedToUtc);

            var total = await q.LongCountAsync(ct);

            var items = await q
                .OrderByDescending(t => t.UpdatedAt ?? DateTime.MinValue)
                .ThenBy(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new ProductTypeDto
                {
                    Id = t.Id,
                    Name = t.Name!,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    IsActive = t.IsActive,
                    IsDeleted = t.IsDeleted,
                    Category = new ProductTypeDto.CategoryBrief
                    {
                        Id = t.CategoryId,
                        Name = t.Category != null ? t.Category.Name : null
                    }
                })
                .ToListAsync(ct);

            return ApiResult<PagedResult<ProductTypeDto>>.Ok(
                new PagedResult<ProductTypeDto>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = total
                }, "Liste", 200);
        }

        public async Task<ApiResult<ProductTypeDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var t = await _db.ProductTypes.AsNoTracking().IgnoreQueryFilters()
                        .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t is null)
                return ApiResult<ProductTypeDto>.Fail("Ürün türü bulunamadı", statusCode: 404);

            var dto = new ProductTypeDto
            {
                Id = t.Id,
                Name = t.Name!,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                IsActive = t.IsActive,
                IsDeleted = t.IsDeleted,
                Category = new ProductTypeDto.CategoryBrief
                {
                    Id = t.CategoryId,
                    Name = t.Category != null ? t.Category.Name : null
                }
            };
            return ApiResult<ProductTypeDto>.Ok(dto, "Bulundu", 200);
        }

        public async Task<ApiResult<ProductTypeDto>> CreateAsync(ProductTypeCreateDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            // Aynı category + name kombinasyonuna karşı basit kontrol (opsiyonel)
            var dup = await _db.ProductTypes.AnyAsync(x => x.Name == dto.Name && x.CategoryId == dto.CategoryId, ct);
            if (dup) return ApiResult<ProductTypeDto>.Fail("Bu isim/kategori kombinasyonu zaten var.", statusCode: 409);

            var entity = new Domain.Entities.ProductTypes
            {
                Name = dto.Name.Trim(),
                CategoryId = dto.CategoryId,
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            };

            _db.ProductTypes.Add(entity);
            await _db.SaveChangesAsync(ct);

            var created = new ProductTypeDto
            {
                Id = entity.Id,
                Name = entity.Name!,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                IsActive = entity.IsActive,
                IsDeleted = entity.IsDeleted,
                Category = new ProductTypeDto.CategoryBrief
                {
                    Id = entity.CategoryId,
                    Name = entity.CategoryId == null ? null :
                           await _db.ProductCategories.AsNoTracking()
                               .Where(c => c.Id == entity.CategoryId)
                               .Select(c => c.Name)
                               .FirstOrDefaultAsync(ct)
                }
            };
            return ApiResult<ProductTypeDto>.Ok(created, "Oluşturuldu", 201);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, ProductTypeUpdateDto dto, CancellationToken ct = default)
        {
            var entity = await _db.ProductTypes.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Ürün türü bulunamadı", statusCode: 404);

            // Duplicate guard (opsiyonel)
            var dup = await _db.ProductTypes
                .AnyAsync(x => x.Id != id && x.Name == dto.Name && x.CategoryId == dto.CategoryId, ct);
            if (dup) return ApiResult<bool>.Fail("Bu isim/kategori kombinasyonu zaten var.", statusCode: 409);

            entity.Name = dto.Name.Trim();
            entity.CategoryId = dto.CategoryId;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        // Soft delete + guard (child: ProductVariants)
        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.ProductTypes.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Ürün türü bulunamadı", statusCode: 404);

            var hasVariants = await _db.ProductVariants.AnyAsync(v => v.ProductTypeId == id && !v.IsDeleted, ct);
            if (hasVariants)
                return ApiResult<bool>.Fail("Bu türe bağlı ürün varyantları var. Önce bağlı kayıtları silin/pasif edin.", statusCode: 409);

            _db.ProductTypes.Remove(entity); // soft-delete’e dönüşür
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi (soft)", 200);
        }

        // Hard delete (child yoksa)
        public async Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default)
        {
            var anyChild = await _db.ProductVariants.IgnoreQueryFilters()
                             .AnyAsync(v => v.ProductTypeId == id, ct);
            if (anyChild)
                return ApiResult<bool>.Fail("Bağlı varyantlar olduğundan kalıcı silme yapılamaz.", statusCode: 409);

            var affected = await _db.ProductTypes.IgnoreQueryFilters()
                             .Where(x => x.Id == id)
                             .ExecuteDeleteAsync(ct);

            return affected == 1
                ? ApiResult<bool>.Ok(true, "Kalıcı silindi", 200)
                : ApiResult<bool>.Fail("Ürün türü bulunamadı", statusCode: 404);
        }

        public async Task<ApiResult<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
        {
            var entity = await _db.ProductTypes.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Ürün türü bulunamadı", statusCode: 404);

            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return ApiResult<bool>.Ok(true, isActive ? "Aktif edildi" : "Pasif edildi", 200);
        }
    }
}
