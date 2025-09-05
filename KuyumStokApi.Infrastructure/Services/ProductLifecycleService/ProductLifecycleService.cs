using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.ProductLifecycles;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.ProductLifecycleService
{
    /// <summary>Stok yaşam döngüsü kayıtları (liste/detay + create).</summary>
    public sealed class ProductLifecyclesService : IProductLifecyclesService
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUserService _cu;
        public ProductLifecyclesService(AppDbContext db, ICurrentUserService cu)
        {
            _db = db; _cu = cu;
        }

        public async Task<ApiResult<PagedResult<ProductLifecycleDto>>> GetPagedAsync(ProductLifecycleFilter f, CancellationToken ct = default)
        {
            var page = Math.Max(1, f.Page);
            var size = Math.Clamp(f.PageSize, 1, 200);

            var q = _db.ProductLifecycles.AsNoTracking().AsQueryable();

            if (f.StockId.HasValue) q = q.Where(x => x.StockId == f.StockId);
            if (f.ActionId.HasValue) q = q.Where(x => x.ActionId == f.ActionId);
            if (f.UserId.HasValue) q = q.Where(x => x.UserId == f.UserId);
            if (f.FromUtc.HasValue) q = q.Where(x => x.Timestamp == null || x.Timestamp >= f.FromUtc);
            if (f.ToUtc.HasValue) q = q.Where(x => x.Timestamp == null || x.Timestamp <= f.ToUtc);

            var total = await q.LongCountAsync(ct);

            var items = await q
                .OrderByDescending(x => x.Timestamp ?? x.UpdatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(x => new ProductLifecycleDto
                {
                    Id = x.Id,
                    StockId = x.StockId,
                    UserId = x.UserId ?? 0,
                    ActionId = x.ActionId ?? 0,
                    Note = x.Notes,
                    Timestamp = x.Timestamp,
                    UpdatedAt = x.UpdatedAt,

                    ActionName = x.Action != null ? x.Action.Name : null,
                    UserName = x.User != null ? x.User.Username : null,
                    StockBarcode = x.Stock != null ? x.Stock.Barcode : null,
                    BranchId = x.Stock != null ? x.Stock.BranchId : null,
                    ProductVariantId = x.Stock != null ? x.Stock.ProductVariantId : 0
                })
                .ToListAsync(ct);

            var paged = new PagedResult<ProductLifecycleDto>
            {
                Items = items,
                Page = page,
                PageSize = size,
                TotalCount = total
            };
            return ApiResult<PagedResult<ProductLifecycleDto>>.Ok(paged, "Liste getirildi", 200);
        }

        public async Task<ApiResult<ProductLifecycleDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var x = await _db.ProductLifecycles.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductLifecycleDto
                {
                    Id = p.Id,
                    StockId = p.StockId,
                    UserId = p.UserId ?? 0,
                    ActionId = p.ActionId ?? 0,
                    Note = p.Notes,
                    Timestamp = p.Timestamp,
                    UpdatedAt = p.UpdatedAt,
                    ActionName = p.Action != null ? p.Action.Name : null,
                    UserName = p.User != null ? p.User.Username : null,
                    StockBarcode = p.Stock != null ? p.Stock.Barcode : null,
                    BranchId = p.Stock != null ? p.Stock.BranchId : null,
                    ProductVariantId = p.Stock != null ? p.Stock.ProductVariantId : 0
                })
                .FirstOrDefaultAsync(ct);

            return x is null
                ? ApiResult<ProductLifecycleDto>.Fail("Kayıt bulunamadı", statusCode: 404)
                : ApiResult<ProductLifecycleDto>.Ok(x, "Bulundu", 200);
        }

        public async Task<ApiResult<ProductLifecycleDto>> CreateAsync(ProductLifecycleCreateDto dto, CancellationToken ct = default)
        {
            // stok var mı?
            var stockExists = await _db.Stocks.AnyAsync(s => s.Id == dto.StockId, ct);
            if (!stockExists)
                return ApiResult<ProductLifecycleDto>.Fail("Geçersiz stock_id", statusCode: 400);

            // aksiyon var mı?
            var actionExists = await _db.LifecycleActions.AnyAsync(a => a.Id == dto.ActionId, ct);
            if (!actionExists)
                return ApiResult<ProductLifecycleDto>.Fail("Geçersiz action_id", statusCode: 400);

            var now = DateTime.UtcNow;
            var e = new KuyumStokApi.Domain.Entities.ProductLifecycles
            {
                StockId = dto.StockId,
                UserId = _cu.UserId,        // oturumdaki kullanıcı
                ActionId = dto.ActionId,
                Notes = dto.Note,
                Timestamp = dto.Timestamp ?? now,
                UpdatedAt = now
            };

            _db.ProductLifecycles.Add(e);
            await _db.SaveChangesAsync(ct);

            var res = new ProductLifecycleDto
            {
                Id = e.Id,
                StockId = e.StockId,
                UserId = e.UserId ?? 0,
                ActionId = e.ActionId ?? 0,
                Note = e.Notes,
                Timestamp = e.Timestamp,
                UpdatedAt = e.UpdatedAt
            };
            return ApiResult<ProductLifecycleDto>.Ok(res, "Oluşturuldu", 201);
        }
    }
}
