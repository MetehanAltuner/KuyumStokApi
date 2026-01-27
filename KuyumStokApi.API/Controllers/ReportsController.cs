using KuyumStokApi.Application.DTOs.Reports;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.API.Controllers
{
    /// <summary>Raporlama uçları.</summary>
    [ApiController]
    [Route("api/reports")]
    public sealed class ReportsController : ControllerBase
    {
        private readonly IReportsService _reportsService;

        public ReportsController(IReportsService reportsService)
        {
            _reportsService = reportsService;
        }

        /// <summary>Personel performans raporu (sayfalı).</summary>
        [HttpGet("personnel-performance")]
        [Authorize]
        public async Task<IActionResult> GetPersonnelPerformance([FromQuery] PersonnelPerformanceQueryDto query, CancellationToken ct)
        {
            var result = await _reportsService.GetPersonnelPerformanceAsync(query, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Personel performans raporu XLSX çıktısı.</summary>
        [HttpGet("personnel-performance/export")]
        [Authorize]
        public async Task<IActionResult> ExportPersonnelPerformance([FromQuery] PersonnelPerformanceQueryDto query, CancellationToken ct)
        {
            var result = await _reportsService.ExportPersonnelPerformanceXlsxAsync(query, ct);
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            var fileName = $"personnel_report_{DateTime.UtcNow:yyyyMMdd_HHmm}.xlsx";
            return File(result.Data, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
