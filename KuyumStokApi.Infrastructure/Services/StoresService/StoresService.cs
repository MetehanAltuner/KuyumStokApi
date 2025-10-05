using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Stores;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.StoresService
{
    /// <summary>Mağaza servis implementasyonu.</summary>
    public sealed class StoresService : IStoresService
    {
        private readonly AppDbContext _db;
        public StoresService(AppDbContext db) => _db = db;

        public async Task<ApiResult<PagedResult<StoreDto>>> GetPagedAsync(StoreFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            IQueryable<Domain.Entities.Stores> q = _db.Stores.AsNoTracking();

            if (filter.IncludeDeleted) q = q.IgnoreQueryFilters();

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                q = q.Where(s => EF.Functions.ILike(s.Name!, $"%{qstr}%"));
            }

            if (filter.IsActive is not null)
                q = q.Where(s => s.IsActive == filter.IsActive);

            if (filter.UpdatedFromUtc is not null)
                q = q.Where(s => s.UpdatedAt == null || s.UpdatedAt >= filter.UpdatedFromUtc);

            if (filter.UpdatedToUtc is not null)
                q = q.Where(s => s.UpdatedAt == null || s.UpdatedAt <= filter.UpdatedToUtc);

            var total = await q.LongCountAsync(ct);

            // Şube sayısı için ayrı sorgu: projeksiyonda subquery (performans için ok)
            var items = await q
                .OrderByDescending(s => s.UpdatedAt ?? DateTime.MinValue)
                .ThenBy(s => s.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StoreDto
                {
                    Id = s.Id,
                    Name = s.Name!,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    IsActive = s.IsActive,
                    IsDeleted = s.IsDeleted,
                    BranchCount = _db.Branches.Count(b => b.StoreId == s.Id && !b.IsDeleted)
                })
                .ToListAsync(ct);

            return ApiResult<PagedResult<StoreDto>>.Ok(
                new PagedResult<StoreDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total },
                "Liste", 200);
        }

        public async Task<ApiResult<StoreDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var s = await _db.Stores.AsNoTracking().IgnoreQueryFilters()
                        .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (s is null) return ApiResult<StoreDto>.Fail("Mağaza bulunamadı", statusCode: 404);

            var dto = new StoreDto
            {
                Id = s.Id,
                Name = s.Name!,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                IsActive = s.IsActive,
                IsDeleted = s.IsDeleted,
                BranchCount = await _db.Branches.CountAsync(b => b.StoreId == s.Id && !b.IsDeleted, ct)
            };
            return ApiResult<StoreDto>.Ok(dto, "Bulundu", 200);
        }

        public async Task<ApiResult<StoreDto>> CreateAsync(StoreCreateDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            // isim eşsizliği istersen aç
            var dup = await _db.Stores.AnyAsync(x => x.Name == dto.Name, ct);
            if (dup) return ApiResult<StoreDto>.Fail("Bu isimde mağaza zaten var.", statusCode: 409);

            var entity = new Domain.Entities.Stores
            {
                Name = dto.Name.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            };

            _db.Stores.Add(entity);
            await _db.SaveChangesAsync(ct);

            var created = await GetByIdAsync(entity.Id, ct);
            created.StatusCode = 201;
            created.Message = "Oluşturuldu";
            return created;
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, StoreUpdateDto dto, CancellationToken ct = default)
        {
            var entity = await _db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Mağaza bulunamadı", statusCode: 404);

            // isim çakışması kontrolü (opsiyonel)
            var dup = await _db.Stores.AnyAsync(x => x.Id != id && x.Name == dto.Name, ct);
            if (dup) return ApiResult<bool>.Fail("Bu isimde mağaza zaten var.", statusCode: 409);

            entity.Name = dto.Name.Trim();
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        // Soft delete + guard: Branches varsa 409
        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.Stores.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Mağaza bulunamadı", statusCode: 404);

            var hasBranches = await _db.Branches.AnyAsync(b => b.StoreId == id && !b.IsDeleted, ct);
            if (hasBranches)
                return ApiResult<bool>.Fail("Bu mağazaya bağlı şubeler var. Önce şubeleri silin/pasif edin.", statusCode: 409);

            _db.Stores.Remove(entity); // soft-delete (global hook)
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi (soft)", 200);
        }

        // Hard delete: bağlı şube yoksa
        public async Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default)
        {
            var anyBranch = await _db.Branches.IgnoreQueryFilters().AnyAsync(b => b.StoreId == id, ct);
            if (anyBranch)
                return ApiResult<bool>.Fail("Bağlı şubeler bulunduğundan kalıcı silme yapılamaz.", statusCode: 409);

            var affected = await _db.Stores.IgnoreQueryFilters()
                              .Where(x => x.Id == id)
                              .ExecuteDeleteAsync(ct);

            return affected == 1
                ? ApiResult<bool>.Ok(true, "Kalıcı silindi", 200)
                : ApiResult<bool>.Fail("Mağaza bulunamadı", statusCode: 404);
        }

        public async Task<ApiResult<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
        {
            var entity = await _db.Stores.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Mağaza bulunamadı", statusCode: 404);

            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return ApiResult<bool>.Ok(true, isActive ? "Aktif edildi" : "Pasif edildi", 200);
        }
    }
}
