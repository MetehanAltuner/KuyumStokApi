using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Banks;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.BanksService
{
    public sealed class BanksService : IBanksService
    {
        private readonly AppDbContext _db;
        public BanksService(AppDbContext db) => _db = db;

        public async Task<ApiResult<PagedResult<BankDto>>> GetPagedAsync(BankFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            var q = _db.Banks.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                q = q.Where(b => EF.Functions.ILike(b.Name!, $"%{qstr}%")
                              || EF.Functions.ILike(b.Description ?? "", $"%{qstr}%"));
            }

            if (filter.UpdatedFromUtc.HasValue)
                q = q.Where(b => b.UpdatedAt == null || b.UpdatedAt >= filter.UpdatedFromUtc);

            if (filter.UpdatedToUtc.HasValue)
                q = q.Where(b => b.UpdatedAt == null || b.UpdatedAt <= filter.UpdatedToUtc);

            var total = await q.LongCountAsync(ct);

            var items = await q
                .OrderByDescending(b => b.UpdatedAt ?? DateTime.MinValue)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BankDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync(ct);

            var paged = new PagedResult<BankDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };

            return ApiResult<PagedResult<BankDto>>.Ok(paged, "Liste getirildi", 200);
        }

        public async Task<ApiResult<BankDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var b = await _db.Banks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (b is null) return ApiResult<BankDto>.Fail("Banka bulunamadı", statusCode: 404);

            var dto = new BankDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                UpdatedAt = b.UpdatedAt
            };
            return ApiResult<BankDto>.Ok(dto, "Bulundu", 200);
        }

        public async Task<ApiResult<BankDto>> CreateAsync(BankCreateDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var entity = new KuyumStokApi.Domain.Entities.Banks
            {
                Name = dto.Name,
                Description = dto.Description,
                UpdatedAt = now
            };

            _db.Banks.Add(entity);
            await _db.SaveChangesAsync(ct);

            var result = new BankDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                UpdatedAt = entity.UpdatedAt
            };
            return ApiResult<BankDto>.Ok(result, "Oluşturuldu", 201);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, BankUpdateDto dto, CancellationToken ct = default)
        {
            var entity = await _db.Banks.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return ApiResult<bool>.Fail("Banka bulunamadı", statusCode: 404);

            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.Banks.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return ApiResult<bool>.Fail("Banka bulunamadı", statusCode: 404);

            _db.Banks.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi", 200);
        }
    }
}
