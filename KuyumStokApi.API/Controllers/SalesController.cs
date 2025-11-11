using KuyumStokApi.Application.DTOs.Receipts;
using KuyumStokApi.Application.DTOs.Sales;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class SalesController : ControllerBase
    {
        private readonly ISalesService _svc;
        public SalesController(ISalesService svc) => _svc = svc;

        /// <summary>Satış/alış içeren birleşik fiş oluşturur.</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] UnifiedReceiptCreateDto dto, CancellationToken ct)
        {
            var r = await _svc.CreateUnifiedAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }
        /// <summary>Satışları filtreleyerek sayfalı listeler.</summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged([FromQuery] SaleFilter filter, CancellationToken ct)
            => StatusCode((await _svc.GetPagedAsync(filter, ct)).StatusCode, await _svc.GetPagedAsync(filter, ct));

        /// <summary>Satış detayını (satırları ile) getirir.</summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
            => StatusCode((await _svc.GetLineByIdAsync(id, ct)).StatusCode, await _svc.GetLineByIdAsync(id, ct));
    }
}
