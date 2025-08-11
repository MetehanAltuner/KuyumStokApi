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

        /// <summary>Satış fişi oluşturur ve stoğu düşer.</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] SaleCreateDto dto, CancellationToken ct)
        {
            var r = await _svc.CreateAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }
    }
}
