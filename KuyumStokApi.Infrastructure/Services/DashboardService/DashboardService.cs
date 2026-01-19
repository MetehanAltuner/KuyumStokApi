using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Dashboard;
using KuyumStokApi.Application.DTOs.Reports;
using KuyumStokApi.Application.Interfaces.Auth;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using KuyumStokApi.Application.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.DashboardService
{
    public sealed class DashboardService : IDashboardService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ICurrentUserContext _currentUser;
        private readonly IReportsService _reportsService;
        private readonly KuyumStokApi.Infrastructure.Services.AnomalyDetectionService.AnomalyDetectionService _anomalyDetectionService;
        private readonly KuyumStokApi.Infrastructure.Services.WorkloadEstimationService.WorkloadEstimationService _workloadEstimationService;
        private readonly IHubContext<DashboardHub>? _hubContext;
        private readonly ILogger<DashboardService> _logger;

        private static readonly string[] OwnerRoleHints = { "owner", "admin" };
        private static readonly string[] ManagerRoleHints = { "manager" };

        public DashboardService(
            IDbContextFactory<AppDbContext> dbFactory,
            ICurrentUserContext currentUser,
            IReportsService reportsService,
            KuyumStokApi.Infrastructure.Services.AnomalyDetectionService.AnomalyDetectionService anomalyDetectionService,
            KuyumStokApi.Infrastructure.Services.WorkloadEstimationService.WorkloadEstimationService workloadEstimationService,
            IHubContext<DashboardHub>? hubContext = null,
            ILogger<DashboardService> logger = null!)
        {
            _dbFactory = dbFactory;
            _currentUser = currentUser;
            _reportsService = reportsService;
            _anomalyDetectionService = anomalyDetectionService;
            _workloadEstimationService = workloadEstimationService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<ApiResult<LiveCountersDto>> GetLiveCountersAsync(CancellationToken ct = default)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var scope = await ResolveScopeAsync(db, ct);
                if (!scope.AccessibleBranchIds.Any())
                    return ApiResult<LiveCountersDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

                var now = DateTime.UtcNow;
                var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

                // Son satış zamanı
                var lastSale = await db.Sales.AsNoTracking()
                    .Where(s => s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value))
                    .OrderByDescending(s => s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue)
                    .Select(s => s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue)
                    .FirstOrDefaultAsync(ct);

                var minutesSinceLastSale = lastSale != DateTime.MinValue
                    ? (int)(now - lastSale).TotalMinutes
                    : 0;

                // Bugünkü işlem sayısı
                var todaySalesCount = await db.Sales.AsNoTracking()
                    .Where(s => s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value) &&
                                (s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue) >= todayStart)
                    .CountAsync(ct);

                var todayPurchasesCount = await db.Purchases.AsNoTracking()
                    .Where(p => p.BranchId != null && scope.AccessibleBranchIds.Contains(p.BranchId.Value) &&
                                (p.CreatedAt ?? p.UpdatedAt ?? DateTime.MinValue) >= todayStart)
                    .CountAsync(ct);

                var todayTransactionCount = todaySalesCount + todayPurchasesCount;

                // Stok senkronizasyon zamanı
                var lastStockSync = await db.Stocks.AsNoTracking()
                    .Where(s => s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value))
                    .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt ?? DateTime.MinValue)
                    .Select(s => s.UpdatedAt ?? s.CreatedAt ?? DateTime.MinValue)
                    .FirstOrDefaultAsync(ct);

                var lastStockSyncTime = lastStockSync != DateTime.MinValue ? (DateTime?)lastStockSync : null;
                var lastStockSyncFormatted = lastStockSyncTime.HasValue
                    ? $"{lastStockSyncTime.Value:HH:mm} itibarıyla senkronize"
                    : "Henüz senkronize edilmedi";

                var dto = new LiveCountersDto
                {
                    MinutesSinceLastSale = minutesSinceLastSale,
                    TodayTransactionCount = todayTransactionCount,
                    LastStockSyncTime = lastStockSyncTime,
                    LastStockSyncFormatted = lastStockSyncFormatted
                };

                // SignalR broadcast (opsiyonel)
                await BroadcastLiveCountersAsync(dto, ct);

                return ApiResult<LiveCountersDto>.Ok(dto, "Canlı sayaçlar hazırlandı", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Canlı sayaçlar hesaplanırken hata oluştu");
                return ApiResult<LiveCountersDto>.Fail("Canlı sayaçlar hesaplanırken bir hata oluştu.", statusCode: 500);
            }
        }

        public async Task<ApiResult<SalesTrendReportDto>> GetWeeklyTrendAsync(CancellationToken ct = default)
        {
            try
            {
                var now = DateTime.UtcNow;
                var weekStart = now.AddDays(-7);
                var range = new ReportDateRange(weekStart, now);

                var result = await _reportsService.GetSalesTrendAsync(ReportTrendGranularity.Weekly, range, ct);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Haftalık trend hesaplanırken hata oluştu");
                return ApiResult<SalesTrendReportDto>.Fail("Haftalık trend hesaplanırken bir hata oluştu.", statusCode: 500);
            }
        }

        public async Task<ApiResult<DailySummaryDto>> GetDailySummaryAsync(DateTime? date = null, CancellationToken ct = default)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var scope = await ResolveScopeAsync(db, ct);
                if (!scope.AccessibleBranchIds.Any())
                    return ApiResult<DailySummaryDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

                var targetDate = date ?? DateTime.UtcNow;
                var dayStart = new DateTime(targetDate.Year, targetDate.Month, targetDate.Day, 0, 0, 0, DateTimeKind.Utc);
                var dayEnd = dayStart.AddDays(1).AddTicks(-1);

                // Satış toplamı
                var totalSales = await (from d in db.SaleDetails.AsNoTracking()
                                       join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                                       where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                                       let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                       where created >= dayStart && created <= dayEnd
                                       select (d.Quantity ?? 0) * (d.SoldPrice ?? 0m))
                    .SumAsync(ct);

                // Alış maliyeti
                var totalCost = await (from d in db.PurchaseDetails.AsNoTracking()
                                      join p in db.Purchases.AsNoTracking() on d.PurchaseId equals p.Id
                                      where p.BranchId != null && scope.AccessibleBranchIds.Contains(p.BranchId.Value)
                                      let created = p.CreatedAt ?? p.UpdatedAt ?? DateTime.MinValue
                                      where created >= dayStart && created <= dayEnd
                                      select (d.Quantity ?? 0) * (d.PurchasePrice ?? 0m))
                    .SumAsync(ct);

                var totalProfit = totalSales - totalCost;
                var profitPercentage = totalSales > 0 ? (totalProfit / totalSales) * 100 : 0;

                // Decimal değerleri 2 ondalık basamağa yuvarla
                totalSales = Math.Round(totalSales, 2);
                totalCost = Math.Round(totalCost, 2);
                totalProfit = Math.Round(totalProfit, 2);
                profitPercentage = Math.Round(profitPercentage, 2);

                // En çok satan ürün
                var topSellingProduct = await (from d in db.SaleDetails.AsNoTracking()
                                              join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                                              where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                                              let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                              where created >= dayStart && created <= dayEnd
                                              join st in db.Stocks.AsNoTracking() on d.StockId equals st.Id into js
                                              from st in js.DefaultIfEmpty()
                                              join pv in db.ProductVariants.AsNoTracking() on st.ProductVariantId equals pv.Id into jpv
                                              from pv in jpv.DefaultIfEmpty()
                                              group d by new { ProductName = pv != null ? pv.Name : st != null ? st.Barcode : "Tanımsız" } into g
                                              orderby g.Sum(x => x.Quantity ?? 0) descending
                                              select g.Key.ProductName)
                    .FirstOrDefaultAsync(ct);

                // Kritik stok sayısı
                var criticalStockCount = await (from s in db.Stocks.AsNoTracking()
                                                where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                                                join l in db.Limits.AsNoTracking() on new { s.BranchId, s.ProductVariantId } equals new { BranchId = l.BranchId, ProductVariantId = l.ProductVariantId } into jl
                                                from l in jl.DefaultIfEmpty()
                                                where l != null && (s.Quantity ?? 0) < (l.MinThreshold ?? 0)
                                                select s.Id)
                    .CountAsync(ct);

                var statusMessage = profitPercentage >= 25
                    ? "Gün başarılı geçti! 🎉"
                    : profitPercentage >= 15
                        ? "Gün iyi geçti."
                        : "Gün normal seviyede.";

                var dto = new DailySummaryDto
                {
                    Date = targetDate,
                    TotalSales = totalSales,
                    TotalProfit = totalProfit,
                    ProfitPercentage = profitPercentage,
                    TopSellingProduct = topSellingProduct ?? "Veri yok",
                    CriticalStockCount = criticalStockCount,
                    StatusMessage = statusMessage
                };

                // SignalR broadcast (opsiyonel)
                await BroadcastDailySummaryAsync(dto, ct);

                return ApiResult<DailySummaryDto>.Ok(dto, "Gün sonu raporu hazırlandı", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gün sonu raporu hesaplanırken hata oluştu");
                return ApiResult<DailySummaryDto>.Fail("Gün sonu raporu hesaplanırken bir hata oluştu.", statusCode: 500);
            }
        }

        public async Task<ApiResult<List<AnomalyDto>>> GetAnomaliesAsync(CancellationToken ct = default)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var scope = await ResolveScopeAsync(db, ct);
                if (!scope.AccessibleBranchIds.Any())
                    return ApiResult<List<AnomalyDto>>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

                var anomalies = new List<AnomalyDto>();
                var now = DateTime.UtcNow;
                var thirtyDaysAgo = now.AddDays(-30);

                // Son 30 günlük günlük satış verileri
                var dailySales = await (from d in db.SaleDetails.AsNoTracking()
                                       join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                                       where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                                       let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                       where created >= thirtyDaysAgo && created <= now
                                       let day = new DateTime(created.Year, created.Month, created.Day, 0, 0, 0, DateTimeKind.Utc)
                                       group d by day into g
                                       select new
                                       {
                                           Day = g.Key,
                                           Revenue = g.Sum(x => (x.Quantity ?? 0) * (x.SoldPrice ?? 0m))
                                       })
                    .ToListAsync(ct);

                if (dailySales.Any())
                {
                    // Günlük satış değerlerini liste olarak hazırla
                    var salesValues = dailySales.Select(x => x.Revenue).ToList();

                    // Bugünkü satış
                    var todaySales = dailySales
                        .Where(x => x.Day.Date == now.Date)
                        .Sum(x => x.Revenue);

                    // AnomalyDetectionService kullanarak anomali tespiti
                    var salesAnomaly = _anomalyDetectionService.DetectSalesAnomaly(salesValues, todaySales);

                    if (salesAnomaly.IsAnomaly)
                    {
                        if (salesAnomaly.ZScore < -2)
                        {
                            var riskScore = (int)Math.Min(100, Math.Abs(salesAnomaly.ZScore) * 30);
                            anomalies.Add(new AnomalyDto
                            {
                                Type = "HighSalesDrop",
                                Description = salesAnomaly.Message,
                                RiskScore = riskScore
                            });
                        }
                        else if (salesAnomaly.ZScore > 2)
                        {
                            anomalies.Add(new AnomalyDto
                            {
                                Type = "HighSalesIncrease",
                                Description = salesAnomaly.Message,
                                RiskScore = 20
                            });
                        }
                    }
                    else
                    {
                        anomalies.Add(new AnomalyDto
                        {
                            Type = "NormalSales",
                            Description = salesAnomaly.Message,
                            RiskScore = 30
                        });
                    }
                }

                // Stok anomali tespiti
                var criticalStocks = await (from s in db.Stocks.AsNoTracking()
                                           where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                                           join l in db.Limits.AsNoTracking() on new { s.BranchId, s.ProductVariantId } equals new { BranchId = l.BranchId, ProductVariantId = l.ProductVariantId } into jl
                                           from l in jl.DefaultIfEmpty()
                                           where l != null && (s.Quantity ?? 0) < (l.MinThreshold ?? 0)
                                           select new { s.Quantity, l.MinThreshold })
                    .ToListAsync(ct);

                if (criticalStocks.Any())
                {
                    // Her kritik stok için AnomalyDetectionService kullan
                    var stockAnomalies = criticalStocks
                        .Select(cs => _anomalyDetectionService.DetectStockAnomaly(cs.Quantity ?? 0, (int)(cs.MinThreshold ?? 0)))
                        .ToList();

                    var avgRiskScore = stockAnomalies
                        .Where(a => a.IsAnomaly)
                        .Select(a => (int)Math.Min(100, Math.Abs(a.ZScore) * 50))
                        .DefaultIfEmpty(0)
                        .Average();

                    anomalies.Add(new AnomalyDto
                    {
                        Type = "LowStockLevel",
                        Description = $"Düşük stok seviyesi ({criticalStocks.Count} ürün)",
                        RiskScore = (int)avgRiskScore
                    });
                }

                if (!anomalies.Any())
                {
                    anomalies.Add(new AnomalyDto
                    {
                        Type = "NormalSales",
                        Description = "Normal satış",
                        RiskScore = 30
                    });
                }

                // SignalR broadcast (opsiyonel)
                await BroadcastAnomaliesAsync(anomalies, ct);

                return ApiResult<List<AnomalyDto>>.Ok(anomalies, "Anomali analizi tamamlandı", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Anomali analizi yapılırken hata oluştu");
                return ApiResult<List<AnomalyDto>>.Fail("Anomali analizi yapılırken bir hata oluştu.", statusCode: 500);
            }
        }

        public async Task<ApiResult<MonthlyTargetDto>> GetMonthlyTargetAsync(CancellationToken ct = default)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var scope = await ResolveScopeAsync(db, ct);
                if (!scope.AccessibleBranchIds.Any())
                    return ApiResult<MonthlyTargetDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

                var now = DateTime.UtcNow;
                var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

                // Mevcut ay satış toplamı
                var currentAmount = await (from d in db.SaleDetails.AsNoTracking()
                                          join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                                          where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                                          let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                          where created >= monthStart && created <= monthEnd
                                          select (d.Quantity ?? 0) * (d.SoldPrice ?? 0m))
                    .SumAsync(ct);

                // Hedef tutar - veritabanından oku (mağaza bazlı)
                decimal targetAmount = 100000m; // Default değer
                if (scope.StoreId.HasValue)
                {
                    var monthlyTarget = await db.MonthlyTargets.AsNoTracking()
                        .Where(mt => mt.StoreId == scope.StoreId.Value 
                                  && mt.Year == now.Year 
                                  && mt.Month == now.Month
                                  && !mt.IsDeleted)
                        .FirstOrDefaultAsync(ct);
                    
                    if (monthlyTarget != null)
                    {
                        targetAmount = monthlyTarget.TargetAmount;
                    }
                }
                var progressPercentage = targetAmount > 0 ? (currentAmount / targetAmount) * 100 : 0;
                var remainingAmount = Math.Max(0, targetAmount - currentAmount);

                // Decimal değerleri 2 ondalık basamağa yuvarla
                currentAmount = Math.Round(currentAmount, 2);
                targetAmount = Math.Round(targetAmount, 2);
                progressPercentage = Math.Round(progressPercentage, 2);
                remainingAmount = Math.Round(remainingAmount, 2);

                var statusMessage = progressPercentage >= 75
                    ? $"Harika gidiyorsun! Sadece ₺{remainingAmount:N0} kaldı! 🚀"
                    : progressPercentage >= 50
                        ? $"İyi gidiyorsun! ₺{remainingAmount:N0} daha hedefe ulaşmak için kaldı."
                        : $"Hedefe ulaşmak için ₺{remainingAmount:N0} daha gerekiyor.";

                var dto = new MonthlyTargetDto
                {
                    TargetAmount = targetAmount,
                    CurrentAmount = currentAmount,
                    ProgressPercentage = progressPercentage,
                    RemainingAmount = remainingAmount,
                    StatusMessage = statusMessage
                };

                return ApiResult<MonthlyTargetDto>.Ok(dto, "Aylık hedef raporu hazırlandı", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Aylık hedef raporu hesaplanırken hata oluştu");
                return ApiResult<MonthlyTargetDto>.Fail("Aylık hedef raporu hesaplanırken bir hata oluştu.", statusCode: 500);
            }
        }

        public async Task<ApiResult<List<ReminderDto>>> GetRemindersAsync(CancellationToken ct = default)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var scope = await ResolveScopeAsync(db, ct);
                if (!scope.AccessibleBranchIds.Any())
                    return ApiResult<List<ReminderDto>>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

                var reminders = new List<ReminderDto>();
                var now = DateTime.UtcNow;
                var thirtyDaysAgo = now.AddDays(-30);
                var sevenDaysAgo = now.AddDays(-7);

                // Kritik stok kontrolü
                var criticalStocks = await (from s in db.Stocks.AsNoTracking()
                                           where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                                           join pv in db.ProductVariants.AsNoTracking() on s.ProductVariantId equals pv.Id
                                           join l in db.Limits.AsNoTracking() on new { s.BranchId, s.ProductVariantId } equals new { BranchId = l.BranchId, ProductVariantId = l.ProductVariantId } into jl
                                           from l in jl.DefaultIfEmpty()
                                           where l != null && (s.Quantity ?? 0) < (l.MinThreshold ?? 0)
                                           select new { pv.Name, s.Quantity, s.ProductVariantId, l.MinThreshold })
                    .ToListAsync(ct);

                foreach (var cs in criticalStocks)
                {
                    // Son 7 günlük ortalama günlük satış hızı
                    var dailySales = await (from d in db.SaleDetails.AsNoTracking()
                                           join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                                           where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                                           let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                           where created >= sevenDaysAgo && created <= now
                                           join st in db.Stocks.AsNoTracking() on d.StockId equals st.Id
                                           where st.ProductVariantId == cs.ProductVariantId
                                           select d.Quantity ?? 0)
                        .SumAsync(ct);

                    var avgDailySales = dailySales / 7.0;
                    
                    if (avgDailySales > 0)
                    {
                        var daysUntilDepletion = (int)Math.Ceiling((cs.Quantity ?? 0) / avgDailySales);
                        reminders.Add(new ReminderDto
                        {
                            Type = "CriticalStock",
                            Message = $"{cs.Name} {daysUntilDepletion} gün içinde bitebilir.",
                            Priority = daysUntilDepletion <= 3 ? 5 : daysUntilDepletion <= 7 ? 4 : 3
                        });
                    }
                    else
                    {
                        reminders.Add(new ReminderDto
                        {
                            Type = "CriticalStock",
                            Message = $"{cs.Name} için satış verisi yetersiz, stok seviyesini manuel kontrol edin.",
                            Priority = 3
                        });
                    }
                }

                // Uzun süre satılmayan ürünler
                var unsoldProducts = await (from pv in db.ProductVariants.AsNoTracking()
                                           where !pv.IsDeleted
                                           let lastSale = (from d in db.SaleDetails.AsNoTracking()
                                                          join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                                                          join st in db.Stocks.AsNoTracking() on d.StockId equals st.Id
                                                          where st.ProductVariantId == pv.Id
                                                          let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                                          orderby created descending
                                                          select (DateTime?)created)
                                               .FirstOrDefault()
                                           where !lastSale.HasValue || lastSale.Value < thirtyDaysAgo
                                           select pv.Name)
                    .Take(5)
                    .ToListAsync(ct);

                if (unsoldProducts.Any())
                {
                    reminders.Add(new ReminderDto
                    {
                        Type = "UnsoldProduct",
                        Message = $"{unsoldProducts.Count} ürün 30 gündür satılmadı. Kampanya önerilir.",
                        Priority = 2
                    });
                }

                // Kritik stok sayısı
                if (criticalStocks.Any())
                {
                    reminders.Add(new ReminderDto
                    {
                        Type = "StockDepletion",
                        Message = $"Kritik stok: {criticalStocks.Count} ürün.",
                        Priority = 4
                    });
                }

                return ApiResult<List<ReminderDto>>.Ok(reminders, "Hatırlatıcılar hazırlandı", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hatırlatıcılar hesaplanırken hata oluştu");
                return ApiResult<List<ReminderDto>>.Fail("Hatırlatıcılar hesaplanırken bir hata oluştu.", statusCode: 500);
            }
        }

        public async Task<ApiResult<List<TopProductDto>>> GetTopProductsAsync(int limit = 5, string period = "week", CancellationToken ct = default)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var scope = await ResolveScopeAsync(db, ct);
                if (!scope.AccessibleBranchIds.Any())
                    return ApiResult<List<TopProductDto>>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

                var now = DateTime.UtcNow;
                DateTime periodStart;

                switch (period.ToLowerInvariant())
                {
                    case "month":
                        periodStart = now.AddDays(-30);
                        break;
                    case "all":
                        periodStart = DateTime.MinValue;
                        break;
                    default: // week
                        periodStart = now.AddDays(-7);
                        break;
                }

                var topProducts = await (from d in db.SaleDetails.AsNoTracking()
                                        join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                                        where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                                        let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                        where created >= periodStart && created <= now
                                        join st in db.Stocks.AsNoTracking() on d.StockId equals st.Id into js
                                        from st in js.DefaultIfEmpty()
                                        join pv in db.ProductVariants.AsNoTracking() on st.ProductVariantId equals pv.Id into jpv
                                        from pv in jpv.DefaultIfEmpty()
                                        group d by new
                                        {
                                            ProductName = pv != null ? pv.Name : st != null ? st.Barcode : "Tanımsız"
                                        } into g
                                        orderby g.Sum(x => x.Quantity ?? 0) descending
                                        select new TopProductDto
                                        {
                                            ProductName = g.Key.ProductName,
                                            QuantitySold = g.Sum(x => x.Quantity ?? 0),
                                            Revenue = Math.Round(g.Sum(x => (x.Quantity ?? 0) * (x.SoldPrice ?? 0m)), 2)
                                        })
                    .Take(limit)
                    .ToListAsync(ct);

                return ApiResult<List<TopProductDto>>.Ok(topProducts, "En çok satan ürünler hazırlandı", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "En çok satan ürünler hesaplanırken hata oluştu");
                return ApiResult<List<TopProductDto>>.Fail("En çok satan ürünler hesaplanırken bir hata oluştu.", statusCode: 500);
            }
        }

        public async Task<ApiResult<WorkloadEstimateDto>> GetWorkloadEstimateAsync(CancellationToken ct = default)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var scope = await ResolveScopeAsync(db, ct);
                if (!scope.AccessibleBranchIds.Any())
                    return ApiResult<WorkloadEstimateDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

                var now = DateTime.UtcNow;
                var thirtyDaysAgo = now.AddDays(-30);

                // Son 30 günlük günlük işlem sayıları
                var dailyTransactions = await (from s in db.Sales.AsNoTracking()
                                             where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                                             let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                             where created >= thirtyDaysAgo && created <= now
                                             let day = new DateTime(created.Year, created.Month, created.Day, 0, 0, 0, DateTimeKind.Utc)
                                             group s by day into g
                                             select new { Day = g.Key, Count = g.Count() })
                    .ToListAsync(ct);

                var dailyPurchases = await (from p in db.Purchases.AsNoTracking()
                                           where p.BranchId != null && scope.AccessibleBranchIds.Contains(p.BranchId.Value)
                                           let created = p.CreatedAt ?? p.UpdatedAt ?? DateTime.MinValue
                                           where created >= thirtyDaysAgo && created <= now
                                           let day = new DateTime(created.Year, created.Month, created.Day, 0, 0, 0, DateTimeKind.Utc)
                                           group p by day into g
                                           select new { Day = g.Key, Count = g.Count() })
                    .ToListAsync(ct);

                var combined = dailyTransactions
                    .Concat(dailyPurchases)
                    .GroupBy(x => x.Day)
                    .Select(g => new { Day = g.Key, Count = g.Sum(x => x.Count) })
                    .ToList();

                if (!combined.Any())
                {
                    return ApiResult<WorkloadEstimateDto>.Ok(new WorkloadEstimateDto
                    {
                        EstimatedWorkloadPercentage = 0,
                        IntensityLevel = "Düşük",
                        EstimatedTransactionCount = 0,
                        Message = "Yeterli veri yok"
                    }, "İş yükü tahmini hazırlandı", 200);
                }

                // Günlük işlem sayılarını sıralı liste olarak hazırla (en eski -> en yeni)
                var dailyCounts = combined
                    .OrderBy(x => x.Day)
                    .Select(x => x.Count)
                    .ToList();

                // WorkloadEstimationService ile tahmin yap
                // Önce lineer regresyon dene, yeterli veri yoksa moving average kullan
                int estimatedCount;
                if (dailyCounts.Count >= 7)
                {
                    estimatedCount = _workloadEstimationService.EstimateWithLinearRegression(dailyCounts);
                }
                else
                {
                    estimatedCount = _workloadEstimationService.EstimateWithMovingAverage(dailyCounts, dailyCounts.Count);
                }

                var overallAvg = combined.Select(x => x.Count).Average();
                var workloadPercentage = _workloadEstimationService.CalculateWorkloadPercentage(estimatedCount, overallAvg);
                var intensityLevel = _workloadEstimationService.DetermineIntensityLevel(estimatedCount, workloadPercentage);
                var message = _workloadEstimationService.GenerateMessage(intensityLevel, estimatedCount);

                var dto = new WorkloadEstimateDto
                {
                    EstimatedWorkloadPercentage = workloadPercentage,
                    IntensityLevel = intensityLevel,
                    EstimatedTransactionCount = estimatedCount,
                    Message = message
                };

                return ApiResult<WorkloadEstimateDto>.Ok(dto, "İş yükü tahmini hazırlandı", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İş yükü tahmini hesaplanırken hata oluştu");
                return ApiResult<WorkloadEstimateDto>.Fail("İş yükü tahmini hesaplanırken bir hata oluştu.", statusCode: 500);
            }
        }

        public async Task<ApiResult<BranchComparisonDto>> GetBranchComparisonAsync(CancellationToken ct = default)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var scope = await ResolveScopeAsync(db, ct);
                if (!scope.AccessibleBranchIds.Any())
                    return ApiResult<BranchComparisonDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

                var now = DateTime.UtcNow;
                var last7Days = now.AddDays(-7);
                var previous7Days = last7Days.AddDays(-7);

                var branches = new List<BranchComparisonItemDto>();

                foreach (var branchId in scope.AccessibleBranchIds)
                {
                    var branch = await db.Branches.AsNoTracking()
                        .Where(b => b.Id == branchId)
                        .Select(b => b.Name)
                        .FirstOrDefaultAsync(ct);

                    if (string.IsNullOrEmpty(branch)) continue;

                    // Satış toplamı (son 7 gün)
                    var totalSales = await (from d in db.SaleDetails.AsNoTracking()
                                           join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                                           where s.BranchId == branchId
                                           let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                           where created >= last7Days && created <= now
                                           select (d.Quantity ?? 0) * (d.SoldPrice ?? 0m))
                        .SumAsync(ct);

                    // Alış maliyeti (son 7 gün)
                    var totalCost = await (from d in db.PurchaseDetails.AsNoTracking()
                                          join p in db.Purchases.AsNoTracking() on d.PurchaseId equals p.Id
                                          where p.BranchId == branchId
                                          let created = p.CreatedAt ?? p.UpdatedAt ?? DateTime.MinValue
                                          where created >= last7Days && created <= now
                                          select (d.Quantity ?? 0) * (d.PurchasePrice ?? 0m))
                        .SumAsync(ct);

                    var totalProfit = totalSales - totalCost;

                    // Fiş sayısı
                    var receiptCount = await db.Sales.AsNoTracking()
                        .Where(s => s.BranchId == branchId)
                        .CountAsync(ct);

                    // POS oranı
                    var totalPayments = await (from sp in db.SalePayments.AsNoTracking()
                                              join s in db.Sales.AsNoTracking() on sp.SaleId equals s.Id
                                              where s.BranchId == branchId
                                              select sp.Amount)
                        .SumAsync(ct);

                    var posPayments = await (from sp in db.SalePayments.AsNoTracking()
                                            join s in db.Sales.AsNoTracking() on sp.SaleId equals s.Id
                                            join pm in db.PaymentMethods.AsNoTracking() on sp.PaymentMethodId equals pm.Id
                                            where s.BranchId == branchId && pm.Name != null && (pm.Name.Contains("Kart") || pm.Name.Contains("POS") || pm.Name.Contains("Kredi"))
                                            select sp.Amount)
                        .SumAsync(ct);

                    var posPercentage = totalPayments > 0 ? (posPayments / totalPayments) * 100 : 0;

                    // Decimal değerleri 2 ondalık basamağa yuvarla
                    totalSales = Math.Round(totalSales, 2);
                    totalCost = Math.Round(totalCost, 2);
                    totalProfit = Math.Round(totalProfit, 2);
                    posPercentage = Math.Round(posPercentage, 2);

                    // Kritik stok sayısı
                    var criticalStockCount = await (from s in db.Stocks.AsNoTracking()
                                                   where s.BranchId == branchId
                                                   join l in db.Limits.AsNoTracking() on new { s.BranchId, s.ProductVariantId } equals new { BranchId = l.BranchId, ProductVariantId = l.ProductVariantId } into jl
                                                   from l in jl.DefaultIfEmpty()
                                                   where l != null && (s.Quantity ?? 0) < (l.MinThreshold ?? 0)
                                                   select s.Id)
                        .CountAsync(ct);

                    // Trend (son 7 gün vs önceki 7 gün)
                    var last7DaysSales = await (from d in db.SaleDetails.AsNoTracking()
                                               join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                                               where s.BranchId == branchId
                                               let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                               where created >= last7Days && created <= now
                                               select (d.Quantity ?? 0) * (d.SoldPrice ?? 0m))
                        .SumAsync(ct);

                    var previous7DaysSales = await (from d in db.SaleDetails.AsNoTracking()
                                                   join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                                                   where s.BranchId == branchId
                                                   let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                                   where created >= previous7Days && created < last7Days
                                                   select (d.Quantity ?? 0) * (d.SoldPrice ?? 0m))
                        .SumAsync(ct);

                    string trend;
                    if (previous7DaysSales == 0)
                    {
                        trend = last7DaysSales > 0 ? "up" : "stable";
                    }
                    else
                    {
                        var change = ((last7DaysSales - previous7DaysSales) / previous7DaysSales) * 100;
                        trend = change > 5 ? "up" : change < -5 ? "down" : "stable";
                    }

                    branches.Add(new BranchComparisonItemDto
                    {
                        BranchName = branch,
                        TotalSales = totalSales,
                        TotalProfit = totalProfit,
                        ReceiptCount = receiptCount,
                        PosPercentage = posPercentage,
                        CriticalStockCount = criticalStockCount,
                        Trend = trend
                    });
                }

                // Satış toplamına göre sırala
                branches = branches.OrderByDescending(b => b.TotalSales).ToList();

                var dto = new BranchComparisonDto
                {
                    Branches = branches
                };

                return ApiResult<BranchComparisonDto>.Ok(dto, "Şube karşılaştırması hazırlandı", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şube karşılaştırması hesaplanırken hata oluştu");
                return ApiResult<BranchComparisonDto>.Fail("Şube karşılaştırması hesaplanırken bir hata oluştu.", statusCode: 500);
            }
        }

        public async Task<ApiResult<ProfitLossDto>> GetProfitLossAsync(string period = "week", CancellationToken ct = default)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var scope = await ResolveScopeAsync(db, ct);
                if (!scope.AccessibleBranchIds.Any())
                    return ApiResult<ProfitLossDto>.Fail("Görüntüleyebileceğiniz şube bulunamadı.", statusCode: 403);

                var now = DateTime.UtcNow;
                DateTime periodStart;
                Func<DateTime, DateTime> periodKeySelector;
                Func<DateTime, string> periodLabelSelector;

                switch (period.ToLowerInvariant())
                {
                    case "day":
                        periodStart = now.AddDays(-7); // Son 7 gün
                        periodKeySelector = d => new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
                        periodLabelSelector = d => d.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
                        break;
                    case "month":
                        periodStart = now.AddMonths(-6).Date; // Son 6 ay
                        periodKeySelector = d => new DateTime(d.Year, d.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                        periodLabelSelector = d => d.ToString("MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
                        break;
                    default: // week
                        periodStart = now.AddDays(-(7 * 4)); // Son 4 hafta
                        periodKeySelector = d =>
                        {
                            var dayOfWeek = (int)d.DayOfWeek;
                            var diff = (7 + (dayOfWeek - (int)DayOfWeek.Monday)) % 7;
                            var weekStart = d.AddDays(-diff);
                            return new DateTime(weekStart.Year, weekStart.Month, weekStart.Day, 0, 0, 0, DateTimeKind.Utc);
                        };
                        periodLabelSelector = d => $"Hafta {GetWeekOfYear(d)} ({d:dd MMM})";
                        break;
                }

                // Satış verileri - önce memory'ye çek, sonra grupla
                var salesRaw = await (from d in db.SaleDetails.AsNoTracking()
                                     join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                                     where s.BranchId != null && scope.AccessibleBranchIds.Contains(s.BranchId.Value)
                                     let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                     where created >= periodStart && created <= now
                                     select new
                                     {
                                         Created = created,
                                         Quantity = d.Quantity ?? 0,
                                         SoldPrice = d.SoldPrice ?? 0m
                                     })
                    .ToListAsync(ct);

                var salesData = salesRaw
                    .GroupBy(x => periodKeySelector(x.Created))
                    .Select(g => new
                    {
                        Period = g.Key,
                        Sales = g.Sum(x => x.Quantity * x.SoldPrice)
                    })
                    .ToList();

                // Alış maliyeti verileri - önce memory'ye çek, sonra grupla
                var costRaw = await (from d in db.PurchaseDetails.AsNoTracking()
                                     join p in db.Purchases.AsNoTracking() on d.PurchaseId equals p.Id
                                     where p.BranchId != null && scope.AccessibleBranchIds.Contains(p.BranchId.Value)
                                     let created = p.CreatedAt ?? p.UpdatedAt ?? DateTime.MinValue
                                     where created >= periodStart && created <= now
                                     select new
                                     {
                                         Created = created,
                                         Quantity = d.Quantity ?? 0,
                                         PurchasePrice = d.PurchasePrice ?? 0m
                                     })
                    .ToListAsync(ct);

                var costData = costRaw
                    .GroupBy(x => periodKeySelector(x.Created))
                    .Select(g => new
                    {
                        Period = g.Key,
                        Cost = g.Sum(x => x.Quantity * x.PurchasePrice)
                    })
                    .ToList();

                // Verileri birleştir
                var items = new List<ProfitLossItemDto>();
                var salesDict = salesData.ToDictionary(x => x.Period, x => x.Sales);
                var costDict = costData.ToDictionary(x => x.Period, x => x.Cost);

                var allPeriods = salesDict.Keys.Union(costDict.Keys).OrderBy(p => p).ToList();

                decimal? previousProfit = null;
                foreach (var periodKey in allPeriods)
                {
                    var sales = salesDict.GetValueOrDefault(periodKey, 0);
                    var cost = costDict.GetValueOrDefault(periodKey, 0);
                    var profit = sales - cost;
                    var profitPercentage = sales > 0 ? (profit / sales) * 100 : 0;

                    // Decimal değerleri 2 ondalık basamağa yuvarla
                    sales = Math.Round(sales, 2);
                    cost = Math.Round(cost, 2);
                    profit = Math.Round(profit, 2);
                    profitPercentage = Math.Round(profitPercentage, 2);

                    string trend = "stable";
                    if (previousProfit.HasValue)
                    {
                        if (previousProfit.Value > 0)
                        {
                            var change = ((profit - previousProfit.Value) / previousProfit.Value) * 100;
                            trend = change > 5 ? "up" : change < -5 ? "down" : "stable";
                        }
                        else
                        {
                            trend = profit > previousProfit.Value ? "up" : profit < previousProfit.Value ? "down" : "stable";
                        }
                    }

                    items.Add(new ProfitLossItemDto
                    {
                        Period = periodKey,
                        PeriodLabel = periodLabelSelector(periodKey),
                        Sales = sales,
                        Cost = cost,
                        Profit = profit,
                        ProfitPercentage = profitPercentage,
                        Trend = trend
                    });

                    previousProfit = profit;
                }

                var totalSales = items.Sum(x => x.Sales);
                var totalCost = items.Sum(x => x.Cost);
                var totalProfit = totalSales - totalCost;
                var totalProfitPercentage = totalSales > 0 ? (totalProfit / totalSales) * 100 : 0;

                // Decimal değerleri 2 ondalık basamağa yuvarla
                totalSales = Math.Round(totalSales, 2);
                totalCost = Math.Round(totalCost, 2);
                totalProfit = Math.Round(totalProfit, 2);
                totalProfitPercentage = Math.Round(totalProfitPercentage, 2);

                var dto = new ProfitLossDto
                {
                    Items = items.OrderBy(x => x.Period).ToList(),
                    TotalSales = totalSales,
                    TotalCost = totalCost,
                    TotalProfit = totalProfit,
                    TotalProfitPercentage = totalProfitPercentage
                };

                return ApiResult<ProfitLossDto>.Ok(dto, "Kar-Zarar tablosu hazırlandı", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kar-Zarar tablosu hesaplanırken hata oluştu");
                return ApiResult<ProfitLossDto>.Fail("Kar-Zarar tablosu hesaplanırken bir hata oluştu.", statusCode: 500);
            }
        }

        private static int GetWeekOfYear(DateTime date)
        {
            var culture = new System.Globalization.CultureInfo("tr-TR");
            var calendar = culture.Calendar;
            return calendar.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        #region Helpers

        /// <summary>
        /// Kullanıcı bilgisi olmadan tüm şubeler için scope oluşturur (background service için)
        /// </summary>
        private async Task<ReportScope> ResolveScopeForBackgroundServiceAsync(AppDbContext db, CancellationToken ct)
        {
            // Background service için tüm aktif şubeleri al (owner gibi davran)
            var branchIds = await db.Branches.AsNoTracking()
                .Where(b => !b.IsDeleted)
                .Select(b => b.Id)
                .ToListAsync(ct);

            return new ReportScope
            {
                UserId = 0, // Background service için kullanıcı yok
                BranchId = null,
                StoreId = null,
                RoleName = "system", // System olarak işaretle
                AccessibleBranchIds = branchIds
            };
        }

        private async Task<ReportScope> ResolveScopeAsync(AppDbContext db, CancellationToken ct)
        {
            if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
                throw new InvalidOperationException("Kullanıcı kimliği doğrulanamadı.");

            var user = await db.Users.AsNoTracking()
                .Include(u => u.Branch)
                .ThenInclude(b => b.Store)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, ct);

            if (user == null)
                throw new InvalidOperationException("Kullanıcı kaydı bulunamadı.");

            var roleName = user.Role?.Name?.ToLowerInvariant();
            var branchIds = new List<int>();

            if (IsOwnerRole(roleName))
            {
                if (user.Branch?.StoreId != null)
                {
                    branchIds = await db.Branches.AsNoTracking()
                        .Where(b => b.StoreId == user.Branch.StoreId && !b.IsDeleted)
                        .Select(b => b.Id)
                        .ToListAsync(ct);
                }
                else
                {
                    branchIds = await db.Branches.AsNoTracking()
                        .Where(b => !b.IsDeleted)
                        .Select(b => b.Id)
                        .ToListAsync(ct);
                }
            }
            else if (IsManagerRole(roleName) && user.BranchId != null)
            {
                branchIds.Add(user.BranchId.Value);
            }
            else if (user.BranchId != null)
            {
                branchIds.Add(user.BranchId.Value);
            }

            return new ReportScope
            {
                UserId = user.Id,
                BranchId = user.BranchId,
                StoreId = user.Branch?.StoreId,
                RoleName = roleName,
                AccessibleBranchIds = branchIds.Distinct().ToList()
            };
        }

        private static bool IsOwnerRole(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return false;
            return OwnerRoleHints.Any(h => roleName.Contains(h, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsManagerRole(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return false;
            return ManagerRoleHints.Any(h => roleName.Contains(h, StringComparison.OrdinalIgnoreCase));
        }

        private sealed class ReportScope
        {
            public int UserId { get; set; }
            public int? BranchId { get; set; }
            public int? StoreId { get; set; }
            public string? RoleName { get; set; }
            public List<int> AccessibleBranchIds { get; set; } = new();
        }

        #region SignalR Broadcast Methods

        /// <summary>
        /// Live counters verilerini SignalR hub üzerinden broadcast eder
        /// </summary>
        private async Task BroadcastLiveCountersAsync(LiveCountersDto dto, CancellationToken ct)
        {
            if (_hubContext == null) return;

            try
            {
                await _hubContext.Clients.All.SendAsync("LiveCountersUpdated", dto, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Live counters broadcast edilirken hata oluştu");
            }
        }

        /// <summary>
        /// Daily summary verilerini SignalR hub üzerinden broadcast eder
        /// </summary>
        private async Task BroadcastDailySummaryAsync(DailySummaryDto dto, CancellationToken ct)
        {
            if (_hubContext == null) return;

            try
            {
                await _hubContext.Clients.All.SendAsync("DailySummaryUpdated", dto, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Daily summary broadcast edilirken hata oluştu");
            }
        }

        /// <summary>
        /// Anomaly verilerini SignalR hub üzerinden broadcast eder
        /// </summary>
        private async Task BroadcastAnomaliesAsync(List<AnomalyDto> anomalies, CancellationToken ct)
        {
            if (_hubContext == null) return;

            try
            {
                await _hubContext.Clients.All.SendAsync("AnomaliesUpdated", anomalies, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Anomalies broadcast edilirken hata oluştu");
            }
        }

        #endregion

        public async Task<ApiResult<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default)
        {
            try
            {
                var summary = new DashboardSummaryDto();

                // Sadece parametresiz ve broadcast yapılmayan metodları paralel olarak çağır
                var weeklyTrendTask = GetWeeklyTrendAsync(ct);
                var monthlyTargetTask = GetMonthlyTargetAsync(ct);
                var remindersTask = GetRemindersAsync(ct);
                var workloadEstimateTask = GetWorkloadEstimateAsync(ct);
                var branchComparisonTask = GetBranchComparisonAsync(ct);
                var riskScoreLegendTask = GetRiskScoreLegendAsync(ct);

                // Her task'ı bağımsız olarak işle (bir hata diğerlerini etkilemez)
                // Weekly Trend
                try
                {
                    var weeklyTrendResult = await weeklyTrendTask;
                    if (weeklyTrendResult.Success && weeklyTrendResult.Data != null)
                    {
                        summary.WeeklyTrend = weeklyTrendResult.Data;
                    }
                    else
                    {
                        _logger.LogWarning("WeeklyTrend başarısız: {Message}", weeklyTrendResult.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WeeklyTrend alınırken hata oluştu");
                }

                // Monthly Target
                try
                {
                    var monthlyTargetResult = await monthlyTargetTask;
                    if (monthlyTargetResult.Success && monthlyTargetResult.Data != null)
                    {
                        summary.MonthlyTarget = monthlyTargetResult.Data;
                    }
                    else
                    {
                        _logger.LogWarning("MonthlyTarget başarısız: {Message}", monthlyTargetResult.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MonthlyTarget alınırken hata oluştu");
                }

                // Reminders
                try
                {
                    var remindersResult = await remindersTask;
                    if (remindersResult.Success && remindersResult.Data != null)
                    {
                        summary.Reminders = remindersResult.Data;
                    }
                    else
                    {
                        _logger.LogWarning("Reminders başarısız: {Message}", remindersResult.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Reminders alınırken hata oluştu");
                }

                // Workload Estimate
                try
                {
                    var workloadEstimateResult = await workloadEstimateTask;
                    if (workloadEstimateResult.Success && workloadEstimateResult.Data != null)
                    {
                        summary.WorkloadEstimate = workloadEstimateResult.Data;
                    }
                    else
                    {
                        _logger.LogWarning("WorkloadEstimate başarısız: {Message}", workloadEstimateResult.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WorkloadEstimate alınırken hata oluştu");
                }

                // Branch Comparison
                try
                {
                    var branchComparisonResult = await branchComparisonTask;
                    if (branchComparisonResult.Success && branchComparisonResult.Data != null)
                    {
                        summary.BranchComparison = branchComparisonResult.Data;
                    }
                    else
                    {
                        _logger.LogWarning("BranchComparison başarısız: {Message}", branchComparisonResult.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "BranchComparison alınırken hata oluştu");
                }

                // Risk Score Legend
                try
                {
                    var riskScoreLegendResult = await riskScoreLegendTask;
                    if (riskScoreLegendResult.Success && riskScoreLegendResult.Data != null)
                    {
                        summary.RiskScoreLegend = riskScoreLegendResult.Data;
                    }
                    else
                    {
                        _logger.LogWarning("RiskScoreLegend başarısız: {Message}", riskScoreLegendResult.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RiskScoreLegend alınırken hata oluştu");
                }

                // Summary endpoint'i broadcast yapmaz, sadece tek seferlik veri döndürür
                // Broadcast yapan endpoint'ler: live-counters, daily-summary, anomalies

                return ApiResult<DashboardSummaryDto>.Ok(summary, "Dashboard özeti hazırlandı", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard özeti hazırlanırken beklenmeyen hata oluştu");
                return ApiResult<DashboardSummaryDto>.Fail("Dashboard özeti hazırlanırken bir hata oluştu.", statusCode: 500);
            }
        }

        public async Task<ApiResult<RiskScoreLegendDto>> GetRiskScoreLegendAsync(CancellationToken ct = default)
        {
            try
            {
                var ranges = new List<RiskScoreRangeDto>
                {
                    new RiskScoreRangeDto
                    {
                        Min = 0,
                        Max = 20,
                        Level = "Düşük",
                        Description = "Düşük risk seviyesi. Normal işleyiş.",
                        Color = "#10b981" // Yeşil
                    },
                    new RiskScoreRangeDto
                    {
                        Min = 21,
                        Max = 50,
                        Level = "Orta",
                        Description = "Orta risk seviyesi. Dikkatli takip edilmeli.",
                        Color = "#f59e0b" // Sarı
                    },
                    new RiskScoreRangeDto
                    {
                        Min = 51,
                        Max = 80,
                        Level = "Yüksek",
                        Description = "Yüksek risk seviyesi. Acil müdahale gerekebilir.",
                        Color = "#f97316" // Turuncu
                    },
                    new RiskScoreRangeDto
                    {
                        Min = 81,
                        Max = 100,
                        Level = "Kritik",
                        Description = "Kritik risk seviyesi. Hemen müdahale edilmeli.",
                        Color = "#ef4444" // Kırmızı
                    }
                };

                var dto = new RiskScoreLegendDto
                {
                    Ranges = ranges
                };

                return ApiResult<RiskScoreLegendDto>.Ok(dto, "Risk skor sözlüğü hazırlandı", 200);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Risk skor sözlüğü hazırlanırken hata oluştu");
                return ApiResult<RiskScoreLegendDto>.Fail("Risk skor sözlüğü hazırlanırken bir hata oluştu.", statusCode: 500);
            }
        }

        #endregion
    }
}

