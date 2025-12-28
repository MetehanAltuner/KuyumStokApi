using KuyumStokApi.Application.Common;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KuyumStokApi.API.Controllers
{
    /// <summary>Development ortamı için yardımcı endpoint'ler.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class DevController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public DevController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        /// <summary>
        /// Tüm transaction verilerini temizler (Stocks, Sales, Purchases, vb.).
        /// SADECE DEVELOPMENT ORTAMINDA ÇALIŞIR!
        /// </summary>
        [HttpDelete("cleanup-all-data")]
        [Authorize]
        public async Task<IActionResult> CleanupAllData(CancellationToken ct)
        {
            // Sadece Development ortamında çalış
            if (!_env.IsDevelopment())
            {
                return StatusCode(403, ApiResult<bool>.Fail(
                    "Bu endpoint sadece Development ortamında çalışır.", 
                    statusCode: 403));
            }

            try
            {
                // Transaction içinde silme işlemleri (foreign key constraint'ler için sıralı silme)
                using var transaction = await _db.Database.BeginTransactionAsync(ct);

                // 1. İlişkili detay tabloları (önce bunlar silinmeli - foreign key'ler nedeniyle)
                var saleDetailsCount = await _db.SaleDetails.IgnoreQueryFilters().ExecuteDeleteAsync(ct);
                var salePaymentsCount = await _db.SalePayments.IgnoreQueryFilters().ExecuteDeleteAsync(ct);
                var bankTransactionsCount = await _db.BankTransactions.IgnoreQueryFilters().ExecuteDeleteAsync(ct);
                var purchaseDetailsCount = await _db.PurchaseDetails.IgnoreQueryFilters().ExecuteDeleteAsync(ct);
                var productLifecyclesCount = await _db.ProductLifecycles.IgnoreQueryFilters().ExecuteDeleteAsync(ct);

                // 2. Ana transaction tabloları
                var salesCount = await _db.Sales.IgnoreQueryFilters().ExecuteDeleteAsync(ct);
                var purchasesCount = await _db.Purchases.IgnoreQueryFilters().ExecuteDeleteAsync(ct);

                // 3. Stoklar (en son - çünkü diğer tablolar buna referans veriyor)
                var stocksCount = await _db.Stocks.IgnoreQueryFilters().ExecuteDeleteAsync(ct);

                await transaction.CommitAsync(ct);

                var result = new
                {
                    SaleDetails = saleDetailsCount,
                    SalePayments = salePaymentsCount,
                    BankTransactions = bankTransactionsCount,
                    PurchaseDetails = purchaseDetailsCount,
                    ProductLifecycles = productLifecyclesCount,
                    Sales = salesCount,
                    Purchases = purchasesCount,
                    Stocks = stocksCount,
                    Total = saleDetailsCount + salePaymentsCount + bankTransactionsCount + 
                            purchaseDetailsCount + productLifecyclesCount + salesCount + 
                            purchasesCount + stocksCount
                };

                return Ok(ApiResult<object>.Ok(result, 
                    $"Tüm transaction verileri temizlendi. Toplam {result.Total} kayıt silindi.", 200));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<bool>.Fail(
                    $"Temizleme sırasında hata oluştu: {ex.Message}", 
                    statusCode: 500));
            }
        }
    }
}

