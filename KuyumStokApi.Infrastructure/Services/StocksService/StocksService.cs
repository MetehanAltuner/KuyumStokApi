using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Stocks;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.StocksService
{
    /// <summary>Stok servis implementasyonu.</summary>
    public sealed class StocksService : IStocksService
    {
        private readonly AppDbContext _db;
        public StocksService(AppDbContext db) => _db = db;

        public async Task<ApiResult<PagedResult<StockDto>>> GetPagedAsync(StockFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            var q = _db.Stocks.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                q = q.Where(s =>
                    EF.Functions.ILike(s.Barcode, $"%{qstr}%") ||
                    EF.Functions.ILike(s.QrCode ?? "", $"%{qstr}%") ||
                    (s.ProductVariant != null && (
                        EF.Functions.ILike(s.ProductVariant.Brand ?? "", $"%{qstr}%") ||
                        EF.Functions.ILike(s.ProductVariant.Ayar ?? "", $"%{qstr}%")
                    ))
                );
            }

            if (filter.BranchId is not null)
                q = q.Where(s => s.BranchId == filter.BranchId);

            if (filter.ProductTypeId is not null)
                q = q.Where(s => s.ProductVariant != null && s.ProductVariant.ProductTypeId == filter.ProductTypeId);

            if (filter.ProductVariantId is not null)
                q = q.Where(s => s.ProductVariantId == filter.ProductVariantId);

            if (filter.GramMin is not null)
                q = q.Where(s => s.ProductVariant != null && s.ProductVariant.Gram >= filter.GramMin);

            if (filter.GramMax is not null)
                q = q.Where(s => s.ProductVariant != null && s.ProductVariant.Gram <= filter.GramMax);

            if (filter.UpdatedFromUtc is not null)
                q = q.Where(s => s.UpdatedAt == null || s.UpdatedAt >= filter.UpdatedFromUtc);

            if (filter.UpdatedToUtc is not null)
                q = q.Where(s => s.UpdatedAt == null || s.UpdatedAt <= filter.UpdatedToUtc);

            var total = await q.LongCountAsync(ct);

            var items = await q
                .OrderByDescending(s => s.UpdatedAt ?? DateTime.MinValue)
                .ThenBy(s => s.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StockDto
                {
                    Id = s.Id,
                    Quantity = s.Quantity,
                    Barcode = s.Barcode,
                    QrCode = s.QrCode,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    Branch = new StockDto.BranchBrief
                    {
                        Id = s.BranchId,
                        Name = s.Branch != null ? s.Branch.Name : null
                    },
                    ProductVariant = new StockDto.VariantBrief
                    {
                        Id = s.ProductVariantId,
                        Gram = s.ProductVariant != null ? s.ProductVariant.Gram : null,
                        Ayar = s.ProductVariant != null ? s.ProductVariant.Ayar : null,
                        Brand = s.ProductVariant != null ? s.ProductVariant.Brand : null,
                        ProductTypeId = s.ProductVariant != null ? s.ProductVariant.ProductTypeId : null,
                        ProductTypeName = s.ProductVariant != null && s.ProductVariant.ProductType != null
                                            ? s.ProductVariant.ProductType.Name
                                            : null
                    }
                })
                .ToListAsync(ct);

            return ApiResult<PagedResult<StockDto>>.Ok(
                new PagedResult<StockDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total },
                "Liste", 200);
        }

        public async Task<ApiResult<StockDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var s = await _db.Stocks.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (s is null)
                return ApiResult<StockDto>.Fail("Stok bulunamadı", statusCode: 404);

            return await MapToDto(s, ct);
        }

        public async Task<ApiResult<StockDto>> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
        {
            var s = await _db.Stocks.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Barcode == barcode, ct);

            if (s is null)
                return ApiResult<StockDto>.Fail("Stok bulunamadı", statusCode: 404);

            return await MapToDto(s, ct);
        }

        public async Task<ApiResult<StockDto>> CreateAsync(StockCreateDto dto, CancellationToken ct = default)
        {
            // Barkod benzersizlik
            var exists = await _db.Stocks.AnyAsync(x => x.Barcode == dto.Barcode, ct);
            if (exists)
                return ApiResult<StockDto>.Fail("Bu barkod zaten kullanılıyor.", statusCode: 409);

            var now = DateTime.UtcNow;

            var entity = new Domain.Entities.Stocks
            {
                ProductVariantId = dto.ProductVariantId,
                BranchId = dto.BranchId,
                Quantity = dto.Quantity,
                Barcode = dto.Barcode,
                QrCode = dto.QrCode,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Stocks.Add(entity);
            await _db.SaveChangesAsync(ct);

            var created = await GetByIdAsync(entity.Id, ct);
            created.StatusCode = 201;
            created.Message = "Oluşturuldu";
            return created;
        }

        public async Task<ApiResult<bool>> UpdateAsync(int id, StockUpdateDto dto, CancellationToken ct = default)
        {
            var entity = await _db.Stocks.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Stok bulunamadı", statusCode: 404);

            if (!string.IsNullOrWhiteSpace(dto.Barcode) && dto.Barcode != entity.Barcode)
            {
                var exists = await _db.Stocks.AnyAsync(x => x.Barcode == dto.Barcode, ct);
                if (exists)
                    return ApiResult<bool>.Fail("Bu barkod zaten kullanılıyor.", statusCode: 409);

                entity.Barcode = dto.Barcode;
            }

            entity.ProductVariantId = dto.ProductVariantId ?? entity.ProductVariantId;
            entity.BranchId = dto.BranchId ?? entity.BranchId;
            entity.Quantity = dto.Quantity ?? entity.Quantity;
            entity.QrCode = dto.QrCode ?? entity.QrCode;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        // Silme GUARD: satış/alış/lifecycle bağlılığı varsa 409
        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.Stocks.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Stok bulunamadı", statusCode: 404);

            var usedInSale = await _db.SaleDetails.AnyAsync(d => d.StockId == id, ct);
            var usedInPurchase = await _db.PurchaseDetails.AnyAsync(d => d.StockId == id, ct);
            var usedInLife = await _db.ProductLifecycles.AnyAsync(l => l.StockId == id, ct);

            if (usedInSale || usedInPurchase || usedInLife)
                return ApiResult<bool>.Fail("Bu stok satış/alış/hareket kayıtlarında kullanılıyor. Silinemez.", statusCode: 409);

            // (Stocks için soft-delete kolonlarımız yok; bu yüzden hard delete'e denk gelir)
            _db.Stocks.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi", 200);
        }

        // Hard delete: aynı guard (ayrıca örnek olsun diye ExecuteDelete)
        public async Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default)
        {
            var usedInSale = await _db.SaleDetails.AnyAsync(d => d.StockId == id, ct);
            var usedInPurchase = await _db.PurchaseDetails.AnyAsync(d => d.StockId == id, ct);
            var usedInLife = await _db.ProductLifecycles.AnyAsync(l => l.StockId == id, ct);

            if (usedInSale || usedInPurchase || usedInLife)
                return ApiResult<bool>.Fail("Bağlı kayıtlar nedeniyle kalıcı silme yapılamaz.", statusCode: 409);

            var affected = await _db.Stocks.Where(s => s.Id == id).ExecuteDeleteAsync(ct);
            return affected == 1
                ? ApiResult<bool>.Ok(true, "Kalıcı silindi", 200)
                : ApiResult<bool>.Fail("Stok bulunamadı", statusCode: 404);
        }

        // ---- helpers ----
        private async Task<ApiResult<StockDto>> MapToDto(Domain.Entities.Stocks s, CancellationToken ct)
        {
            var dto = new StockDto
            {
                Id = s.Id,
                Quantity = s.Quantity,
                Barcode = s.Barcode,
                QrCode = s.QrCode,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                Branch = new StockDto.BranchBrief
                {
                    Id = s.BranchId,
                    Name = s.BranchId == null ? null :
                           await _db.Branches.AsNoTracking().Where(b => b.Id == s.BranchId).Select(b => b.Name).FirstOrDefaultAsync(ct)
                },
                ProductVariant = new StockDto.VariantBrief
                {
                    Id = s.ProductVariantId,
                    Gram = s.ProductVariantId == null ? null :
                                      await _db.ProductVariants.AsNoTracking().Where(v => v.Id == s.ProductVariantId).Select(v => v.Gram).FirstOrDefaultAsync(ct),
                    Ayar = s.ProductVariantId == null ? null :
                                      await _db.ProductVariants.AsNoTracking().Where(v => v.Id == s.ProductVariantId).Select(v => v.Ayar).FirstOrDefaultAsync(ct),
                    Brand = s.ProductVariantId == null ? null :
                                      await _db.ProductVariants.AsNoTracking().Where(v => v.Id == s.ProductVariantId).Select(v => v.Brand).FirstOrDefaultAsync(ct),
                    ProductTypeId = s.ProductVariantId == null ? null :
                                      await _db.ProductVariants.AsNoTracking().Where(v => v.Id == s.ProductVariantId).Select(v => v.ProductTypeId).FirstOrDefaultAsync(ct),
                    ProductTypeName = s.ProductVariantId == null ? null :
                                      await _db.ProductVariants.AsNoTracking()
                                          .Where(v => v.Id == s.ProductVariantId && v.ProductType != null)
                                          .Select(v => v.ProductType!.Name)
                                          .FirstOrDefaultAsync(ct)
                }
            };

            return ApiResult<StockDto>.Ok(dto, "Bulundu", 200);
        }
    }
}
