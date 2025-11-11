using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.ThermalPrinters;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.ThermalPrintersService
{
    /// <summary>Şube termal yazıcı yönetimi.</summary>
    public sealed class ThermalPrintersService : IThermalPrintersService
    {
        private readonly AppDbContext _db;

        public ThermalPrintersService(AppDbContext db) => _db = db;

        public async Task<ApiResult<PagedResult<ThermalPrinterDto>>> GetPagedAsync(ThermalPrinterFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            IQueryable<Domain.Entities.ThermalPrinters> query = _db.ThermalPrinters.AsNoTracking();

            if (filter.IncludeDeleted)
                query = query.IgnoreQueryFilters();

            if (filter.BranchId.HasValue)
                query = query.Where(p => p.BranchId == filter.BranchId);

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive);

            if (filter.UpdatedFromUtc.HasValue)
                query = query.Where(p => p.UpdatedAt == null || p.UpdatedAt >= filter.UpdatedFromUtc);

            if (filter.UpdatedToUtc.HasValue)
                query = query.Where(p => p.UpdatedAt == null || p.UpdatedAt <= filter.UpdatedToUtc);

            var total = await query.LongCountAsync(ct);

            var items = await query
                .OrderByDescending(p => p.UpdatedAt ?? DateTime.MinValue)
                .ThenBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ThermalPrinterDto
                {
                    Id = p.Id,
                    BranchId = p.BranchId,
                    Name = p.Name,
                    IpAddress = p.IpAddress,
                    Port = p.Port,
                    Description = p.Description,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    IsActive = p.IsActive,
                    IsDeleted = p.IsDeleted,
                    Branch = new ThermalPrinterDto.BranchBrief
                    {
                        Id = p.BranchId,
                        Name = p.Branch != null ? p.Branch.Name : null
                    }
                })
                .ToListAsync(ct);

            var payload = new PagedResult<ThermalPrinterDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };

            return ApiResult<PagedResult<ThermalPrinterDto>>.Ok(payload, "Termal yazıcılar listelendi.", 200);
        }

        public async Task<ApiResult<ThermalPrinterDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.ThermalPrinters.AsNoTracking().IgnoreQueryFilters()
                .Include(p => p.Branch)
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (entity is null)
                return ApiResult<ThermalPrinterDto>.Fail("Termal yazıcı bulunamadı.", statusCode: 404);

            return ApiResult<ThermalPrinterDto>.Ok(Map(entity), "Termal yazıcı bulundu.", 200);
        }

        public async Task<ApiResult<ThermalPrinterDto>> CreateAsync(ThermalPrinterCreateDto dto, CancellationToken ct = default)
        {
            // her branch için tek yazıcı kuralı
            var exists = await _db.ThermalPrinters.AnyAsync(p => p.BranchId == dto.BranchId, ct);
            if (exists)
                return ApiResult<ThermalPrinterDto>.Fail("Bu şube için termal yazıcı zaten tanımlı.", statusCode: 409);

            var branchExists = await _db.Branches.AnyAsync(b => b.Id == dto.BranchId, ct);
            if (!branchExists)
                return ApiResult<ThermalPrinterDto>.Fail("Şube bulunamadı.", statusCode: 400);

            var now = DateTime.UtcNow;
            var entity = new Domain.Entities.ThermalPrinters
            {
                BranchId = dto.BranchId,
                Name = dto.Name.Trim(),
                IpAddress = dto.IpAddress.Trim(),
                Port = dto.Port,
                Description = dto.Description,
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            };

            _db.ThermalPrinters.Add(entity);
            await _db.SaveChangesAsync(ct);

            var created = await GetByIdAsync(entity.Id, ct);
            created.StatusCode = 201;
            created.Message = "Termal yazıcı oluşturuldu.";
            return created;
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, ThermalPrinterUpdateDto dto, CancellationToken ct = default)
        {
            var entity = await _db.ThermalPrinters.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Termal yazıcı bulunamadı.", statusCode: 404);

            if (!string.IsNullOrWhiteSpace(dto.Name))
                entity.Name = dto.Name.Trim();

            if (!string.IsNullOrWhiteSpace(dto.IpAddress))
                entity.IpAddress = dto.IpAddress.Trim();

            if (dto.Port.HasValue)
                entity.Port = dto.Port.Value;

            entity.Description = dto.Description ?? entity.Description;

            if (dto.IsActive.HasValue)
                entity.IsActive = dto.IsActive.Value;

            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Termal yazıcı güncellendi.", 200);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.ThermalPrinters.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Termal yazıcı bulunamadı.", statusCode: 404);

            _db.ThermalPrinters.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Termal yazıcı silindi.", 200);
        }

        public async Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default)
        {
            var affected = await _db.ThermalPrinters.IgnoreQueryFilters()
                .Where(p => p.Id == id)
                .ExecuteDeleteAsync(ct);

            return affected == 1
                ? ApiResult<bool>.Ok(true, "Termal yazıcı kalıcı olarak silindi.", 200)
                : ApiResult<bool>.Fail("Termal yazıcı bulunamadı.", statusCode: 404);
        }

        private static ThermalPrinterDto Map(Domain.Entities.ThermalPrinters entity) =>
            new ThermalPrinterDto
            {
                Id = entity.Id,
                BranchId = entity.BranchId,
                Name = entity.Name,
                IpAddress = entity.IpAddress,
                Port = entity.Port,
                Description = entity.Description,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                IsActive = entity.IsActive,
                IsDeleted = entity.IsDeleted,
                Branch = entity.Branch == null ? null : new ThermalPrinterDto.BranchBrief
                {
                    Id = entity.Branch.Id,
                    Name = entity.Branch.Name
                }
            };
    }
}


