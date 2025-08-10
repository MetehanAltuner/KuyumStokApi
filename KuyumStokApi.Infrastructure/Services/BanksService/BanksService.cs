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

            IQueryable<Domain.Entities.Banks> q = _db.Banks.AsNoTracking();

            if (filter.IncludeDeleted)
                q = q.IgnoreQueryFilters(); // silinmişleri de gör

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                q = q.Where(b => EF.Functions.ILike(b.Name!, $"%{qstr}%")
                              || EF.Functions.ILike(b.Description ?? "", $"%{qstr}%"));
            }

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
                .Select(b => new BankDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    UpdatedAt = b.UpdatedAt,
                    IsActive = b.IsActive,
                    IsDeleted = b.IsDeleted
                })
                .ToListAsync(ct);

            return ApiResult<PagedResult<BankDto>>.Ok(
                new PagedResult<BankDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total },
                "Liste getirildi", 200);
        }

        public async Task<ApiResult<BankDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var b = await _db.Banks.AsNoTracking().IgnoreQueryFilters() // silinmiş de olabilir
                                   .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (b is null) return ApiResult<BankDto>.Fail("Banka bulunamadı", statusCode: 404);

            var dto = new BankDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                UpdatedAt = b.UpdatedAt,
                IsActive = b.IsActive,
                IsDeleted = b.IsDeleted
            };
            return ApiResult<BankDto>.Ok(dto, "Bulundu", 200);
        }

        public async Task<ApiResult<BankDto>> CreateAsync(BankCreateDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var entity = new Domain.Entities.Banks
            {
                Name = dto.Name.Trim(),
                Description = dto.Description,
                UpdatedAt = now,
                IsActive = true
            };

            _db.Banks.Add(entity);
            await _db.SaveChangesAsync(ct);

            var result = new BankDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                UpdatedAt = entity.UpdatedAt,
                IsActive = entity.IsActive,
                IsDeleted = entity.IsDeleted
            };
            return ApiResult<BankDto>.Ok(result, "Oluşturuldu", 201);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, BankUpdateDto dto, CancellationToken ct = default)
        {
            var entity = await _db.Banks.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return ApiResult<bool>.Fail("Banka bulunamadı", statusCode: 404);

            entity.Name = dto.Name.Trim();
            entity.Description = dto.Description;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        // Soft delete (DbContext hook'u devreye girer)
        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.Banks.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return ApiResult<bool>.Fail("Banka bulunamadı", statusCode: 404);

            _db.Banks.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi (soft)", 200);
        }

        // Kalıcı silme (admin için)
        public async Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default)
        {
            var affected = await _db.Banks.IgnoreQueryFilters()
                                          .Where(x => x.Id == id)
                                          .ExecuteDeleteAsync(ct);
            return affected == 1
                ? ApiResult<bool>.Ok(true, "Kalıcı silindi", 200)
                : ApiResult<bool>.Fail("Banka bulunamadı", statusCode: 404);
        }

        public async Task<ApiResult<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
        {
            var entity = await _db.Banks.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null) return ApiResult<bool>.Fail("Banka bulunamadı", statusCode: 404);

            entity.IsActive = isActive;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, isActive ? "Aktif edildi" : "Pasif edildi", 200);
        }
    }
}
