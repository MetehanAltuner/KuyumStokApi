using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.LifeCycleActions;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.LifecycleActionsService
{
    /// <summary>Lifecycle aksiyon sözlüğü (CRUD).</summary>
    public sealed class LifecycleActionsService : ILifecycleActionsService
    {
        private readonly AppDbContext _db;
        public LifecycleActionsService(AppDbContext db) => _db = db;

        public async Task<ApiResult<PagedResult<LifecycleActionDto>>> GetPagedAsync(LifecycleActionFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var size = Math.Clamp(filter.PageSize, 1, 200);

            var q = _db.LifecycleActions.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qx = filter.Query.Trim();
                q = q.Where(x => EF.Functions.ILike(x.Name!, $"%{qx}%") ||
                                 EF.Functions.ILike(x.Description ?? "", $"%{qx}%"));
            }

            var total = await q.LongCountAsync(ct);

            var items = await q
                .OrderBy(x => x.Name)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(x => new LifecycleActionDto
                {
                    Id = x.Id,
                    Name = x.Name!,
                    Description = x.Description
                })
                .ToListAsync(ct);

            var paged = new PagedResult<LifecycleActionDto>
            {
                Items = items,
                Page = page,
                PageSize = size,
                TotalCount = total
            };
            return ApiResult<PagedResult<LifecycleActionDto>>.Ok(paged, "Liste getirildi", 200);
        }

        public async Task<ApiResult<LifecycleActionDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var x = await _db.LifecycleActions.AsNoTracking()
                .Where(l => l.Id == id)
                .Select(l => new LifecycleActionDto
                {
                    Id = l.Id,
                    Name = l.Name!,
                    Description = l.Description
                })
                .FirstOrDefaultAsync(ct);

            return x is null
                ? ApiResult<LifecycleActionDto>.Fail("Aksiyon bulunamadı", statusCode: 404)
                : ApiResult<LifecycleActionDto>.Ok(x, "Bulundu", 200);
        }

        public async Task<ApiResult<LifecycleActionDto>> CreateAsync(LifecycleActionCreateDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var e = new KuyumStokApi.Domain.Entities.LifecycleActions
            {
                Name = dto.Name,
                Description = dto.Description
            };
            _db.LifecycleActions.Add(e);
            await _db.SaveChangesAsync(ct);

            var res = new LifecycleActionDto
            {
                Id = e.Id,
                Name = e.Name!,
                Description = e.Description
            };
            return ApiResult<LifecycleActionDto>.Ok(res, "Oluşturuldu", 201);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, LifecycleActionUpdateDto dto, CancellationToken ct = default)
        {
            var e = await _db.LifecycleActions.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return ApiResult<bool>.Fail("Aksiyon bulunamadı", statusCode: 404);

            e.Name = dto.Name;
            e.Description = dto.Description;
            await _db.SaveChangesAsync(ct);

            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var e = await _db.LifecycleActions.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (e is null) return ApiResult<bool>.Fail("Aksiyon bulunamadı", statusCode: 404);

            _db.LifecycleActions.Remove(e);
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi", 200);
        }
    }
}
