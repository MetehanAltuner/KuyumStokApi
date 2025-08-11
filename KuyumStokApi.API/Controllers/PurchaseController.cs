using KuyumStokApi.Application.DTOs.Purchase;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class PurchasesController : ControllerBase
    {
        private readonly IPurchasesService _svc;
        public PurchasesController(IPurchasesService svc) => _svc = svc;

        /// <summary>Alış fişi oluşturur ve stoğu artırır.</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] PurchaseCreateDto dto, CancellationToken ct)
        {
            var r = await _svc.CreateAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }
    }
}
