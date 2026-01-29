using KuyumStokApi.Application.DTOs.Dashboard;
using KuyumStokApi.Application.Hubs;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.DashboardService
{
    public class DashboardNotificationService : IDashboardNotificationService
    {
        private readonly IHubContext<DashboardHub>? _hubContext;
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardNotificationService> _logger;

        // Hangi entity değişikliklerinin hangi dashboard verilerini etkilediğini tanımlar
        private static readonly Dictionary<string, string[]> EntityToDashboardMapping = new()
        {
            // Sales değişiklikleri -> LiveCounters, DailySummary, Anomalies etkilenir
            { nameof(Domain.Entities.Sales), new[] { "LiveCounters", "DailySummary", "Anomalies" } },
            
            // Purchases değişiklikleri -> LiveCounters, DailySummary, Anomalies etkilenir
            { nameof(Domain.Entities.Purchases), new[] { "LiveCounters", "DailySummary", "Anomalies" } },
            
            // Stocks değişiklikleri -> LiveCounters, Anomalies etkilenir
            { nameof(Domain.Entities.Stocks), new[] { "LiveCounters", "Anomalies" } },
            
            // SaleDetails değişiklikleri -> LiveCounters, DailySummary, Anomalies
            { nameof(Domain.Entities.SaleDetails), new[] { "LiveCounters", "DailySummary", "Anomalies" } },
            
            // PurchaseDetails değişiklikleri -> LiveCounters, DailySummary, Anomalies
            { nameof(Domain.Entities.PurchaseDetails), new[] { "LiveCounters", "DailySummary", "Anomalies" } },
            
            // Limits değişiklikleri -> Anomalies etkilenir
            { nameof(Domain.Entities.Limits), new[] { "Anomalies" } },
        };

        public DashboardNotificationService(
            IHubContext<DashboardHub>? hubContext,
            IDashboardService dashboardService,
            ILogger<DashboardNotificationService> logger)
        {
            _hubContext = hubContext;
            _dashboardService = dashboardService;
            _logger = logger;
        }

        public async Task NotifyDashboardChangesAsync(
            IEnumerable<string> changedEntityTypes, 
            CancellationToken ct = default)
        {
            if (_hubContext == null) return;

            var changedTypes = changedEntityTypes.Distinct().ToList();
            if (!changedTypes.Any()) return;

            // Etkilenen dashboard verilerini topla
            var affectedDashboards = new HashSet<string>();
            foreach (var entityType in changedTypes)
            {
                if (EntityToDashboardMapping.TryGetValue(entityType, out var dashboards))
                {
                    foreach (var dashboard in dashboards)
                    {
                        affectedDashboards.Add(dashboard);
                    }
                }
            }

            if (!affectedDashboards.Any()) return;

            // Fire-and-forget: Broadcast'leri paralel olarak başlat, hatalar kritik değil
            _ = Task.Run(async () =>
            {
                try
                {
                    await BroadcastAffectedDashboardsAsync(affectedDashboards, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Dashboard broadcast sırasında hata oluştu");
                }
            }, ct);
        }

        public async Task NotifySaleCommittedAsync(
            int? saleId,
            int? purchaseId,
            CancellationToken ct = default)
        {
            if (_hubContext == null)
            {
                _logger.LogWarning("Dashboard broadcast atlandı (hub yok). SaleId={SaleId}, PurchaseId={PurchaseId}", saleId, purchaseId);
                return;
            }

            var affectedDashboards = new HashSet<string> { "LiveCounters", "DailySummary", "Anomalies" };

            try
            {
                await BroadcastAffectedDashboardsAsync(affectedDashboards, ct);
                _logger.LogInformation("Dashboard broadcast tamamlandı (sale commit). SaleId={SaleId}, PurchaseId={PurchaseId}", saleId, purchaseId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dashboard broadcast başarısız (sale commit). SaleId={SaleId}, PurchaseId={PurchaseId}", saleId, purchaseId);
            }
        }

        private async Task BroadcastAffectedDashboardsAsync(
            HashSet<string> dashboards, 
            CancellationToken ct)
        {
            var tasks = new List<Task>();

            if (dashboards.Contains("LiveCounters"))
            {
                tasks.Add(BroadcastLiveCountersAsync(ct));
            }

            if (dashboards.Contains("DailySummary"))
            {
                tasks.Add(BroadcastDailySummaryAsync(ct));
            }

            if (dashboards.Contains("Anomalies"))
            {
                tasks.Add(BroadcastAnomaliesAsync(ct));
            }

            if (tasks.Any())
            {
                await Task.WhenAll(tasks);
            }
        }

        private async Task BroadcastLiveCountersAsync(CancellationToken ct)
        {
            try
            {
                var result = await _dashboardService.GetLiveCountersAsync(ct);
                if (result.Success && result.Data != null && _hubContext != null)
                {
                    await _hubContext.Clients.All.SendAsync("LiveCountersUpdated", result.Data, ct);
                    _logger.LogDebug("LiveCounters broadcast edildi (olay temelli)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LiveCounters broadcast edilirken hata oluştu");
            }
        }

        private async Task BroadcastDailySummaryAsync(CancellationToken ct)
        {
            try
            {
                var result = await _dashboardService.GetDailySummaryAsync(null, ct);
                if (result.Success && result.Data != null && _hubContext != null)
                {
                    await _hubContext.Clients.All.SendAsync("DailySummaryUpdated", result.Data, ct);
                    _logger.LogDebug("DailySummary broadcast edildi (olay temelli)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DailySummary broadcast edilirken hata oluştu");
            }
        }

        private async Task BroadcastAnomaliesAsync(CancellationToken ct)
        {
            try
            {
                var result = await _dashboardService.GetAnomaliesAsync(ct);
                if (result.Success && result.Data != null && _hubContext != null)
                {
                    await _hubContext.Clients.All.SendAsync("AnomaliesUpdated", result.Data, ct);
                    _logger.LogDebug("Anomalies broadcast edildi (olay temelli)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Anomalies broadcast edilirken hata oluştu");
            }
        }
    }
}
