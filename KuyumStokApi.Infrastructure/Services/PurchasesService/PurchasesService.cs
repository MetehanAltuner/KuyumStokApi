using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Purchase;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Domain.Entities;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.PurchasesService
{
    /// <summary>Alış işlemleri: stok girişi + fiş/detay + lifecycle.</summary>
    public sealed class PurchasesService : IPurchasesService
    {
        private readonly AppDbContext _db;
        public PurchasesService(AppDbContext db) => _db = db;

        public async Task<ApiResult<PurchaseResultDto>> CreateAsync(PurchaseCreateDto dto, CancellationToken ct = default)
        {
            if (dto.Items.Count == 0)
                return ApiResult<PurchaseResultDto>.Fail("Kalem yok.", statusCode: 400);

            using var tx = await _db.Database.BeginTransactionAsync(ct);

            var purchase = new Purchases
            {
                UserId = dto.UserId,
                BranchId = dto.BranchId,
                CustomerId = dto.CustomerId,
                PaymentMethodId = dto.PaymentMethodId
            };
            _db.Purchases.Add(purchase);
            await _db.SaveChangesAsync(ct);

            var createdStockIds = new List<int>();

            foreach (var i in dto.Items)
            {
                // barcode UNIQUE: varsa aynı branch/variant’ta birleştir; yoksa oluştur
                var stock = await _db.Stocks
                    .FirstOrDefaultAsync(s => s.Barcode == i.Barcode, ct);

                if (stock is null)
                {
                    stock = new Stocks
                    {
                        ProductVariantId = i.ProductVariantId,
                        BranchId = i.BranchId,
                        Barcode = i.Barcode,
                        Quantity = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Stocks.Add(stock);
                    await _db.SaveChangesAsync(ct);
                }
                else
                {
                    // güvenlik: branch/variant uyuşsun
                    if (stock.BranchId != i.BranchId || stock.ProductVariantId != i.ProductVariantId)
                        return ApiResult<PurchaseResultDto>.Fail($"Barkod çakışması: {i.Barcode}", statusCode: 409);
                }

                stock.Quantity = (stock.Quantity ?? 0) + i.Quantity;
                stock.UpdatedAt = DateTime.UtcNow;

                _db.PurchaseDetails.Add(new PurchaseDetails
                {
                    PurchaseId = purchase.Id,
                    Quantity = i.Quantity,
                    PurchasePrice = i.PurchasePrice,
                    StockId = stock.Id
                });

                // lifecycle: “Giriş”
                _db.ProductLifecycles.Add(new ProductLifecycles
                {
                    StockId = stock.Id,
                    UserId = dto.UserId,
                    Notes = "Purchase",
                    Timestamp = DateTime.UtcNow,
                    ActionId = null // istersen lifecycle_actions tablosundan “Purchase” id’sini kullan
                });

                createdStockIds.Add(stock.Id);
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return ApiResult<PurchaseResultDto>.Ok(new PurchaseResultDto
            {
                Id = purchase.Id,
                CreatedAt = purchase.CreatedAt,
                StockIds = createdStockIds
            }, "Alış kaydedildi", 201);
        }
        public async Task<ApiResult<PagedResult<PurchaseListDto>>> GetPagedAsync(PurchaseFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var size = Math.Clamp(filter.PageSize, 1, 200);

            var q =
                from p in _db.Purchases.AsNoTracking()
                join b in _db.Branches.AsNoTracking() on p.BranchId equals b.Id into jb
                from b in jb.DefaultIfEmpty()
                join u in _db.Users.AsNoTracking() on p.UserId equals u.Id into ju
                from u in ju.DefaultIfEmpty()
                join c in _db.Customers.AsNoTracking() on p.CustomerId equals c.Id into jc
                from c in jc.DefaultIfEmpty()
                join pm in _db.PaymentMethods.AsNoTracking() on p.PaymentMethodId equals pm.Id into jpm
                from pm in jpm.DefaultIfEmpty()
                select new { p, BranchName = b!.Name, UserName = u!.Username, CustomerName = c!.Name, PaymentMethod = pm!.Name };

            if (filter.BranchId.HasValue) q = q.Where(x => x.p.BranchId == filter.BranchId);
            if (filter.UserId.HasValue) q = q.Where(x => x.p.UserId == filter.UserId);
            if (filter.CustomerId.HasValue) q = q.Where(x => x.p.CustomerId == filter.CustomerId);
            if (filter.PaymentMethodId.HasValue) q = q.Where(x => x.p.PaymentMethodId == filter.PaymentMethodId);
            if (filter.FromUtc.HasValue) q = q.Where(x => x.p.CreatedAt >= filter.FromUtc);
            if (filter.ToUtc.HasValue) q = q.Where(x => x.p.CreatedAt <= filter.ToUtc);

            var total = await q.LongCountAsync(ct);

            // satır toplamı
            var items = await q
                .OrderByDescending(x => x.p.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(x => new PurchaseListDto
                {
                    Id = x.p.Id,
                    CreatedAt = x.p.CreatedAt,
                    BranchId = x.p.BranchId,
                    BranchName = x.BranchName,
                    UserId = x.p.UserId,
                    UserName = x.UserName,
                    CustomerId = x.p.CustomerId,
                    CustomerName = x.CustomerName,
                    PaymentMethodId = x.p.PaymentMethodId,
                    PaymentMethod = x.PaymentMethod,
                    ItemCount = _db.PurchaseDetails.Where(d => d.PurchaseId == x.p.Id).Count(),
                    TotalAmount = _db.PurchaseDetails
                        .Where(d => d.PurchaseId == x.p.Id)
                        .Sum(d => (d.PurchasePrice ?? 0) * (decimal?)(d.Quantity ?? 0)) ?? 0m
                })
                .ToListAsync(ct);

            var result = new PagedResult<PurchaseListDto>
            {
                Items = items,
                Page = page,
                PageSize = size,
                TotalCount = total
            };

            return ApiResult<PagedResult<PurchaseListDto>>.Ok(result, "Alış listesi", 200);
        }

        public async Task<ApiResult<PurchaseDetailDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var head =
                await (from p in _db.Purchases.AsNoTracking()
                       join b in _db.Branches.AsNoTracking() on p.BranchId equals b.Id into jb
                       from b in jb.DefaultIfEmpty()
                       join u in _db.Users.AsNoTracking() on p.UserId equals u.Id into ju
                       from u in ju.DefaultIfEmpty()
                       join c in _db.Customers.AsNoTracking() on p.CustomerId equals c.Id into jc
                       from c in jc.DefaultIfEmpty()
                       join pm in _db.PaymentMethods.AsNoTracking() on p.PaymentMethodId equals pm.Id into jpm
                       from pm in jpm.DefaultIfEmpty()
                       where p.Id == id
                       select new
                       {
                           p,
                           BranchName = b!.Name,
                           UserName = u!.Username,
                           CustomerName = c!.Name,
                           PaymentMethod = pm!.Name
                       }).FirstOrDefaultAsync(ct);

            if (head is null)
                return ApiResult<PurchaseDetailDto>.Fail("Alış bulunamadı", statusCode: 404);

            var lines =
                await (from d in _db.PurchaseDetails.AsNoTracking()
                       join s in _db.Stocks.AsNoTracking() on d.StockId equals s.Id
                       join pv in _db.ProductVariants.AsNoTracking() on s.ProductVariantId equals pv.Id into jpv
                       from pv in jpv.DefaultIfEmpty()
                       where d.PurchaseId == id
                       select new PurchaseDetailLineDto
                       {
                           Id = d.Id,
                           StockId = s.Id,
                           Barcode = s.Barcode,
                           Quantity = d.Quantity ?? 0,
                           PurchasePrice = d.PurchasePrice,
                           ProductVariantId = s.ProductVariantId,
                           VariantDisplay =
                               pv == null ? null :
                               $"{pv.Brand ?? ""} {pv.Ayar ?? ""} {s.Gram ?? 0:0.##}g"
                       }).ToListAsync(ct);

            var dto = new PurchaseDetailDto
            {
                Id = head.p.Id,
                CreatedAt = head.p.CreatedAt,
                BranchId = head.p.BranchId,
                BranchName = head.BranchName,
                UserId = head.p.UserId,
                UserName = head.UserName,
                CustomerId = head.p.CustomerId,
                CustomerName = head.CustomerName,
                PaymentMethodId = head.p.PaymentMethodId,
                PaymentMethod = head.PaymentMethod,
                ItemCount = lines.Count,
                TotalAmount = lines.Sum(l => (l.PurchasePrice ?? 0) * l.Quantity),
                Lines = lines
            };

            return ApiResult<PurchaseDetailDto>.Ok(dto, "Detay", 200);
        }
    }
}
