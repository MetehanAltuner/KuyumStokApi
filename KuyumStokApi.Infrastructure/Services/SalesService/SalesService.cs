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
            if (dto.Items.Count == 0)
                return ApiResult<SaleResultDto>.Fail("Kalem yok.", statusCode: 400);

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            var sale = new Sales
            {
                UserId = dto.UserId,
                BranchId = dto.BranchId,
                CustomerId = dto.CustomerId,
                PaymentMethodId = dto.PaymentMethodId
            };
            _db.Sales.Add(sale);
            await _db.SaveChangesAsync(ct);

            var touchedStocks = new List<int>();

            foreach (var i in dto.Items)
            {
                // stok LOCK için SELECT ... FOR UPDATE muadili:
                var stock = await _db.Stocks
                    .Where(s => s.Id == i.StockId)
                    .FirstOrDefaultAsync(ct);

                if (stock is null)
                    return ApiResult<SaleResultDto>.Fail($"Stok bulunamadı: {i.StockId}", statusCode: 404);

                var qty = stock.Quantity ?? 0;
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

                // lifecycle: “Çıkış”
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

            // opsiyonel: kart/pos satışlarında bank_transactions
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
        public async Task<ApiResult<PagedResult<SaleListDto>>> GetPagedAsync(SaleFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var size = Math.Clamp(filter.PageSize, 1, 200);

            var q =
                from s in _db.Sales.AsNoTracking()
                join b in _db.Branches.AsNoTracking() on s.BranchId equals b.Id into jb
                from b in jb.DefaultIfEmpty()
                join u in _db.Users.AsNoTracking() on s.UserId equals u.Id into ju
                from u in ju.DefaultIfEmpty()
                join c in _db.Customers.AsNoTracking() on s.CustomerId equals c.Id into jc
                from c in jc.DefaultIfEmpty()
                join pm in _db.PaymentMethods.AsNoTracking() on s.PaymentMethodId equals pm.Id into jpm
                from pm in jpm.DefaultIfEmpty()
                select new { s, BranchName = b!.Name, UserName = u!.Username, CustomerName = c!.Name, PaymentMethod = pm!.Name };

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
                    Id = x.s.Id,
                    CreatedAt = x.s.CreatedAt,
                    BranchId = x.s.BranchId,
                    BranchName = x.BranchName,
                    UserId = x.s.UserId,
                    UserName = x.UserName,
                    CustomerId = x.s.CustomerId,
                    CustomerName = x.CustomerName,
                    PaymentMethodId = x.s.PaymentMethodId,
                    PaymentMethod = x.PaymentMethod,
                    ItemCount = _db.SaleDetails.Where(d => d.SaleId == x.s.Id).Count(),
                    TotalAmount = _db.SaleDetails
                        .Where(d => d.SaleId == x.s.Id)
                        .Sum(d => (decimal?)(d.SoldPrice ?? 0) * (decimal?)(d.Quantity ?? 0)) ?? 0m
                })
                .ToListAsync(ct);

            var result = new PagedResult<SaleListDto>
            {
                Items = items,
                Page = page,
                PageSize = size,
                TotalCount = total
            };

            return ApiResult<PagedResult<SaleListDto>>.Ok(result, "Satış listesi", 200);
        }

        public async Task<ApiResult<SaleDetailDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var head =
                await (from s in _db.Sales.AsNoTracking()
                       join b in _db.Branches.AsNoTracking() on s.BranchId equals b.Id into jb
                       from b in jb.DefaultIfEmpty()
                       join u in _db.Users.AsNoTracking() on s.UserId equals u.Id into ju
                       from u in ju.DefaultIfEmpty()
                       join c in _db.Customers.AsNoTracking() on s.CustomerId equals c.Id into jc
                       from c in jc.DefaultIfEmpty()
                       join pm in _db.PaymentMethods.AsNoTracking() on s.PaymentMethodId equals pm.Id into jpm
                       from pm in jpm.DefaultIfEmpty()
                       where s.Id == id
                       select new
                       {
                           s,
                           BranchName = b!.Name,
                           UserName = u!.Username,
                           CustomerName = c!.Name,
                           PaymentMethod = pm!.Name
                       }).FirstOrDefaultAsync(ct);

            if (head is null)
                return ApiResult<SaleDetailDto>.Fail("Satış bulunamadı", statusCode: 404);

            var lines =
                await (from d in _db.SaleDetails.AsNoTracking()
                       join st in _db.Stocks.AsNoTracking() on d.StockId equals st.Id
                       join pv in _db.ProductVariants.AsNoTracking() on st.ProductVariantId equals pv.Id into jpv
                       from pv in jpv.DefaultIfEmpty()
                       where d.SaleId == id
                       select new SaleDetailLineDto
                       {
                           Id = d.Id,
                           StockId = st.Id,
                           Barcode = st.Barcode,
                           Quantity = d.Quantity ?? 0,
                           SoldPrice = d.SoldPrice,
                           ProductVariantId = st.ProductVariantId,
                           VariantDisplay =
                               pv == null ? null :
                               $"{pv.Brand ?? ""} {pv.Ayar ?? ""} {(st.Gram ?? 0):0.##}g"
                       }).ToListAsync(ct);

            var dto = new SaleDetailDto
            {
                Id = head.s.Id,
                CreatedAt = head.s.CreatedAt,
                BranchId = head.s.BranchId,
                BranchName = head.BranchName,
                UserId = head.s.UserId,
                UserName = head.UserName,
                CustomerId = head.s.CustomerId,
                CustomerName = head.CustomerName,
                PaymentMethodId = head.s.PaymentMethodId,
                PaymentMethod = head.PaymentMethod,
                ItemCount = lines.Count,
                TotalAmount = lines.Sum(l => (l.SoldPrice ?? 0) * l.Quantity),
                Lines = lines
            };

            return ApiResult<SaleDetailDto>.Ok(dto, "Detay", 200);
        }
    }
}
