using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Sales;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Domain.Entities;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.SalesService
{
    /// <summary>Satış işlemleri: stok çıkışı + fiş/detay + (ops.) banka hareketi + lifecycle.</summary>
    public sealed class SalesService : ISalesService
    {
        private readonly AppDbContext _db;
        public SalesService(AppDbContext db) => _db = db;

        public async Task<ApiResult<SaleResultDto>> CreateAsync(SaleCreateDto dto, CancellationToken ct = default)
        {
            // ---- VALIDATION ----
            if (dto.Items is null || dto.Items.Count == 0)
                return ApiResult<SaleResultDto>.Fail("Kalem yok.", statusCode: 400);

            if (!dto.UserId.HasValue)
                return ApiResult<SaleResultDto>.Fail("UserId belirtilmeli (veya controller CurrentUser ile set etmelidir).", statusCode: 400);

            if (dto.BranchId <= 0)
                return ApiResult<SaleResultDto>.Fail("BranchId geçersiz.", statusCode: 400);

            // ---- CUSTOMER INLINE UPSERT ----
            int? customerId = dto.CustomerId;
            if (!customerId.HasValue)
            {
                // Ad+Telefon verilmişse mevcut müşteriyi bul, yoksa oluştur.
                if (!string.IsNullOrWhiteSpace(dto.CustomerName))
                {
                    var existing = await _db.Customers
                        .Where(x => x.IsDeleted == false && x.Name == dto.CustomerName &&
                                   (dto.CustomerPhone == null || x.Phone == dto.CustomerPhone))
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefaultAsync(ct);

                    if (existing is null)
                    {
                        var newCust = new Customers
                        {
                            Name = dto.CustomerName!,
                            Phone = dto.CustomerPhone,
                            Note = string.IsNullOrWhiteSpace(dto.CustomerNationalId)
                                ? null
                                : $"TC:{dto.CustomerNationalId}"
                        };
                        _db.Customers.Add(newCust);
                        await _db.SaveChangesAsync(ct);
                        customerId = newCust.Id;
                    }
                    else
                    {
                        // Gerekirse TC bilgisini note’a ekle/merge
                        if (!string.IsNullOrWhiteSpace(dto.CustomerNationalId) &&
                            (existing.Note == null || !existing.Note.Contains(dto.CustomerNationalId)))
                        {
                            existing.Note = (existing.Note ?? string.Empty) + $" TC:{dto.CustomerNationalId}";
                            _db.Customers.Update(existing);
                            await _db.SaveChangesAsync(ct);
                        }
                        customerId = existing.Id;
                    }
                }
                // isim de yoksa müşterisiz satışa izin veriyoruz (customerId null)
            }

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            // ---- SALES (başlık) ----
            var sale = new Sales
            {
                UserId = dto.UserId,
                BranchId = dto.BranchId,
                CustomerId = customerId,
                PaymentMethodId = dto.PaymentMethodId
            };
            _db.Sales.Add(sale);
            await _db.SaveChangesAsync(ct);

            var touchedStocks = new List<int>();

            // ---- (OPSİYONEL) TOPLU LOCK — aynı stoğu iki işlem aynı anda düşmesin ----
            var distinctStockIds = dto.Items.Select(i => i.StockId).Distinct().ToArray();
            // Npgsql’de FOR UPDATE:
            //var lockedStocks = await _db.Stocks
            //    .FromSqlInterpolated($@"SELECT * FROM public.stocks WHERE id = ANY({distinctStockIds}) FOR UPDATE")
            //    .ToListAsync(ct);

            // ---- KALEMLER ----
            foreach (var i in dto.Items)
            {
                var stock = await _db.Stocks
                    .Where(s => s.Id == i.StockId)
                    .FirstOrDefaultAsync(ct);

                if (stock is null)
                    return ApiResult<SaleResultDto>.Fail($"Stok bulunamadı: {i.StockId}", statusCode: 404);

                var qty = stock.Quantity ?? 0;
                if (i.Quantity <= 0)
                    return ApiResult<SaleResultDto>.Fail($"Geçersiz adet: {i.Quantity}", statusCode: 400);

                if (qty < i.Quantity)
                    return ApiResult<SaleResultDto>.Fail($"Yetersiz stok (id:{i.StockId})", statusCode: 409);

                stock.Quantity = qty - i.Quantity;
                stock.UpdatedAt = DateTime.UtcNow;

                _db.SaleDetails.Add(new SaleDetails
                {
                    SaleId = sale.Id,
                    Quantity = i.Quantity,
                    SoldPrice = i.SoldPrice,
                    StockId = stock.Id
                });

                // Lifecycle (Çıkış)
                _db.ProductLifecycles.Add(new ProductLifecycles
                {
                    StockId = stock.Id,
                    UserId = dto.UserId,
                    Notes = "Sale",
                    Timestamp = DateTime.UtcNow,
                    ActionId = null
                });

                touchedStocks.Add(stock.Id);
            }

            // ---- (OPSİYONEL) POS / BANKA HAREKETİ ----
            if (dto.BankId.HasValue && dto.ExpectedAmount.HasValue)
            {
                _db.BankTransactions.Add(new BankTransactions
                {
                    SaleId = sale.Id,
                    BankId = dto.BankId,
                    CommissionRate = dto.CommissionRate,
                    ExpectedAmount = dto.ExpectedAmount,
                    Status = "pending"
                });
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return ApiResult<SaleResultDto>.Ok(new SaleResultDto
            {
                Id = sale.Id,
                CreatedAt = sale.CreatedAt,
                StockIds = touchedStocks
            }, "Satış kaydedildi", 201);
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


    }
}
