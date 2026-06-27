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
        /// Tüm parametresiz dashboard verilerini tek seferde döndürür (birleşik endpoint - broadcast yapmaz)
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken ct = default)
        {
            var result = await _dashboardService.GetSummaryAsync(ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gerçek zamanlı canlı sayaçlar (son satış zamanı, bugünkü işlem sayısı, stok senkronizasyonu) - Broadcast yapar
        /// </summary>
        [HttpGet("live-counters")]
        public async Task<IActionResult> GetLiveCounters(CancellationToken ct = default)
        {
            var result = await _dashboardService.GetLiveCountersAsync(ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Gün sonu raporu (satış, kâr, en çok satan ürün, kritik stok) - Broadcast yapar
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
        /// Anomali algılama (satış düşüşü, stok seviyesi, risk skorları) - Broadcast yapar
        /// </summary>
        [HttpGet("anomalies")]
        public async Task<IActionResult> GetAnomalies(CancellationToken ct = default)
        {
            var result = await _dashboardService.GetAnomaliesAsync(ct);
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
        /// Günlük en çok satan ürün trendi (son N gün)
        /// </summary>
        /// <param name="days">Kaç gün (default: 7, min: 1, max: 90)</param>
        /// <param name="ct">İptal token'ı</param>
        [HttpGet("daily-top-selling")]
        public async Task<IActionResult> GetDailyTopSelling([FromQuery] int days = 7, CancellationToken ct = default)
        {
            var result = await _dashboardService.GetDailyTopSellingTrendAsync(days, ct);
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
        /// Ürün kategorisi bazlı satış pasta grafiği (son N gün). Sadece seçilen dönemde satışı olan kategoriler döner.
        /// </summary>
        /// <param name="storeId">Mağaza Id (zorunlu, 0'dan büyük)</param>
        /// <param name="branchId">Şube Id (opsiyonel; verilirse sadece o şube, verilmezse mağazanın tüm şubeleri)</param>
        /// <param name="days">Kaç gün (default: 7, min: 1, max: 90) - 400 hatası döner</param>
        /// <param name="ct">İptal token'ı</param>
        /// <response code="400">storeId veya days geçersiz; şube mağazaya ait değil</response>
        /// <response code="403">Yetkisiz şube veya mağaza</response>
        [HttpGet("sales-pie")]
        public async Task<IActionResult> GetSalesPieChart(
            [FromQuery] int storeId,
            [FromQuery] int? branchId = null,
            [FromQuery] int days = 7,
            CancellationToken ct = default)
        {
            var result = await _dashboardService.GetSalesPieChartAsync(storeId, branchId, days, ct);
            return StatusCode(result.StatusCode, result);
        }

    }
}

