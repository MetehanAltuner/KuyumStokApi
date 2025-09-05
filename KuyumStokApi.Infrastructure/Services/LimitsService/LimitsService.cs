using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Limits;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.LimitsService
{
    /// <summary>Şube/ürün varyantı bazlı min-max stok limit yönetimi.</summary>
    public sealed class LimitsService : ILimitsService
    {
        private readonly AppDbContext _db;
        public LimitsService(AppDbContext db) => _db = db;

        public async Task<ApiResult<PagedResult<LimitDto>>> GetPagedAsync(LimitFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            var q = _db.Limits
                .AsNoTracking()
                .AsQueryable();

            if (filter.BranchId.HasValue)
                q = q.Where(x => x.BranchId == filter.BranchId);

            if (filter.ProductVariantId.HasValue)
                q = q.Where(x => x.ProductVariantId == filter.ProductVariantId);

            var total = await q.LongCountAsync(ct);

            var items = await q
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new LimitDto
                {
                    Id = x.Id,
                    BranchId = x.BranchId,
                    ProductVariantId = x.ProductVariantId,
                    MinThreshold = x.MinThreshold,
                    MaxThreshold = x.MaxThreshold,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    // Opsiyonel görsel alanlar
                    BranchName = x.Branch != null ? x.Branch.Name : null,
                    VariantLabel = x.ProductVariant != null && x.ProductVariant.ProductType != null
                        ? (x.ProductVariant.ProductType.Name ?? "")
                        : null
                })
                .ToListAsync(ct);

            var paged = new PagedResult<LimitDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };

            return ApiResult<PagedResult<LimitDto>>.Ok(paged, "Liste getirildi", 200);
        }

        public async Task<ApiResult<LimitDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var x = await _db.Limits
                .AsNoTracking()
                .Where(l => l.Id == id)
                .Select(l => new LimitDto
                {
                    Id = l.Id,
                    BranchId = l.BranchId,
                    ProductVariantId = l.ProductVariantId,
                    MinThreshold = l.MinThreshold,
                    MaxThreshold = l.MaxThreshold,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt,
                    BranchName = l.Branch != null ? l.Branch.Name : null,
                    VariantLabel = l.ProductVariant != null && l.ProductVariant.ProductType != null ? (l.ProductVariant.ProductType.Name ?? "") : null
                })
                .FirstOrDefaultAsync(ct);

            return x is null
                ? ApiResult<LimitDto>.Fail("Limit bulunamadı", statusCode: 404)
                : ApiResult<LimitDto>.Ok(x, "Bulundu", 200);
        }

        public async Task<ApiResult<LimitDto>> CreateAsync(LimitCreateDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var entity = new KuyumStokApi.Domain.Entities.Limits
            {
                BranchId = dto.BranchId,
                ProductVariantId = dto.ProductVariantId,
                MinThreshold = dto.MinThreshold,
                MaxThreshold = dto.MaxThreshold,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Limits.Add(entity);
            await _db.SaveChangesAsync(ct);

            var created = new LimitDto
            {
                Id = entity.Id,
                BranchId = entity.BranchId,
                ProductVariantId = entity.ProductVariantId,
                MinThreshold = entity.MinThreshold,
                MaxThreshold = entity.MaxThreshold,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
            return ApiResult<LimitDto>.Ok(created, "Oluşturuldu", 201);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, LimitUpdateDto dto, CancellationToken ct = default)
        {
            var entity = await _db.Limits.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return ApiResult<bool>.Fail("Limit bulunamadı", statusCode: 404);

            entity.MinThreshold = dto.MinThreshold;
            entity.MaxThreshold = dto.MaxThreshold;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.Limits.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return ApiResult<bool>.Fail("Limit bulunamadı", statusCode: 404);

            _db.Limits.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi", 200);
        }
    }
}
