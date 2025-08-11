using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.PaymentMethods;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.PaymentMethodsService
{
    /// <summary>Ödeme yöntemleri CRUD ve listeleme servisidir.</summary>
    public sealed class PaymentMethodsService : IPaymentMethodsService
    {
        private readonly AppDbContext _db;
        public PaymentMethodsService(AppDbContext db) => _db = db;

        public async Task<ApiResult<PagedResult<PaymentMethodDto>>> GetPagedAsync(PaymentMethodFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            var q = _db.PaymentMethods.AsNoTracking().AsQueryable();

            if (filter.OnlyActive == true)
                q = q.Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                q = q.Where(x => EF.Functions.ILike(x.Name!, $"%{qstr}%"));
            }

            var total = await q.LongCountAsync(ct);

            var items = await q
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PaymentMethodDto
                {
                    Id = x.Id,
                    Name = x.Name!,
                    IsActive = x.IsActive
                })
                .ToListAsync(ct);

            var paged = new PagedResult<PaymentMethodDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };

            return ApiResult<PagedResult<PaymentMethodDto>>.Ok(paged, "Liste getirildi", 200);
        }

        public async Task<ApiResult<PaymentMethodDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var x = await _db.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (x is null) return ApiResult<PaymentMethodDto>.Fail("Ödeme yöntemi bulunamadı", statusCode: 404);

            var dto = new PaymentMethodDto
            {
                Id = x.Id,
                Name = x.Name!,
                IsActive = x.IsActive
            };
            return ApiResult<PaymentMethodDto>.Ok(dto, "Bulundu", 200);
        }

        public async Task<ApiResult<PaymentMethodDto>> CreateAsync(PaymentMethodCreateDto dto, CancellationToken ct = default)
        {
            var e = new Domain.Entities.PaymentMethods
            {
                Name = dto.Name,
                IsActive = true
            };

            _db.PaymentMethods.Add(e);
            await _db.SaveChangesAsync(ct);

            var result = new PaymentMethodDto { Id = e.Id, Name = e.Name!, IsActive = e.IsActive };
            return ApiResult<PaymentMethodDto>.Ok(result, "Oluşturuldu", 201);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, PaymentMethodUpdateDto dto, CancellationToken ct = default)
        {
            var e = await _db.PaymentMethods.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (e is null) return ApiResult<bool>.Fail("Ödeme yöntemi bulunamadı", statusCode: 404);

            e.Name = dto.Name;
            e.IsActive = dto.IsActive;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var e = await _db.PaymentMethods.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (e is null) return ApiResult<bool>.Fail("Ödeme yöntemi bulunamadı", statusCode: 404);

            // referans kontrolü: satış/alışta kullanılıyor mu?
            var hasRefs = await _db.Sales.AsNoTracking().AnyAsync(s => s.PaymentMethodId == id, ct)
                       || await _db.Purchases.AsNoTracking().AnyAsync(p => p.PaymentMethodId == id, ct);

            if (hasRefs)
            {
                // Tercih: 409 döndürmek istersen aşağıyı aç, soft-delete'i kapat.
                // return ApiResult<bool>.Fail("Kullanılan ödeme yöntemi silinemez.", statusCode: 409);

                e.IsDeleted = true;
                e.DeletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                return ApiResult<bool>.Ok(true, "Öğe soft-delete yapıldı (kullanımda).", 200);
            }

            _db.PaymentMethods.Remove(e);
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi", 200);
        }
    }
}
