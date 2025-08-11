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
    }
}
