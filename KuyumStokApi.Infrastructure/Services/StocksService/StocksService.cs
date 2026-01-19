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
using System.Security.Cryptography;
using System.Text.Json;
using QRCoder;
using Microsoft.Extensions.Options;
using KuyumStokApi.Infrastructure.QrCode;

namespace KuyumStokApi.Infrastructure.Services.StocksService
{
    /// <summary>Stok servis implementasyonu.</summary>
    public sealed class StocksService : IStocksService
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUserContext _user;
        private readonly QrCodeOptions _qrOptions;

        public StocksService(AppDbContext db, ICurrentUserContext user, IOptions<QrCodeOptions> qrOptions)
        {
            _db = db;
            _user = user;
            _qrOptions = qrOptions.Value;
        }

        // -------- LİSTE: Aynı varyanta sahip stokları gruplayarak toplam adet ve ağırlık ile gösterir --------
        public async Task<ApiResult<PagedResult<StockDto>>> GetPagedAsync(StockFilter filter, CancellationToken ct = default)
        {
            var branchId = filter.BranchId ?? _user.BranchId;
            if (branchId is null)
                return ApiResult<PagedResult<StockDto>>.Fail("Şube belirlenemedi.", statusCode: 400);

            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            // Temel sorgu: Stokları variant ve kategori bilgileriyle join et
            var baseQ =
                from s in _db.Stocks.AsNoTracking()
                where s.BranchId == branchId
                join v in _db.ProductVariants.AsNoTracking() on s.ProductVariantId equals v.Id into jv
                from v in jv.DefaultIfEmpty()
                join t in _db.ProductTypes.AsNoTracking() on v!.ProductTypeId equals t.Id into jt
                from t in jt.DefaultIfEmpty()
                join c in _db.ProductCategories.AsNoTracking() on t!.CategoryId equals c.Id into jc
                from c in jc.DefaultIfEmpty()
                join b in _db.Branches.AsNoTracking() on s.BranchId equals b.Id into jb
                from b in jb.DefaultIfEmpty()
                select new { s, v, t, c, b };

            // Filtreler
            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                baseQ = baseQ.Where(x =>
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
                baseQ = baseQ.Where(x => x.v != null && x.v.ProductTypeId == filter.ProductTypeId);

            if (filter.ProductVariantId is not null)
                baseQ = baseQ.Where(x => x.s.ProductVariantId == filter.ProductVariantId);

            if (filter.GramMin is not null)
                baseQ = baseQ.Where(x => (x.s.Gram ?? 0) >= filter.GramMin);

            if (filter.GramMax is not null)
                baseQ = baseQ.Where(x => (x.s.Gram ?? 0) <= filter.GramMax);

            if (filter.UpdatedFromUtc is not null)
                baseQ = baseQ.Where(x => x.s.UpdatedAt == null || x.s.UpdatedAt >= filter.UpdatedFromUtc);

            if (filter.UpdatedToUtc is not null)
                baseQ = baseQ.Where(x => x.s.UpdatedAt == null || x.s.UpdatedAt <= filter.UpdatedToUtc);

            var total = await baseQ.LongCountAsync(ct);

            var rows = await baseQ
                .OrderByDescending(x => x.s.UpdatedAt ?? x.s.CreatedAt)
                .ThenBy(x => x.s.ProductVariantId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    StockId = x.s.Id,
                    x.s.Quantity,
                    x.s.Barcode,
                    x.s.QrCode,
                    x.s.CreatedAt,
                    x.s.UpdatedAt,
                    x.s.TotalWeightGram,
                    x.s.WorkmanshipMilyem,
                    VariantId = x.v != null ? x.v.Id : (int?)null,
                    VariantName = x.v != null ? x.v.Name : null,
                    VariantAyar = x.v != null ? x.v.Ayar : null,
                    VariantColor = x.v != null ? x.v.Color : null,
                    VariantBrand = x.v != null ? x.v.Brand : null,
                    VariantGram = x.s.Gram,
                    TypeId = x.t != null ? x.t.Id : (int?)null,
                    TypeName = x.t != null ? x.t.Name : null,
                    CategoryName = x.c != null ? x.c.Name : null,
                    BranchId = x.b != null ? x.b.Id : (int?)null,
                    BranchName = x.b != null ? x.b.Name : null
                })
                .ToListAsync(ct);

            var items = rows.Select(x =>
            {
                var hamMilyem = AyarMilyemHelper.GetMilyemFromAyar(x.VariantAyar);
                int? totalMilyem = null;
                if (hamMilyem.HasValue || x.WorkmanshipMilyem.HasValue)
                    totalMilyem = (hamMilyem ?? 0) + (x.WorkmanshipMilyem ?? 0);

                return new StockDto
                {
                    Id = x.StockId,
                    Quantity = x.Quantity,
                    Barcode = x.Barcode ?? string.Empty,
                    QrCode = x.QrCode,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    TotalWeight = x.TotalWeightGram,
                    WorkmanshipMilyem = x.WorkmanshipMilyem,
                    TotalMilyem = totalMilyem,
                    Branch = new StockDto.BranchBrief
                    {
                        Id = x.BranchId,
                        Name = x.BranchName
                    },
                    ProductVariant = new StockDto.VariantBrief
                    {
                        Id = x.VariantId,
                        Name = x.VariantName,
                        Ayar = x.VariantAyar,
                        Color = x.VariantColor,
                        Brand = x.VariantBrand,
                        Gram = x.VariantGram,
                        ProductTypeId = x.TypeId,
                        ProductTypeName = x.TypeName,
                        CategoryName = x.CategoryName
                    }
                };
            }).ToList();

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
                    ToplamAgirlik = g.Sum(z => z.s.TotalWeightGram)
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
                        ToplamAgirlik = x.ToplamAgirlik
                    })
                    .ToListAsync(ct)
            };

            return ApiResult<StockVariantDetailByStoreDto>.Ok(dto, "Detay", 200);
        }

        // ------------ CRUD (sende zaten var; aynen koruyorum) ------------
        public async Task<ApiResult<StockDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
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

        /// <summary>
        /// Stok oluşturur veya merge eder.
        /// Eğer aynı ProductVariantId + BranchId + fiziksel özellikler varsa, mevcut quantity'i artırır.
        /// Yoksa yeni stok satırı oluşturur.
        /// PurchasePrice varsa otomatik olarak Purchase kaydı oluşturur (skipPurchaseCreation=false ise).
        /// </summary>
        public async Task<ApiResult<StockDto>> CreateAsync(StockCreateDto dto, CancellationToken ct = default, bool skipPurchaseCreation = false)
        {
            // BranchId belirleme
            var branchId = dto.BranchId ?? _user.BranchId;
            if (!branchId.HasValue)
                return ApiResult<StockDto>.Fail("BranchId belirlenemedi.", statusCode: 400);

            if (dto.Quantity <= 0)
                return ApiResult<StockDto>.Fail("Quantity >= 1 olmalıdır.", statusCode: 400);

            var effectiveTotalWeightGram = dto.TotalWeightGram;
            if (effectiveTotalWeightGram <= 0 && dto.Gram.HasValue && dto.Gram.Value > 0)
            {
                effectiveTotalWeightGram = dto.Gram.Value;
            }

            if (effectiveTotalWeightGram <= 0)
                return ApiResult<StockDto>.Fail("TotalWeightGram 0'dan büyük olmalıdır.", statusCode: 400);

            // Transaction başlat (PurchasePrice varsa)
            using var transaction = (dto.PurchasePrice.HasValue && dto.PurchasePrice.Value > 0 && !skipPurchaseCreation)
                ? await _db.Database.BeginTransactionAsync(ct)
                : null;

            Guid stockId;
            
            try
            {
                // Merge Criteria: Aynı varyant + şube
                var match = await _db.Stocks.FirstOrDefaultAsync(s =>
                    s.ProductVariantId == dto.ProductVariantId &&
                    s.BranchId == branchId
                , ct);

                if (match != null)
                {
                    // MERGE: Mevcut quantity'i artır
                    match.Quantity = (match.Quantity ?? 0) + dto.Quantity;
                    match.TotalWeightGram += effectiveTotalWeightGram;
                    match.UpdatedAt = DateTime.UtcNow;

                    if (dto.GenerateQrCode && string.IsNullOrWhiteSpace(match.QrCode))
                    {
                        match.QrCode = BuildQrPayload(match);
                    }
                    else if (!string.IsNullOrWhiteSpace(dto.QrCode))
                    {
                        match.QrCode = dto.QrCode;
                    }

                    await _db.SaveChangesAsync(ct);

                    stockId = match.Id;

                    // PurchasePrice varsa ve skipPurchaseCreation false ise Purchase kaydı oluştur
                    if (dto.PurchasePrice.HasValue && dto.PurchasePrice.Value > 0 && !skipPurchaseCreation)
                    {
                        await CreatePurchaseRecordAsync(stockId, branchId.Value, dto.Quantity, effectiveTotalWeightGram, dto.PurchasePrice.Value, ct);
                    }

                    if (transaction != null)
                        await transaction.CommitAsync(ct);

                    var merged = await GetByIdAsync(match.Id, ct);
                    merged.StatusCode = 200;
                    merged.Message = dto.PurchasePrice.HasValue && dto.PurchasePrice.Value > 0 && !skipPurchaseCreation
                        ? (dto.GenerateQrCode ? "Mevcut stok güncellendi, QR kod yenilendi ve alış kaydı oluşturuldu." : "Mevcut stok güncellendi ve alış kaydı oluşturuldu.")
                        : (dto.GenerateQrCode ? "Mevcut stok güncellendi ve QR kod yenilendi." : "Mevcut stok güncellendi (quantity artırıldı)");
                    return merged;
                }

                // YENİ KAYIT: Barcode kontrolü
                var barcode = string.IsNullOrWhiteSpace(dto.Barcode)
                    ? await GenerateUniqueBarcodeAsync(branchId.Value, ct)
                    : dto.Barcode.Trim();

                var barcodeExists = await _db.Stocks.AnyAsync(x => x.Barcode == barcode, ct);
                if (barcodeExists)
                {
                    if (transaction != null)
                        await transaction.RollbackAsync(ct);
                    return ApiResult<StockDto>.Fail("Bu barkod zaten kullanılıyor.", statusCode: 409);
                }

                var now = DateTime.UtcNow;

                var entity = new Domain.Entities.Stocks
                {
                    ProductVariantId = dto.ProductVariantId,
                    BranchId = branchId,
                    Quantity = dto.Quantity,
                    TotalWeightGram = effectiveTotalWeightGram,
                    Barcode = barcode,
                    QrCode = dto.GenerateQrCode && string.IsNullOrWhiteSpace(dto.QrCode)
                        ? null
                        : dto.QrCode,
                    Gram = dto.Gram,
                    Thickness = dto.Thickness,
                    Width = dto.Width,
                    StoneType = dto.StoneType,
                    Carat = dto.Carat,
                    WorkmanshipMilyem = dto.WorkmanshipMilyem,
                    Color = dto.Color,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _db.Stocks.Add(entity);
                await _db.SaveChangesAsync(ct);

                if (dto.GenerateQrCode)
                {
                    entity.QrCode = BuildQrPayload(entity);
                    entity.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                }

                stockId = entity.Id;

                // PurchasePrice varsa ve skipPurchaseCreation false ise Purchase kaydı oluştur
                if (dto.PurchasePrice.HasValue && dto.PurchasePrice.Value > 0 && !skipPurchaseCreation)
                {
                    await CreatePurchaseRecordAsync(stockId, branchId.Value, dto.Quantity, effectiveTotalWeightGram, dto.PurchasePrice.Value, ct);
                }

                if (transaction != null)
                    await transaction.CommitAsync(ct);

                var created = await GetByIdAsync(entity.Id, ct);
                created.StatusCode = 201;
                created.Message = dto.PurchasePrice.HasValue && dto.PurchasePrice.Value > 0 && !skipPurchaseCreation
                    ? "Yeni stok oluşturuldu ve alış kaydı oluşturuldu"
                    : "Yeni stok oluşturuldu";
                return created;
            }
            catch
            {
                if (transaction != null)
                    await transaction.RollbackAsync(ct);
                throw;
            }
        }

        private async Task CreatePurchaseRecordAsync(Guid stockId, int branchId, int quantity, decimal totalWeightGram, decimal purchasePrice, CancellationToken ct)
        {
            var userId = _user.UserId;
            if (!userId.HasValue)
                throw new InvalidOperationException("Kullanıcı bilgisi bulunamadı.");

            // Purchase kaydı oluştur
            var purchase = new Domain.Entities.Purchases
            {
                UserId = userId,
                BranchId = branchId,
                CustomerId = null,
                PaymentMethodId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Purchases.Add(purchase);
            await _db.SaveChangesAsync(ct);

            // PurchaseDetails oluştur
            _db.PurchaseDetails.Add(new Domain.Entities.PurchaseDetails
            {
                PurchaseId = purchase.Id,
                StockId = stockId,
                Quantity = quantity,
                PurchasePrice = purchasePrice,
                TotalWeightGram = totalWeightGram,
                UpdatedAt = DateTime.UtcNow
            });

            // Lifecycle kaydı oluştur
            var purchaseActionId = await _db.LifecycleActions
                .Where(x => x.Name == "Purchase")
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct);

            _db.ProductLifecycles.Add(new Domain.Entities.ProductLifecycles
            {
                StockId = stockId,
                UserId = userId,
                ActionId = purchaseActionId,
                Notes = $"Stok girişi (PurchaseId:{purchase.Id})",
                Timestamp = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }

        public async Task<ApiResult<bool>> UpdateAsync(Guid id, StockUpdateDto dto, CancellationToken ct = default)
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
            
            // Fiziksel özellikler güncelleme
            if (dto.Gram.HasValue) entity.Gram = dto.Gram;
            if (dto.Thickness.HasValue) entity.Thickness = dto.Thickness;
            if (dto.Width.HasValue) entity.Width = dto.Width;
            if (dto.StoneType != null) entity.StoneType = dto.StoneType;
            if (dto.Carat.HasValue) entity.Carat = dto.Carat;
            if (dto.WorkmanshipMilyem.HasValue) entity.WorkmanshipMilyem = dto.WorkmanshipMilyem;
            if (dto.Color != null) entity.Color = dto.Color;
            
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        public async Task<ApiResult<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
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

        public async Task<ApiResult<bool>> HardDeleteAsync(Guid id, CancellationToken ct = default)
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

        /// <summary>
        /// En çok satılan ürünleri (ProductVariant bazında) getirir.
        /// </summary>
        /// <param name="top">Kaç adet gösterilsin (default 10)</param>
        /// <param name="days">Son kaç günün satışları (default 30)</param>
        /// <param name="onlyMarked">Sadece IsFavorite=true olanlar mı (default false)</param>
        /// <param name="ct">İşlem iptal belirteci.</param>
        public async Task<ApiResult<List<FavoriteProductDto>>> GetFavoritesAsync(
            int top = 10,
            int days = 30,
            bool onlyMarked = false,
            CancellationToken ct = default)
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);

            var query =
                from sd in _db.SaleDetails.AsNoTracking()
                join s in _db.Sales.AsNoTracking() on sd.SaleId equals s.Id
                join st in _db.Stocks.AsNoTracking() on sd.StockId equals st.Id
                join pv in _db.ProductVariants.AsNoTracking() on st.ProductVariantId equals pv.Id
                where s.CreatedAt >= fromDate
                group sd by new { pv.Id, pv.Name, pv.Ayar, pv.Color, pv.Brand, pv.IsFavorite } into g
                orderby g.Sum(x => x.Quantity) descending
                select new FavoriteProductDto
                {
                    VariantId = g.Key.Id,
                    VariantName = g.Key.Name,
                    Ayar = g.Key.Ayar,
                    Color = g.Key.Color,
                    Brand = g.Key.Brand,
                    TotalSoldQty = g.Sum(x => x.Quantity ?? 0),
                    IsFavorite = g.Key.IsFavorite
                };

            if (onlyMarked)
                query = query.Where(x => x.IsFavorite);

            var result = await query.Take(top).ToListAsync(ct);

            return ApiResult<List<FavoriteProductDto>>.Ok(result, "Favori ürünler listelendi", 200);
        }

        private async Task<string> GenerateUniqueBarcodeAsync(int branchId, CancellationToken ct)
        {
            string barcode;
            do
            {
                var randomSuffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(3));
                barcode = $"STK-{branchId:D3}-{DateTime.UtcNow:yyMMddHHmmssfff}-{randomSuffix}";
            }
            while (await _db.Stocks.AnyAsync(x => x.Barcode == barcode, ct));

            return barcode;
        }

        private string BuildQrPayload(Domain.Entities.Stocks entity)
        {
            // QR kod içinde sadece URL olacak: {BaseUrl}{StockId}
            var qrUrl = $"{_qrOptions.BaseUrl.TrimEnd('/')}/{entity.Id}";
            
            // QR kod görseli oluştur
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);
            
            // Base64 string olarak döndür
            return Convert.ToBase64String(qrCodeBytes);
        }

        // ---- helpers ----
        private async Task<ApiResult<StockDto>> MapToDto(Domain.Entities.Stocks s, CancellationToken ct)
        {
            var v = await _db.ProductVariants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == s.ProductVariantId, ct);
            var t = v?.ProductTypeId == null ? null :
                    await _db.ProductTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == v.ProductTypeId, ct);
            var c = t?.CategoryId == null ? null :
                    await _db.ProductCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == t.CategoryId, ct);

            // Ham milyem hesaplama (varyanttan gelen ayar bilgisine göre)
            var hamMilyem = AyarMilyemHelper.GetMilyemFromAyar(v?.Ayar);
            
            // Toplam milyem = Ham milyem + İşçilik milyemi
            int? totalMilyem = null;
            if (hamMilyem.HasValue || s.WorkmanshipMilyem.HasValue)
            {
                totalMilyem = (hamMilyem ?? 0) + (s.WorkmanshipMilyem ?? 0);
            }

            var dto = new StockDto
            {
                Id = s.Id,
                Quantity = s.Quantity,
                Barcode = s.Barcode,
                QrCode = s.QrCode,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                TotalWeight = s.TotalWeightGram,
                WorkmanshipMilyem = s.WorkmanshipMilyem,
                TotalMilyem = totalMilyem,
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

