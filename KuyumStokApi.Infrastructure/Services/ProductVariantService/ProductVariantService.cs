using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.ProductVariant.KuyumStokApi.Application.DTOs.ProductVariants;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace KuyumStokApi.Infrastructure.Services.ProductVariantService
{
    /// <summary>Ürün varyantı işlemleri servisi (model odaklı).</summary>
    public sealed class ProductVariantService : IProductVariantService
    {
        private readonly AppDbContext _db;
        public ProductVariantService(AppDbContext db) => _db = db;

        public async Task<ApiResult<PagedResult<ProductVariantDto>>> GetPagedAsync(ProductVariantFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            IQueryable<Domain.Entities.ProductVariants> q = _db.ProductVariants.AsNoTracking();

            if (filter.IncludeDeleted) q = q.IgnoreQueryFilters();

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                q = q.Where(v =>
                    (v.Name  != null && EF.Functions.ILike(v.Name,  $"%{qstr}%")) ||
                    (v.Brand != null && EF.Functions.ILike(v.Brand, $"%{qstr}%")) ||
                    (v.Ayar  != null && EF.Functions.ILike(v.Ayar,  $"%{qstr}%")) ||
                    (v.Color != null && EF.Functions.ILike(v.Color, $"%{qstr}%"))
                );
            }

            if (filter.ProductTypeId is not null)
                q = q.Where(v => v.ProductTypeId == filter.ProductTypeId);

            if (filter.IsActive is not null)
                q = q.Where(v => v.IsActive == filter.IsActive);

            if (filter.UpdatedFromUtc is not null)
                q = q.Where(v => v.UpdatedAt == null || v.UpdatedAt >= filter.UpdatedFromUtc);

            if (filter.UpdatedToUtc is not null)
                q = q.Where(v => v.UpdatedAt == null || v.UpdatedAt <= filter.UpdatedToUtc);

            var total = await q.LongCountAsync(ct);

            var items = await q
                .Include(v => v.ProductType)!.ThenInclude(t => t.Category)
                .OrderByDescending(v => v.UpdatedAt ?? DateTime.MinValue)
                .ThenBy(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    Name = v.Name,
                    Ayar = v.Ayar,
                    Color = v.Color,
                    Brand = v.Brand,
                    CreatedAt = v.CreatedAt,
                    UpdatedAt = v.UpdatedAt,
                    IsActive = v.IsActive,
                    IsDeleted = v.IsDeleted,
                    ProductType = new ProductVariantDto.ProductTypeBrief
                    {
                        Id = v.ProductTypeId,
                        Name = v.ProductType != null ? v.ProductType.Name : null,
                        CategoryId = v.ProductType != null ? v.ProductType.CategoryId : null,
                        CategoryName = v.ProductType != null && v.ProductType.Category != null
                                        ? v.ProductType.Category.Name
                                        : null
                    }
                })
                .ToListAsync(ct);


            return ApiResult<PagedResult<ProductVariantDto>>.Ok(
                new PagedResult<ProductVariantDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total },
                "Liste", 200
            );
        }

        public async Task<ApiResult<ProductVariantDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var v = await _db.ProductVariants.AsNoTracking().IgnoreQueryFilters()
                        .Include(x => x.ProductType)!.ThenInclude(t => t.Category)
                        .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (v is null)
                return ApiResult<ProductVariantDto>.Fail("Varyant bulunamadı", statusCode: 404);

            var dto = new ProductVariantDto
            {
                Id = v.Id,
                Name = v.Name,
                Ayar = v.Ayar,
                Color = v.Color,
                Brand = v.Brand,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt,
                IsActive = v.IsActive,
                IsDeleted = v.IsDeleted,
                ProductType = new ProductVariantDto.ProductTypeBrief
                {
                    Id = v.ProductTypeId,
                    Name = v.ProductType?.Name,
                    CategoryId = v.ProductType?.CategoryId,
                    CategoryName = v.ProductType?.Category?.Name
                }
            };

            return ApiResult<ProductVariantDto>.Ok(dto, "Bulundu", 200);
        }

        public async Task<ApiResult<ProductVariantDto>> CreateAsync(ProductVariantCreateDto dto, CancellationToken ct = default)
        {
            if (dto.ProductTypeId is null || dto.ProductTypeId <= 0)
                return ApiResult<ProductVariantDto>.Fail("ProductTypeId zorunludur.", statusCode: 400);
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResult<ProductVariantDto>.Fail("Name (model) zorunludur.", statusCode: 400);

            // unique: (type, name, brand, ayar, color)
            var exists = await _db.ProductVariants.AnyAsync(v =>
                v.ProductTypeId == dto.ProductTypeId &&
                v.Name == dto.Name &&
                v.Brand == dto.Brand &&
                v.Ayar == dto.Ayar &&
                v.Color == dto.Color, ct);

            if (exists)
                return ApiResult<ProductVariantDto>.Fail("Aynı tür/model/marka/ayar/renk zaten mevcut.", statusCode: 409);

            var now = DateTime.UtcNow;

            var entity = new Domain.Entities.ProductVariants
            {
                ProductTypeId = dto.ProductTypeId,
                Name = dto.Name,
                Ayar = dto.Ayar,
                Color = dto.Color,
                Brand = dto.Brand,
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            };

            _db.ProductVariants.Add(entity);
            await _db.SaveChangesAsync(ct);

            var created = await GetByIdAsync(entity.Id, ct);
            created.StatusCode = 201;
            created.Message = "Oluşturuldu";
            return created;
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, ProductVariantUpdateDto dto, CancellationToken ct = default)
        {
            var entity = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Varyant bulunamadı", statusCode: 404);

            if (dto.ProductTypeId is null || dto.ProductTypeId <= 0)
                return ApiResult<bool>.Fail("ProductTypeId zorunludur.", statusCode: 400);
            if (string.IsNullOrWhiteSpace(dto.Name))
                return ApiResult<bool>.Fail("Name (model) zorunludur.", statusCode: 400);

            // unique çakışma kontrolü
            var conflict = await _db.ProductVariants.AnyAsync(v =>
                v.Id != id &&
                v.ProductTypeId == dto.ProductTypeId &&
                v.Name == dto.Name &&
                v.Brand == dto.Brand &&
                v.Ayar == dto.Ayar &&
                v.Color == dto.Color, ct);

            if (conflict)
                return ApiResult<bool>.Fail("Aynı tür/model/marka/ayar/renk kombinasyonu zaten var.", statusCode: 409);

            entity.ProductTypeId = dto.ProductTypeId;
            entity.Name = dto.Name;
            entity.Ayar = dto.Ayar;
            entity.Color = dto.Color;
            entity.Brand = dto.Brand;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        // Soft delete + bağlılık guard
        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Varyant bulunamadı", statusCode: 404);

            var hasStocks = await _db.Stocks.AnyAsync(s => s.ProductVariantId == id, ct);
            if (hasStocks)
                return ApiResult<bool>.Fail("Bu varyanta bağlı stok kayıtları var. Önce stokları temizleyin.", statusCode: 409);

            var hasLimits = await _db.Limits.AnyAsync(l => l.ProductVariantId == id, ct);
            if (hasLimits)
                return ApiResult<bool>.Fail("Bu varyanta bağlı limit kayıtları var. Önce limitleri temizleyin.", statusCode: 409);

            _db.ProductVariants.Remove(entity); // mevcut soft-delete davranışınla uyumlu
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi (soft)", 200);
        }

        public async Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default)
        {
            var anyStock = await _db.Stocks.IgnoreQueryFilters().AnyAsync(s => s.ProductVariantId == id, ct);
            var anyLimit = await _db.Limits.IgnoreQueryFilters().AnyAsync(l => l.ProductVariantId == id, ct);
            if (anyStock || anyLimit)
                return ApiResult<bool>.Fail("Bağlı kayıtlar bulunduğundan kalıcı silme yapılamaz.", statusCode: 409);

            var affected = await _db.ProductVariants.IgnoreQueryFilters()
                              .Where(v => v.Id == id)
                              .ExecuteDeleteAsync(ct);

            return affected == 1
                ? ApiResult<bool>.Ok(true, "Kalıcı silindi", 200)
                : ApiResult<bool>.Fail("Varyant bulunamadı", statusCode: 404);
        }

        public async Task<ApiResult<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
        {
            var entity = await _db.ProductVariants.IgnoreQueryFilters().FirstOrDefaultAsync(v => v.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Varyant bulunamadı", statusCode: 404);

            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, isActive ? "Aktif edildi" : "Pasif edildi", 200);
        }
    }
}
