using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.API.Controllers
{
    /// <summary>
    /// Dashboard verileri için controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Gerçek zamanlı canlı sayaçlar (son satış zamanı, bugünkü işlem sayısı, stok senkronizasyonu)
        /// </summary>
        [HttpGet("live-counters")]
        public async Task<IActionResult> GetLiveCounters(CancellationToken ct = default)
        {
            var result = await _dashboardService.GetLiveCountersAsync(ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Haftalık trend grafik verisi
        /// </summary>
        [HttpGet("weekly-trend")]
        public async Task<IActionResult> GetWeeklyTrend(CancellationToken ct = default)
        {
            var result = await _dashboardService.GetWeeklyTrendAsync(ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gün sonu raporu (satış, kâr, en çok satan ürün, kritik stok)
        /// </summary>
        /// <param name="date">Tarih (opsiyonel, default bugün)</param>
        /// <param name="ct">İptal token'ı</param>
        [HttpGet("daily-summary")]
        public async Task<IActionResult> GetDailySummary([FromQuery] DateTime? date = null, CancellationToken ct = default)
        {
            var result = await _dashboardService.GetDailySummaryAsync(date, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Anomali algılama (satış düşüşü, stok seviyesi, risk skorları)
        /// </summary>
        [HttpGet("anomalies")]
        public async Task<IActionResult> GetAnomalies(CancellationToken ct = default)
        {
            var result = await _dashboardService.GetAnomaliesAsync(ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Aylık satış hedefi (hedef tutar, mevcut satış, ilerleme yüzdesi)
        /// </summary>
        [HttpGet("monthly-target")]
        public async Task<IActionResult> GetMonthlyTarget(CancellationToken ct = default)
        {
            var result = await _dashboardService.GetMonthlyTargetAsync(ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Hatırlatıcılar ve ajanda (kritik stok, uzun süre satılmayan ürünler, stok tükenme tahmini)
        /// </summary>
        [HttpGet("reminders")]
        public async Task<IActionResult> GetReminders(CancellationToken ct = default)
        {
            var result = await _dashboardService.GetRemindersAsync(ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// En çok satan ürünler
        /// </summary>
        /// <param name="limit">Kaç ürün döndürülecek (default: 5)</param>
        /// <param name="period">Dönem: week, month, all (default: week)</param>
        /// <param name="ct">İptal token'ı</param>
        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 5, [FromQuery] string period = "week", CancellationToken ct = default)
        {
            var result = await _dashboardService.GetTopProductsAsync(limit, period, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Günlük iş yükü tahmini (yoğunluk seviyesi, tahmini işlem sayısı)
        /// </summary>
        [HttpGet("workload-estimate")]
        public async Task<IActionResult> GetWorkloadEstimate(CancellationToken ct = default)
        {
            var result = await _dashboardService.GetWorkloadEstimateAsync(ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Şube karşılaştırması (satış, kâr, fiş sayısı, POS oranı, kritik stok, trend)
        /// </summary>
        [HttpGet("branch-comparison")]
        public async Task<IActionResult> GetBranchComparison(CancellationToken ct = default)
        {
            var result = await _dashboardService.GetBranchComparisonAsync(ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Kar-Zarar tablosu (dönemsel kar-zarar analizi)
        /// </summary>
        /// <param name="period">Dönem: day, week, month (default: week)</param>
        /// <param name="ct">İptal token'ı</param>
        [HttpGet("profit-loss")]
        public async Task<IActionResult> GetProfitLoss([FromQuery] string period = "week", CancellationToken ct = default)
        {
            var result = await _dashboardService.GetProfitLossAsync(period, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Risk skor sözlüğü (0-100 aralığında risk seviyeleri ve açıklamaları)
        /// </summary>
        [HttpGet("risk-score-legend")]
        public async Task<IActionResult> GetRiskScoreLegend(CancellationToken ct = default)
        {
            var result = await _dashboardService.GetRiskScoreLegendAsync(ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}

