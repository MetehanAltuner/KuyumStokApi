using KuyumStokApi.Application.DTOs.Dashboard;
using KuyumStokApi.Application.Hubs;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.DashboardService
{
    /// <summary>
    /// Dashboard verilerini periyodik olarak SignalR hub üzerinden broadcast eden background service
    /// </summary>
    public sealed class DashboardBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<DashboardBackgroundService> _logger;

        public DashboardBackgroundService(
            IServiceProvider serviceProvider,
            IHubContext<DashboardHub> hubContext,
            ILogger<DashboardBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DashboardBackgroundService başlatıldı");

            // İlk güncellemeleri hemen gönder
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            var liveCountersTimer = TimeSpan.FromSeconds(30); // Her 30 saniyede bir
            var dailySummaryTimer = TimeSpan.FromMinutes(1); // Her 1 dakikada bir
            var anomaliesTimer = TimeSpan.FromMinutes(2); // Her 2 dakikada bir
            var summaryTimer = TimeSpan.FromMinutes(1); // Her 1 dakikada bir (summary)

            var lastLiveCountersUpdate = DateTime.UtcNow;
            var lastDailySummaryUpdate = DateTime.UtcNow;
            var lastAnomaliesUpdate = DateTime.UtcNow;
            var lastSummaryUpdate = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;

                    // LiveCounters güncellemesi (30 saniyede bir)
                    if (now - lastLiveCountersUpdate >= liveCountersTimer)
                    {
                        await UpdateLiveCountersAsync(stoppingToken);
                        lastLiveCountersUpdate = now;
                    }

                    // DailySummary güncellemesi (1 dakikada bir)
                    if (now - lastDailySummaryUpdate >= dailySummaryTimer)
                    {
                        await UpdateDailySummaryAsync(stoppingToken);
                        lastDailySummaryUpdate = now;
                    }

                    // Anomalies güncellemesi (2 dakikada bir)
                    if (now - lastAnomaliesUpdate >= anomaliesTimer)
                    {
                        await UpdateAnomaliesAsync(stoppingToken);
                        lastAnomaliesUpdate = now;
                    }

                    // Summary güncellemesi (1 dakikada bir)
                    if (now - lastSummaryUpdate >= summaryTimer)
                    {
                        await UpdateSummaryAsync(stoppingToken);
                        lastSummaryUpdate = now;
                    }

                    // Her 5 saniyede bir kontrol et
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("DashboardBackgroundService durduruluyor");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DashboardBackgroundService'te hata oluştu");
                    // Hata durumunda servisi durdurmadan devam et
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            _logger.LogInformation("DashboardBackgroundService durduruldu");
        }

        private async Task UpdateLiveCountersAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Tüm aktif şubeler için veri çek (background service için)
                var branchIds = await db.Branches.AsNoTracking()
                    .Where(b => !b.IsDeleted)
                    .Select(b => b.Id)
                    .ToListAsync(ct);

                if (!branchIds.Any())
                {
                    _logger.LogWarning("Aktif şube bulunamadı");
                    return;
                }

                var now = DateTime.UtcNow;
                var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

                // Son satış zamanı
                var lastSale = await db.Sales.AsNoTracking()
                    .Where(s => s.BranchId != null && branchIds.Contains(s.BranchId.Value))
                    .OrderByDescending(s => s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue)
                    .Select(s => s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue)
                    .FirstOrDefaultAsync(ct);

                var minutesSinceLastSale = lastSale != DateTime.MinValue
                    ? (int)(now - lastSale).TotalMinutes
                    : 0;

                // Bugünkü işlem sayısı
                var todaySalesCount = await db.Sales.AsNoTracking()
                    .Where(s => s.BranchId != null && branchIds.Contains(s.BranchId.Value) &&
                                (s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue) >= todayStart)
                    .CountAsync(ct);

                var todayPurchasesCount = await db.Purchases.AsNoTracking()
                    .Where(p => p.BranchId != null && branchIds.Contains(p.BranchId.Value) &&
                                (p.CreatedAt ?? p.UpdatedAt ?? DateTime.MinValue) >= todayStart)
                    .CountAsync(ct);

                var todayTransactionCount = todaySalesCount + todayPurchasesCount;

                // Stok senkronizasyon zamanı
                var lastStockSync = await db.Stocks.AsNoTracking()
                    .Where(s => s.BranchId != null && branchIds.Contains(s.BranchId.Value))
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

                await _hubContext.Clients.All.SendAsync("LiveCountersUpdated", dto, ct);
                _logger.LogDebug("LiveCounters broadcast edildi");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LiveCounters güncellenirken hata oluştu");
            }
        }

        private async Task UpdateDailySummaryAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Tüm aktif şubeler için veri çek
                var branchIds = await db.Branches.AsNoTracking()
                    .Where(b => !b.IsDeleted)
                    .Select(b => b.Id)
                    .ToListAsync(ct);

                if (!branchIds.Any())
                {
                    _logger.LogWarning("Aktif şube bulunamadı");
                    return;
                }

                var targetDate = DateTime.UtcNow;
                var dayStart = new DateTime(targetDate.Year, targetDate.Month, targetDate.Day, 0, 0, 0, DateTimeKind.Utc);
                var dayEnd = dayStart.AddDays(1).AddTicks(-1);

                // Satış toplamı
                var totalSales = await (from d in db.SaleDetails.AsNoTracking()
                                       join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                                       where s.BranchId != null && branchIds.Contains(s.BranchId.Value)
                                       let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                       where created >= dayStart && created <= dayEnd
                                       select (d.Quantity ?? 0) * (d.SoldPrice ?? 0m))
                    .SumAsync(ct);

                // Alış maliyeti
                var totalCost = await (from d in db.PurchaseDetails.AsNoTracking()
                                      join p in db.Purchases.AsNoTracking() on d.PurchaseId equals p.Id
                                      where p.BranchId != null && branchIds.Contains(p.BranchId.Value)
                                      let created = p.CreatedAt ?? p.UpdatedAt ?? DateTime.MinValue
                                      where created >= dayStart && created <= dayEnd
                                      select (d.Quantity ?? 0) * (d.PurchasePrice ?? 0m))
                    .SumAsync(ct);

                var totalProfit = totalSales - totalCost;
                var profitPercentage = totalSales > 0 ? (totalProfit / totalSales) * 100 : 0;

                // En çok satan ürün
                var topSellingProduct = await (from d in db.SaleDetails.AsNoTracking()
                                             join s in db.Sales.AsNoTracking() on d.SaleId equals s.Id
                                             join st in db.Stocks.AsNoTracking() on d.StockId equals st.Id
                                             join pv in db.ProductVariants.AsNoTracking() on st.ProductVariantId equals pv.Id
                                             where s.BranchId != null && branchIds.Contains(s.BranchId.Value)
                                             let created = s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue
                                             where created >= dayStart && created <= dayEnd
                                             group d by pv.Name into g
                                             orderby g.Sum(x => x.Quantity ?? 0) descending
                                             select g.Key)
                    .FirstOrDefaultAsync(ct);

                // Kritik stok sayısı (basitleştirilmiş - limit kontrolü yapmadan)
                var criticalStockCount = await db.Stocks.AsNoTracking()
                    .Where(s => s.BranchId != null && branchIds.Contains(s.BranchId.Value) && (s.Quantity ?? 0) < 5)
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

                await _hubContext.Clients.All.SendAsync("DailySummaryUpdated", dto, ct);
                _logger.LogDebug("DailySummary broadcast edildi");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DailySummary güncellenirken hata oluştu");
            }
        }

        private async Task UpdateAnomaliesAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var anomalyDetectionService = scope.ServiceProvider.GetRequiredService<KuyumStokApi.Infrastructure.Services.AnomalyDetectionService.AnomalyDetectionService>();

                // Tüm aktif şubeler için veri çek
                var branchIds = await db.Branches.AsNoTracking()
                    .Where(b => !b.IsDeleted)
                    .Select(b => b.Id)
                    .ToListAsync(ct);

                if (!branchIds.Any())
                {
                    _logger.LogWarning("Aktif şube bulunamadı");
                    return;
                }

                // Anomali tespiti için basit bir kontrol yap
                var anomalies = new List<AnomalyDto>();

                // Son 7 gün satış kontrolü
                var last7Days = DateTime.UtcNow.AddDays(-7);
                var previous7Days = last7Days.AddDays(-7);

                var recentSales = await db.Sales.AsNoTracking()
                    .Where(s => s.BranchId != null && branchIds.Contains(s.BranchId.Value) &&
                                (s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue) >= last7Days)
                    .CountAsync(ct);

                var previousSales = await db.Sales.AsNoTracking()
                    .Where(s => s.BranchId != null && branchIds.Contains(s.BranchId.Value) &&
                                (s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue) >= previous7Days &&
                                (s.CreatedAt ?? s.UpdatedAt ?? DateTime.MinValue) < last7Days)
                    .CountAsync(ct);

                if (previousSales > 0)
                {
                    var dropPercentage = ((previousSales - recentSales) / (double)previousSales) * 100;
                    if (dropPercentage > 30)
                    {
                        anomalies.Add(new AnomalyDto
                        {
                            Type = "HighSalesDrop",
                            Description = $"Satışlarda %{dropPercentage:F1} düşüş tespit edildi.",
                            RiskScore = Math.Min(100, (int)dropPercentage)
                        });
                    }
                }

                // Kritik stok kontrolü
                var lowStockCount = await db.Stocks.AsNoTracking()
                    .Where(s => s.BranchId != null && branchIds.Contains(s.BranchId.Value) && (s.Quantity ?? 0) < 5)
                    .CountAsync(ct);

                if (lowStockCount > 10)
                {
                    anomalies.Add(new AnomalyDto
                    {
                        Type = "LowStockLevel",
                        Description = $"{lowStockCount} ürün kritik stok seviyesinde.",
                        RiskScore = Math.Min(100, lowStockCount * 5)
                    });
                }

                if (anomalies.Count == 0)
                {
                    anomalies.Add(new AnomalyDto
                    {
                        Type = "NormalSales",
                        Description = "Her şey normal görünüyor.",
                        RiskScore = 0
                    });
                }

                await _hubContext.Clients.All.SendAsync("AnomaliesUpdated", anomalies, ct);
                _logger.LogDebug("Anomalies broadcast edildi");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Anomalies güncellenirken hata oluştu");
            }
        }

        private async Task UpdateSummaryAsync(CancellationToken ct)
        {
            try
            {
                // Summary için diğer metodları kullan (basit bir implementasyon)
                // Veya sadece LiveCounters, DailySummary ve Anomalies'i birleştir
                // Şimdilik sadece log yap, summary broadcast'i opsiyonel
                _logger.LogDebug("Summary broadcast atlandı (diğer event'ler yeterli)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Summary güncellenirken hata oluştu");
            }
        }
    }
}

