using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Receipts;
using KuyumStokApi.Application.DTOs.Sales;
using KuyumStokApi.Application.DTOs.Stocks;
using KuyumStokApi.Application.Interfaces.Auth;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Domain.Entities;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.SalesService
{
    /// <summary>Satış işlemleri: stok çıkışı + fiş/detay + çoklu ödeme + lifecycle.</summary>
    public sealed class SalesService : ISalesService
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUserContext _currentUser;
        private readonly IStocksService _stocksService;
        private readonly ILogger<SalesService> _logger;

        public SalesService(
            AppDbContext db,
            ICurrentUserContext currentUser,
            IStocksService stocksService,
            ILogger<SalesService> logger)
        {
            _db = db;
            _currentUser = currentUser;
            _stocksService = stocksService;
            _logger = logger;
        }

        public async Task<ApiResult<UnifiedReceiptResultDto>> CreateUnifiedAsync(UnifiedReceiptCreateDto dto, CancellationToken ct = default)
        {
            if (!_currentUser.IsAuthenticated)
            {
                _logger.LogWarning("❌ Fiş işlemi reddedildi: Kullanıcı kimlik doğrulanamadı.");
                return ApiResult<UnifiedReceiptResultDto>.Fail("Kullanıcı kimliği doğrulanamadı.", statusCode: 401);
            }

            if (!_currentUser.UserId.HasValue || !_currentUser.BranchId.HasValue)
            {
                _logger.LogWarning("❌ Fiş işlemi reddedildi: UserId veya BranchId bulunamadı.");
                return ApiResult<UnifiedReceiptResultDto>.Fail("Kullanıcı bilgileri eksik (UserId veya BranchId).", statusCode: 401);
            }

            var currentUserId = _currentUser.UserId.Value;
            var currentBranchId = dto.BranchId ?? _currentUser.BranchId.Value;

            _logger.LogInformation("🧾 Birleşik fiş işlemi başlatıldı. Mode: {Mode}, User: {UserId}, Branch: {BranchId}",
                dto.Mode, currentUserId, currentBranchId);

            var hasSaleItems = dto.SaleItems is { Count: > 0 };
            var hasPurchaseItems = dto.PurchaseItems is { Count: > 0 };

            if (!hasSaleItems && !hasPurchaseItems)
                return ApiResult<UnifiedReceiptResultDto>.Fail("En az bir satış veya alış kalemi gereklidir.", statusCode: 400);

            var totalPayment = (dto.Cash ?? 0m) + (dto.Eft ?? 0m) + (dto.Pos ?? 0m);

            if (totalPayment <= 0)
                return ApiResult<UnifiedReceiptResultDto>.Fail("Toplam ödeme tutarı sıfırdan büyük olmalıdır.", statusCode: 400);

            if ((dto.Pos ?? 0m) > 0 && !dto.BankId.HasValue)
                return ApiResult<UnifiedReceiptResultDto>.Fail("POS ödemesi için banka seçimi zorunludur.", statusCode: 400);

            var customerId = await UpsertCustomerAsync(dto, ct);

            using var tx = await _db.Database.BeginTransactionAsync(ct);
            int? saleId = null;
            int? purchaseId = null;
            var affectedStockIds = new List<int>();

            try
            {
                if (hasSaleItems)
                {
                var saleResult = await ProcessSaleAsync(dto, currentUserId, currentBranchId, customerId, ct);
                    saleId = saleResult.SaleId;
                    affectedStockIds.AddRange(saleResult.AffectedStockIds);
                }

                if (hasPurchaseItems)
                {
                    var purchaseResult = await ProcessPurchaseAsync(dto, currentUserId, currentBranchId, customerId, ct);
                    purchaseId = purchaseResult.PurchaseId;
                    affectedStockIds.AddRange(purchaseResult.AffectedStockIds);
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation("✅ Birleşik fiş işlemi tamamlandı. SaleId: {SaleId}, PurchaseId: {PurchaseId}",
                    saleId, purchaseId);

                return ApiResult<UnifiedReceiptResultDto>.Ok(new UnifiedReceiptResultDto
                {
                    ReceiptId = saleId ?? purchaseId ?? 0,
                    SaleId = saleId,
                    PurchaseId = purchaseId,
                    CreatedAt = DateTime.UtcNow,
                    AffectedStockIds = affectedStockIds.Distinct().ToList()
                }, "Birleşik fiş işlemi başarıyla tamamlandı.", 201);
            }
            catch (InvalidOperationException ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogWarning(ex, "⚠️ Birleşik fiş doğrulama hatası: {Message}", ex.Message);
                return ApiResult<UnifiedReceiptResultDto>.Fail(ex.Message, statusCode: 400);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _logger.LogError(ex, "❌ Birleşik fiş işlemi sırasında beklenmeyen hata oluştu.");
                return ApiResult<UnifiedReceiptResultDto>.Fail("Birleşik fiş işlemi sırasında beklenmeyen bir hata oluştu.", statusCode: 500);
            }
        }
        public async Task<ApiResult<PagedResult<SaleListDto>>> GetPagedAsync(
    SaleFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var size = Math.Clamp(filter.PageSize, 1, 200);

            var q =
                from d in _db.SaleDetails.AsNoTracking()
                join s in _db.Sales.AsNoTracking() on d.SaleId equals s.Id
                join st in _db.Stocks.AsNoTracking() on d.StockId equals st.Id
                join pv in _db.ProductVariants.AsNoTracking() on st.ProductVariantId equals pv.Id into jpv
                from pv in jpv.DefaultIfEmpty()
                join b in _db.Branches.AsNoTracking() on s.BranchId equals b.Id into jb
                from b in jb.DefaultIfEmpty()
                join u in _db.Users.AsNoTracking() on s.UserId equals u.Id into ju
                from u in ju.DefaultIfEmpty()
                select new { d, s, st, pv, b, u };

            if (filter.BranchId.HasValue) q = q.Where(x => x.s.BranchId == filter.BranchId);
            if (filter.UserId.HasValue) q = q.Where(x => x.s.UserId == filter.UserId);
            if (filter.CustomerId.HasValue) q = q.Where(x => x.s.CustomerId == filter.CustomerId);
            if (filter.PaymentMethodId.HasValue) q = q.Where(x => x.s.PaymentMethodId == filter.PaymentMethodId);
            if (filter.FromUtc.HasValue) q = q.Where(x => x.s.CreatedAt >= filter.FromUtc);
            if (filter.ToUtc.HasValue) q = q.Where(x => x.s.CreatedAt <= filter.ToUtc);

            var total = await q.LongCountAsync(ct);

            var items = await q
                .OrderByDescending(x => x.s.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(x => new SaleListDto
                {
                    SaleId = x.s.Id,
                    LineId = x.d.Id,
                    CreatedAt = x.s.CreatedAt,
                    BranchId = x.s.BranchId,
                    BranchName = x.b != null ? x.b.Name : null,
                    UserId = x.s.UserId,
                    UserName = x.u != null ? x.u.Username : null,
                    StockId = x.st.Id,
                    ProductName = x.pv != null ? x.pv.Name : null,
                    Ayar = x.pv != null ? x.pv.Ayar : null,
                    Renk = x.pv != null ? x.pv.Color : null,
                    AgirlikGram = x.st.Gram,
                    Quantity = x.d.Quantity ?? 0,
                    SoldPrice = x.d.SoldPrice
                })
                .ToListAsync(ct);

            var result = new PagedResult<SaleListDto>
            {
                Items = items,
                Page = page,
                PageSize = size,
                TotalCount = total
            };

            return ApiResult<PagedResult<SaleListDto>>.Ok(result, "Satış kalem listesi", 200);
        }

        public async Task<ApiResult<SaleLineDetailDto>> GetLineByIdAsync(int lineId, CancellationToken ct = default)
        {
            var q =
                from d in _db.SaleDetails.AsNoTracking()
                join s in _db.Sales.AsNoTracking() on d.SaleId equals s.Id
                join st in _db.Stocks.AsNoTracking() on d.StockId equals st.Id
                join pv in _db.ProductVariants.AsNoTracking() on st.ProductVariantId equals pv.Id into jpv
                from pv in jpv.DefaultIfEmpty()
                join pm in _db.PaymentMethods.AsNoTracking() on s.PaymentMethodId equals pm.Id into jpm
                from pm in jpm.DefaultIfEmpty()
                where d.Id == lineId
                select new SaleLineDetailDto
                {
                    SaleId = s.Id,
                    LineId = d.Id,
                    CreatedAt = s.CreatedAt,
                    PaymentMethod = pm != null ? pm.Name : null,

                    StockId = st.Id,
                    ProductName = pv != null ? pv.Name : null,
                    Ayar = pv != null ? pv.Ayar : null,
                    Renk = pv != null ? pv.Color : null,
                    AgirlikGram = st.Gram,

                    ListeFiyati = null,                 // Şemada katalog/list price yok
                    SatisFiyati = d.SoldPrice
                };

            var dto = await q.FirstOrDefaultAsync(ct);
            if (dto is null)
                return ApiResult<SaleLineDetailDto>.Fail("Satış kalemi bulunamadı", statusCode: 404);

            return ApiResult<SaleLineDetailDto>.Ok(dto, "Satış kalem detayı", 200);
        }

        #region Helpers
        private async Task<int?> UpsertCustomerAsync(UnifiedReceiptCreateDto dto, CancellationToken ct)
        {
            if (dto.CustomerId.HasValue)
                return dto.CustomerId;

            if (string.IsNullOrWhiteSpace(dto.CustomerName))
                return null;

            Customers? existing = null;
            if (!string.IsNullOrWhiteSpace(dto.CustomerNationalId))
            {
                existing = await _db.Customers
                    .Where(x => x.NationalId == dto.CustomerNationalId && !x.IsDeleted)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(ct);
            }

            if (existing == null && !string.IsNullOrWhiteSpace(dto.CustomerPhone))
            {
                existing = await _db.Customers
                    .Where(x => x.Name == dto.CustomerName && x.Phone == dto.CustomerPhone && !x.IsDeleted)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(ct);
            }

            if (existing == null)
            {
                var newCustomer = new Customers
                {
                    Name = dto.CustomerName,
                    Phone = dto.CustomerPhone,
                    NationalId = dto.CustomerNationalId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false
                };
                _db.Customers.Add(newCustomer);
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("➕ Yeni müşteri oluşturuldu: {CustomerId} - {CustomerName}", newCustomer.Id, dto.CustomerName);
                return newCustomer.Id;
            }

            _logger.LogInformation("♻️ Mevcut müşteri kullanıldı: {CustomerId} - {CustomerName}", existing.Id, existing.Name);
            return existing.Id;
        }

        private async Task<(int SaleId, List<int> AffectedStockIds)> ProcessSaleAsync(
            UnifiedReceiptCreateDto dto,
            int userId,
            int branchId,
            int? customerId,
            CancellationToken ct)
        {
            _logger.LogInformation("🛒 Satış işlemleri başlatılıyor...");

            var saleActionId = await _db.LifecycleActions
                .Where(x => x.Name == "Sale")
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct);

            var sale = new Sales
            {
                UserId = userId,
                BranchId = branchId,
                CustomerId = customerId,
                PaymentMethodId = dto.PaymentMethodId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Sales.Add(sale);
            await _db.SaveChangesAsync(ct);

            var affectedStockIds = new List<int>();

            foreach (var item in dto.SaleItems!)
            {
                if (item.StockId <= 0)
                    throw new InvalidOperationException("Geçersiz StockId.");
                if (item.Quantity <= 0)
                    throw new InvalidOperationException($"Geçersiz adet: {item.Quantity}");
                if (item.SoldPrice <= 0)
                    throw new InvalidOperationException("SoldPrice 0'dan büyük olmalıdır.");

                var stock = await _db.Stocks
                    .Where(s => s.Id == item.StockId && s.BranchId == branchId)
                    .FirstOrDefaultAsync(ct);

                if (stock == null)
                    throw new InvalidOperationException($"Stok bulunamadı veya farklı şube: {item.StockId}");

                var currentQty = stock.Quantity ?? 0;
                if (currentQty < item.Quantity)
                    throw new InvalidOperationException($"Yetersiz stok (id:{item.StockId}, mevcut:{currentQty})");

                stock.Quantity = currentQty - item.Quantity;
                stock.UpdatedAt = DateTime.UtcNow;

                _db.SaleDetails.Add(new SaleDetails
                {
                    SaleId = sale.Id,
                    StockId = stock.Id,
                    Quantity = item.Quantity,
                    SoldPrice = item.SoldPrice
                });

                _db.ProductLifecycles.Add(new ProductLifecycles
                {
                    StockId = stock.Id,
                    UserId = userId,
                    ActionId = saleActionId,
                    Notes = $"Satış (SaleId:{sale.Id})",
                    Timestamp = DateTime.UtcNow
                });

                affectedStockIds.Add(stock.Id);
                _logger.LogInformation("📦 Stok düşüldü: StockId={StockId}, Adet={Qty}", stock.Id, item.Quantity);
            }

            await ProcessPaymentsAsync(sale.Id, dto, ct);

            _logger.LogInformation("✅ Satış işlemleri tamamlandı: SaleId={SaleId}", sale.Id);
            return (sale.Id, affectedStockIds);
        }

        private async Task<(int PurchaseId, List<int> AffectedStockIds)> ProcessPurchaseAsync(
            UnifiedReceiptCreateDto dto,
            int userId,
            int branchId,
            int? customerId,
            CancellationToken ct)
        {
            _logger.LogInformation("📥 Alış işlemleri başlatılıyor...");

            var purchaseActionId = await _db.LifecycleActions
                .Where(x => x.Name == "Purchase")
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct);

            var purchase = new Purchases
            {
                UserId = userId,
                BranchId = branchId,
                CustomerId = customerId,
                PaymentMethodId = dto.PaymentMethodId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Purchases.Add(purchase);
            await _db.SaveChangesAsync(ct);

            var affectedStockIds = new List<int>();

            foreach (var item in dto.PurchaseItems!)
            {
                if (item.ProductVariantId <= 0)
                    throw new InvalidOperationException("Alış kaleminde ProductVariantId zorunludur.");
                var targetBranchId = item.BranchId > 0 ? item.BranchId : branchId;
                if (targetBranchId <= 0)
                    throw new InvalidOperationException("Alış kaleminde BranchId zorunludur.");
                if (item.Quantity <= 0)
                    throw new InvalidOperationException($"Geçersiz adet: {item.Quantity}");
                if (item.PurchasePrice <= 0)
                    throw new InvalidOperationException("PurchasePrice 0'dan büyük olmalıdır.");

                var stockCreateDto = new StockCreateDto
                {
                    ProductVariantId = item.ProductVariantId,
                    BranchId = targetBranchId,
                    Quantity = item.Quantity,
                    Barcode = item.Barcode,
                    QrCode = item.QrCode,
                    GenerateQrCode = item.GenerateQrCode,
                    Gram = item.Gram,
                    Thickness = item.Thickness,
                    Width = item.Width,
                    StoneType = item.StoneType,
                    Carat = item.Carat,
                    Milyem = item.Milyem,
                    Color = item.Color
                };

                var stockResult = await _stocksService.CreateAsync(stockCreateDto, ct);
                if (!stockResult.Success || stockResult.Data == null)
                    throw new InvalidOperationException($"Stok oluşturulamadı: {stockResult.Message}");

                var stockId = stockResult.Data.Id;

                _db.PurchaseDetails.Add(new PurchaseDetails
                {
                    PurchaseId = purchase.Id,
                    StockId = stockId,
                    Quantity = item.Quantity,
                    PurchasePrice = item.PurchasePrice
                });

                _db.ProductLifecycles.Add(new ProductLifecycles
                {
                    StockId = stockId,
                    UserId = userId,
                    ActionId = purchaseActionId,
                    Notes = $"Alış (PurchaseId:{purchase.Id})",
                    Timestamp = DateTime.UtcNow
                });

                affectedStockIds.Add(stockId);
                _logger.LogInformation("📦 Stok eklendi/güncellendi: StockId={StockId}, Adet={Qty}", stockId, item.Quantity);
            }

            _logger.LogInformation("✅ Alış işlemleri tamamlandı: PurchaseId={PurchaseId}", purchase.Id);
            return (purchase.Id, affectedStockIds);
        }

        private async Task ProcessPaymentsAsync(int saleId, UnifiedReceiptCreateDto dto, CancellationToken ct)
        {
            var paymentMethodIds = await _db.PaymentMethods
                .Where(x => x.IsDeleted == false && x.IsActive == true)
                .ToDictionaryAsync(x => x.Name, x => x.Id, ct);

            if ((dto.Cash ?? 0m) > 0)
            {
                if (!paymentMethodIds.TryGetValue("Nakit", out var cashMethodId))
                    throw new InvalidOperationException("'Nakit' ödeme yöntemi tanımlı değil.");

                _db.SalePayments.Add(new SalePayments
                {
                    SaleId = saleId,
                    PaymentMethodId = cashMethodId,
                    Amount = dto.Cash ?? 0m,
                    NetAmount = dto.Cash ?? 0m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                _logger.LogInformation("💵 Nakit ödeme eklendi: {Amount:F2} TL", dto.Cash ?? 0m);
            }

            if ((dto.Eft ?? 0m) > 0)
            {
                if (!paymentMethodIds.TryGetValue("Havale/EFT", out var eftMethodId))
                    throw new InvalidOperationException("'Havale/EFT' ödeme yöntemi tanımlı değil.");

                _db.SalePayments.Add(new SalePayments
                {
                    SaleId = saleId,
                    PaymentMethodId = eftMethodId,
                    Amount = dto.Eft ?? 0m,
                    NetAmount = dto.Eft ?? 0m,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                _logger.LogInformation("🏦 EFT/Havale ödeme eklendi: {Amount:F2} TL", dto.Eft ?? 0m);
            }

            if ((dto.Pos ?? 0m) > 0)
            {
                if (!paymentMethodIds.TryGetValue("Kredi Kartı", out var posMethodId))
                    throw new InvalidOperationException("'Kredi Kartı' ödeme yöntemi tanımlı değil.");

                var commissionRate = dto.POS_CommissionRate ?? 0m;
                var gross = dto.Pos ?? 0m;
                var netAmount = gross - (gross * commissionRate);

                _db.SalePayments.Add(new SalePayments
                {
                    SaleId = saleId,
                    PaymentMethodId = posMethodId,
                    BankId = dto.BankId,
                    Amount = gross,
                    CommissionRate = commissionRate,
                    NetAmount = netAmount,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                _logger.LogInformation("💳 POS ödeme eklendi: {Amount:F2} TL, Net: {Net:F2} TL", gross, netAmount);
            }
        }
        #endregion
    }
}
