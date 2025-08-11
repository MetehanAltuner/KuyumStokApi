using KuyumStokApi.Application.DTOs.Customers;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    /// <summary>Müşteri CRUD ve listeme uçları.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class CustomersController : ControllerBase
    {
        private readonly ICustomersService _svc;
        public CustomersController(ICustomersService svc) => _svc = svc;

        /// <summary>Müşterileri filtreleyerek sayfalı listeler.</summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged([FromQuery] CustomerFilter filter, CancellationToken ct)
            => StatusCode((await _svc.GetPagedAsync(filter, ct)).StatusCode, await _svc.GetPagedAsync(filter, ct));

        /// <summary>Id’ye göre müşteri detayını getirir.</summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var r = await _svc.GetByIdAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Yeni müşteri oluşturur.</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto, CancellationToken ct)
        {
            var r = await _svc.CreateAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Mevcut müşteriyi günceller.</summary>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateDto dto, CancellationToken ct)
        {
            var r = await _svc.UpdateAsync(id, dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Müşteriyi siler (ilişkisi varsa soft-delete).</summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var r = await _svc.DeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }
    }
}
