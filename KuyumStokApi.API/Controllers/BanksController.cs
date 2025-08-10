using KuyumStokApi.Application.DTOs.Banks;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class BanksController : ControllerBase
    {
        private readonly IBanksService _svc;
        public BanksController(IBanksService svc) => _svc = svc;

        /// <summary>Bankaları filtreleyerek sayfalı listeler.</summary>
        /// <remarks>
        /// Query/name araması, güncellenme tarihi aralığı ve sayfalama desteklenir.
        /// </remarks>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged([FromQuery] BankFilter filter, CancellationToken ct)
        {
            var r = await _svc.GetPagedAsync(filter, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Id’ye göre banka detayını getirir.</summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var r = await _svc.GetByIdAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Yeni bir banka kaydı oluşturur.</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] BankCreateDto dto, CancellationToken ct)
        {
            var r = await _svc.CreateAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Var olan banka kaydını günceller.</summary>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] BankUpdateDto dto, CancellationToken ct)
        {
            var r = await _svc.UpdateAsync(id, dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Belirtilen id’deki banka kaydını siler.</summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var r = await _svc.DeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }
    }
}
