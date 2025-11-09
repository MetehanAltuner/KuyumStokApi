using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Stocks;
using KuyumStokApi.Application.Interfaces.Auth;
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
        private readonly ICurrentUserContext _user;

        public StocksService(AppDbContext db, ICurrentUserContext user)
        {
            _db = db;
            _user = user;
        }

        // -------- LİSTE: sadece kullanıcının şubesindeki stok SATIRLARI (gruplama yok) --------
        public async Task<ApiResult<PagedResult<StockDto>>> GetPagedAsync(StockFilter filter, CancellationToken ct = default)
        {
            var branchId = filter.BranchId ?? _user.BranchId;
            if (branchId is null)
                return ApiResult<PagedResult<StockDto>>.Fail("Şube belirlenemedi.", statusCode: 400);

            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            var q =
                from s in _db.Stocks.AsNoTracking()
                where s.BranchId == branchId
                join v in _db.ProductVariants.AsNoTracking() on s.ProductVariantId equals v.Id into jv
                from v in jv.DefaultIfEmpty()
                join t in _db.ProductTypes.AsNoTracking() on v!.ProductTypeId equals t.Id into jt
                from t in jt.DefaultIfEmpty()
                join c in _db.ProductCategories.AsNoTracking() on t!.CategoryId equals c.Id into jc
                from c in jc.DefaultIfEmpty()
                select new { s, v, t, c };

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                q = q.Where(x =>
                    EF.Functions.ILike(x.s.Barcode, $"%{qstr}%") ||
                    EF.Functions.ILike(x.s.QrCode ?? "", $"%{qstr}%") ||
                    (x.v != null && (
                        EF.Functions.ILike(x.v.Name, $"%{qstr}%") ||
                        EF.Functions.ILike(x.v.Brand ?? "", $"%{qstr}%") ||
                        EF.Functions.ILike(x.v.Ayar ?? "", $"%{qstr}%") ||
                        EF.Functions.ILike(x.v.Color ?? "", $"%{qstr}%")
                    ))
                );
            }

            if (filter.ProductTypeId is not null)
                q = q.Where(x => x.v != null && x.v.ProductTypeId == filter.ProductTypeId);

            if (filter.ProductVariantId is not null)
                q = q.Where(x => x.s.ProductVariantId == filter.ProductVariantId);

            if (filter.GramMin is not null)
                q = q.Where(x => (x.s.Gram ?? 0) >= filter.GramMin);

            if (filter.GramMax is not null)
                q = q.Where(x => (x.s.Gram ?? 0) <= filter.GramMax);

            if (filter.UpdatedFromUtc is not null)
                q = q.Where(x => x.s.UpdatedAt == null || x.s.UpdatedAt >= filter.UpdatedFromUtc);

            if (filter.UpdatedToUtc is not null)
                q = q.Where(x => x.s.UpdatedAt == null || x.s.UpdatedAt <= filter.UpdatedToUtc);

            var total = await q.LongCountAsync(ct);

            var items = await q
                .OrderByDescending(x => x.s.UpdatedAt ?? DateTime.MinValue)
                .ThenBy(x => x.s.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new StockDto
                {
                    Id = x.s.Id,
                    Quantity = x.s.Quantity,
                    Barcode = x.s.Barcode,
                    QrCode = x.s.QrCode,
                    CreatedAt = x.s.CreatedAt,
                    UpdatedAt = x.s.UpdatedAt,
                    TotalWeight = (x.s.Gram ?? 0) * (decimal)(x.s.Quantity ?? 0),

                    Branch = new StockDto.BranchBrief
                    {
                        Id = x.s.BranchId,
                        Name = x.s.Branch != null ? x.s.Branch.Name : null
                    },
                    ProductVariant = new StockDto.VariantBrief
                    {
                        Id = x.s.ProductVariantId,
                        Name = x.v != null ? x.v.Name : null,
                        Ayar = x.v != null ? x.v.Ayar : null,
                        Color = x.v != null ? x.v.Color : null,
                        Brand = x.v != null ? x.v.Brand : null,

                        Gram = x.s.Gram,
                        ProductTypeId = x.v != null ? x.v.ProductTypeId : null,
                        ProductTypeName = x.t != null ? x.t.Name : null,
                        CategoryName = x.c != null ? x.c.Name : null
                    }
                })
                .ToListAsync(ct);

            return ApiResult<PagedResult<StockDto>>.Ok(
                new PagedResult<StockDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total },
                "Şube stok listesi", 200);
        }

        // -------- DETAY: seçilen varyant, aynı store’daki tüm şubeler --------
        public async Task<ApiResult<StockVariantDetailByStoreDto>> GetVariantDetailInStoreAsync(int variantId, CancellationToken ct = default)
        {
            var myBranchId = _user.BranchId;
            if (myBranchId is null)
                return ApiResult<StockVariantDetailByStoreDto>.Fail("Şube belirlenemedi.", statusCode: 400);

            var storeId = await _db.Branches.AsNoTracking()
                              .Where(b => b.Id == myBranchId)
                              .Select(b => b.StoreId)
                              .FirstOrDefaultAsync(ct);

            if (storeId is null)
                return ApiResult<StockVariantDetailByStoreDto>.Fail("Kullanıcı şubesinin mağazası bulunamadı.", statusCode: 404);

            var v = await _db.ProductVariants.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == variantId, ct);
            if (v is null)
                return ApiResult<StockVariantDetailByStoreDto>.Fail("Varyant bulunamadı.", statusCode: 404);

            var baseQ =
                from s in _db.Stocks.AsNoTracking()
                join b in _db.Branches.AsNoTracking() on s.BranchId equals b.Id
                where s.ProductVariantId == variantId && b.StoreId == storeId
                select new { s, b };

            var grouped =
                from x in baseQ
                group x by new { x.b.Id, x.b.Name } into g
                select new
                {
                    BranchId = g.Key.Id,
                    BranchName = g.Key.Name,
                    ToplamAdet = g.Sum(z => z.s.Quantity ?? 0),
                    ToplamAgirlik = g.Sum(z => (z.s.Gram ?? 0) * (decimal)(z.s.Quantity ?? 0)),
                    Items = g.Select(z => new StockVariantDetailByStoreDto.StockChip
                    {
                        StockId = z.s.Id,
                        Barcode = z.s.Barcode,
                        Gram = z.s.Gram ?? 0,
                        Color = v.Color
                    })
                };

            var dto = new StockVariantDetailByStoreDto
            {
                VariantId = v.Id,
                VariantName = v.Name,
                Ayar = v.Ayar,
                Color = v.Color,
                Branches = await grouped
                    .OrderBy(x => x.BranchName)
                    .Select(x => new StockVariantDetailByStoreDto.BranchBlock
                    {
                        BranchId = x.BranchId,
                        BranchName = x.BranchName ?? "",
                        ToplamAdet = x.ToplamAdet,
                        ToplamAgirlik = x.ToplamAgirlik,
                        Items = x.Items.ToList()
                    })
                    .ToListAsync(ct)
            };

            return ApiResult<StockVariantDetailByStoreDto>.Ok(dto, "Detay", 200);
        }

        // ------------ CRUD (sende zaten var; aynen koruyorum) ------------
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
            var exists = await _db.Stocks.AnyAsync(x => x.Barcode == dto.Barcode, ct);
            if (exists)
                return ApiResult<StockDto>.Fail("Bu barkod zaten kullanılıyor.", statusCode: 409);

            var now = DateTime.UtcNow;
            
            var entity = new Domain.Entities.Stocks
            {
                ProductVariantId = dto.ProductVariantId,
                BranchId = _user.BranchId, //Bunu user üzerinden allllll
                Quantity = dto.Quantity,
                Gram = dto.Weight,
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

            _db.Stocks.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi", 200);
        }

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
            var v = await _db.ProductVariants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == s.ProductVariantId, ct);
            var t = v?.ProductTypeId == null ? null :
                    await _db.ProductTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == v.ProductTypeId, ct);
            var c = t?.CategoryId == null ? null :
                    await _db.ProductCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == t.CategoryId, ct);

            var dto = new StockDto
            {
                Id = s.Id,
                Quantity = s.Quantity,
                Barcode = s.Barcode,
                QrCode = s.QrCode,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                TotalWeight = (s.Gram ?? 0) * (decimal)(s.Quantity ?? 0),
                Branch = new StockDto.BranchBrief
                {
                    Id = s.BranchId,
                    Name = s.BranchId == null ? null :
                           await _db.Branches.AsNoTracking().Where(b => b.Id == s.BranchId).Select(b => b.Name).FirstOrDefaultAsync(ct)
                },
                ProductVariant = new StockDto.VariantBrief
                {
                    Id = s.ProductVariantId,
                    Name = v?.Name,
                    Ayar = v?.Ayar,
                    Color = v?.Color,
                    Brand = v?.Brand,
                    Gram = s.Gram,
                    ProductTypeId = v?.ProductTypeId,
                    ProductTypeName = t?.Name,
                    CategoryName = c?.Name
                }
            };

            return ApiResult<StockDto>.Ok(dto, "Bulundu", 200);
        }
    }
}

