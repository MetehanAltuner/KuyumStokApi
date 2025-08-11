using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Branches;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.BranchesService
{
    /// <summary>Şube servis implementasyonu.</summary>
    public sealed class BranchesService : IBranchesService
    {
        private readonly AppDbContext _db;
        public BranchesService(AppDbContext db) => _db = db;

        public async Task<ApiResult<PagedResult<BranchDto>>> GetPagedAsync(BranchFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            IQueryable<Domain.Entities.Branches> q = _db.Branches.AsNoTracking();

            if (filter.IncludeDeleted) q = q.IgnoreQueryFilters();

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                q = q.Where(b =>
                    EF.Functions.ILike(b.Name!, $"%{qstr}%") ||
                    EF.Functions.ILike(b.Address ?? "", $"%{qstr}%"));
            }

            if (filter.StoreId is not null)
                q = q.Where(b => b.StoreId == filter.StoreId);

            if (filter.IsActive is not null)
                q = q.Where(b => b.IsActive == filter.IsActive);

            if (filter.UpdatedFromUtc is not null)
                q = q.Where(b => b.UpdatedAt == null || b.UpdatedAt >= filter.UpdatedFromUtc);

            if (filter.UpdatedToUtc is not null)
                q = q.Where(b => b.UpdatedAt == null || b.UpdatedAt <= filter.UpdatedToUtc);

            var total = await q.LongCountAsync(ct);

            var items = await q
                .OrderByDescending(b => b.UpdatedAt ?? DateTime.MinValue)
                .ThenBy(b => b.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BranchDto
                {
                    Id = b.Id,
                    Name = b.Name!,
                    Address = b.Address,
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt,
                    IsActive = b.IsActive,
                    IsDeleted = b.IsDeleted,
                    Store = new BranchDto.StoreBrief
                    {
                        Id = b.StoreId,
                        Name = b.Store != null ? b.Store.Name : null
                    }
                })
                .ToListAsync(ct);

            return ApiResult<PagedResult<BranchDto>>.Ok(
                new PagedResult<BranchDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total },
                "Liste", 200);
        }

        public async Task<ApiResult<BranchDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var b = await _db.Branches.AsNoTracking().IgnoreQueryFilters()
                        .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (b is null) return ApiResult<BranchDto>.Fail("Şube bulunamadı", statusCode: 404);

            var dto = new BranchDto
            {
                Id = b.Id,
                Name = b.Name!,
                Address = b.Address,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                IsActive = b.IsActive,
                IsDeleted = b.IsDeleted,
                Store = new BranchDto.StoreBrief
                {
                    Id = b.StoreId,
                    Name = b.StoreId == null ? null :
                           await _db.Stores.AsNoTracking()
                               .Where(s => s.Id == b.StoreId)
                               .Select(s => s.Name)
                               .FirstOrDefaultAsync(ct)
                }
            };
            return ApiResult<BranchDto>.Ok(dto, "Bulundu", 200);
        }

        public async Task<ApiResult<BranchDto>> CreateAsync(BranchCreateDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var entity = new Domain.Entities.Branches
            {
                Name = dto.Name.Trim(),
                Address = dto.Address,
                StoreId = dto.StoreId,
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            };

            _db.Branches.Add(entity);
            await _db.SaveChangesAsync(ct);

            var created = await GetByIdAsync(entity.Id, ct);
            created.StatusCode = 201;
            created.Message = "Oluşturuldu";
            return created;
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, BranchUpdateDto dto, CancellationToken ct = default)
        {
            var entity = await _db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Şube bulunamadı", statusCode: 404);

            entity.Name = dto.Name.Trim();
            entity.Address = dto.Address;
            entity.StoreId = dto.StoreId;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        // Soft delete + bağlılık guard’ları
        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.Branches.FirstOrDefaultAsync(b => b.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Şube bulunamadı", statusCode: 404);

            var hasUsers = await _db.Users.AnyAsync(u => u.BranchId == id /* && !u.IsDeleted */, ct);
            var hasStocks = await _db.Stocks.AnyAsync(s => s.BranchId == id, ct);
            var hasSales = await _db.Sales.AnyAsync(s => s.BranchId == id, ct);
            var hasPurchases = await _db.Purchases.AnyAsync(p => p.BranchId == id, ct);

            if (hasUsers || hasStocks || hasSales || hasPurchases)
                return ApiResult<bool>.Fail("Şube bağlı kayıtlar içeriyor (kullanıcı/stok/satış/alış). Silinemez.", statusCode: 409);

            _db.Branches.Remove(entity); // soft-delete’e dönüşür (global hook)
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi (soft)", 200);
        }

        // Hard delete (bağlı kayıt yoksa)
        public async Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default)
        {
            var hasAny =
                await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.BranchId == id, ct) ||
                await _db.Stocks.AnyAsync(s => s.BranchId == id, ct) ||
                await _db.Sales.AnyAsync(s => s.BranchId == id, ct) ||
                await _db.Purchases.AnyAsync(p => p.BranchId == id, ct);

            if (hasAny)
                return ApiResult<bool>.Fail("Bağlı kayıtlar nedeniyle kalıcı silme yapılamaz.", statusCode: 409);

            var affected = await _db.Branches.IgnoreQueryFilters()
                              .Where(x => x.Id == id)
                              .ExecuteDeleteAsync(ct);

            return affected == 1
                ? ApiResult<bool>.Ok(true, "Kalıcı silindi", 200)
                : ApiResult<bool>.Fail("Şube bulunamadı", statusCode: 404);
        }

        public async Task<ApiResult<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
        {
            var entity = await _db.Branches.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Şube bulunamadı", statusCode: 404);

            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, isActive ? "Aktif edildi" : "Pasif edildi", 200);
        }
    }
}
