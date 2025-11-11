using KuyumStokApi.Application.DTOs.Reports;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class ReportsController : ControllerBase
    {
        private readonly IReportsService _reports;

        public ReportsController(IReportsService reports)
        {
            _reports = reports;
        }

        /// <summary>
        /// Mağaza sahibi/üst düzey roller için genel satış raporları.
        /// </summary>
        [HttpGet("store-overview")]
        public async Task<IActionResult> GetStoreOverview([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
        {
            var range = new ReportDateRange(fromUtc, toUtc);
            var result = await _reports.GetStoreOverviewAsync(range, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Şube bazlı performans raporu (şube müdürü yetkisi).
        /// </summary>
        [HttpGet("branch-overview")]
        public async Task<IActionResult> GetBranchOverview([FromQuery] int? branchId, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
        {
            var range = new ReportDateRange(fromUtc, toUtc);
            var result = await _reports.GetBranchOverviewAsync(branchId, range, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Kullanıcı bazlı satış performans raporu.
        /// </summary>
        [HttpGet("user-performance")]
        public async Task<IActionResult> GetUserPerformance([FromQuery] int? userId, [FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken ct)
        {
            var range = new ReportDateRange(fromUtc, toUtc);
            var result = await _reports.GetUserPerformanceAsync(userId, range, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Satış trendi (grafik) verilerini döner.
        /// </summary>
        /// <param name="granularity">Grafik dilim genişliği: Daily = günlük, Weekly = haftanın başlangıcı (Pazartesi), Monthly = ayın ilk günü.</param>
        /// <param name="fromUtc">Opsiyonel başlangıç zamanı (UTC).</param>
        /// <param name="toUtc">Opsiyonel bitiş zamanı (UTC).</param>
        [HttpGet("sales-trend")]
        public async Task<IActionResult> GetSalesTrend([FromQuery] ReportTrendGranularity granularity = ReportTrendGranularity.Daily, [FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null, CancellationToken ct = default)
        {
            var range = new ReportDateRange(fromUtc, toUtc);
            var result = await _reports.GetSalesTrendAsync(granularity, range, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}

