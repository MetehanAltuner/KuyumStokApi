using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Customers;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.CustomersService
{
    /// <summary>Müşteri operasyonları (listele, detay, ekle, güncelle, sil).</summary>
    public sealed class CustomersService : ICustomersService
    {
        private readonly AppDbContext _db;
        public CustomersService(AppDbContext db) => _db = db;

        public async Task<ApiResult<PagedResult<CustomerDto>>> GetPagedAsync(CustomerFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            var q = _db.Customers.AsNoTracking().AsQueryable();

            if (filter.OnlyActive == true)
                q = q.Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                q = q.Where(x =>
                    EF.Functions.ILike(x.Name!, $"%{qstr}%") ||
                    EF.Functions.ILike(x.Phone ?? "", $"%{qstr}%") ||
                    EF.Functions.ILike(x.Note ?? "", $"%{qstr}%"));
            }

            if (filter.UpdatedFromUtc.HasValue)
                q = q.Where(x => x.UpdatedAt == null || x.UpdatedAt >= filter.UpdatedFromUtc);

            if (filter.UpdatedToUtc.HasValue)
                q = q.Where(x => x.UpdatedAt == null || x.UpdatedAt <= filter.UpdatedToUtc);

            var total = await q.LongCountAsync(ct);

            var items = await q
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CustomerDto
                {
                    Id = x.Id,
                    Name = x.Name!,
                    Phone = x.Phone,
                    Note = x.Note,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    IsActive = x.IsActive
                })
                .ToListAsync(ct);

            var paged = new PagedResult<CustomerDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };

            return ApiResult<PagedResult<CustomerDto>>.Ok(paged, "Liste getirildi", 200);
        }

        public async Task<ApiResult<CustomerDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var x = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
            if (x is null) return ApiResult<CustomerDto>.Fail("Müşteri bulunamadı", statusCode: 404);

            var dto = new CustomerDto
            {
                Id = x.Id,
                Name = x.Name!,
                Phone = x.Phone,
                Note = x.Note,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IsActive = x.IsActive
            };
            return ApiResult<CustomerDto>.Ok(dto, "Bulundu", 200);
        }

        public async Task<ApiResult<CustomerDto>> CreateAsync(CustomerCreateDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var e = new Domain.Entities.Customers
            {
                Name = dto.Name,
                Phone = dto.Phone,
                Note = dto.Note,
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
            };

            _db.Customers.Add(e);
            await _db.SaveChangesAsync(ct);

            var result = new CustomerDto
            {
                Id = e.Id,
                Name = e.Name!,
                Phone = e.Phone,
                Note = e.Note,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                IsActive = e.IsActive
            };
            return ApiResult<CustomerDto>.Ok(result, "Oluşturuldu", 201);
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, CustomerUpdateDto dto, CancellationToken ct = default)
        {
            var e = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (e is null) return ApiResult<bool>.Fail("Müşteri bulunamadı", statusCode: 404);

            e.Name = dto.Name;
            e.Phone = dto.Phone;
            e.Note = dto.Note;
            e.IsActive = dto.IsActive;
            e.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var e = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (e is null) return ApiResult<bool>.Fail("Müşteri bulunamadı", statusCode: 404);

            // referans kontrolü (satış/alış)
            var hasRefs = await _db.Sales.AsNoTracking().AnyAsync(s => s.CustomerId == id, ct)
                       || await _db.Purchases.AsNoTracking().AnyAsync(p => p.CustomerId == id, ct);

            if (hasRefs)
            {
                // tercihe göre 409 veya soft-delete:
                // return ApiResult<bool>.Fail("Müşterinin hareketleri var, silinemez.", statusCode: 409);
                e.IsDeleted = true;
                e.DeletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                return ApiResult<bool>.Ok(true, "Müşteri soft-delete yapıldı (kayıtları olduğu için).", 200);
            }

            _db.Customers.Remove(e);
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi", 200);
        }
    }
}
