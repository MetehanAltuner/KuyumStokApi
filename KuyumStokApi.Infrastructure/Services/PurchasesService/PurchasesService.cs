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

namespace KuyumStokApi.Infrastructure.Services.PurchaseService
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
    }
}
