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
        /// <summary>Alışları filtreleyerek sayfalı listeler.</summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged([FromQuery] PurchaseFilter filter, CancellationToken ct)
            => StatusCode((await _svc.GetPagedAsync(filter, ct)).StatusCode, await _svc.GetPagedAsync(filter, ct));

        /// <summary>Alış detayını (satırları ile) getirir.</summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
            => StatusCode((await _svc.GetByIdAsync(id, ct)).StatusCode, await _svc.GetByIdAsync(id, ct));
    }
}
