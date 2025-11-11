using KuyumStokApi.Application.DTOs.ThermalPrinters;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.API.Controllers
{
    /// <summary>Termal yazıcı yönetimi API uçları.</summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class ThermalPrintersController : ControllerBase
    {
        private readonly IThermalPrintersService _service;

        public ThermalPrintersController(IThermalPrintersService service) => _service = service;

        /// <summary>Termal yazıcıları filtreleyerek sayfalı listeler.</summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] ThermalPrinterFilter filter, CancellationToken ct)
        {
            var result = await _service.GetPagedAsync(filter, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Id’ye göre termal yazıcı detayını getirir.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _service.GetByIdAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Yeni termal yazıcı oluşturur.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ThermalPrinterCreateDto dto, CancellationToken ct)
        {
            var result = await _service.CreateAsync(dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Termal yazıcı bilgilerini günceller.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ThermalPrinterUpdateDto dto, CancellationToken ct)
        {
            var result = await _service.UpdateAsync(id, dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Termal yazıcıyı soft-delete yapar.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _service.DeleteAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Termal yazıcıyı kalıcı olarak siler.</summary>
        [HttpDelete("{id:int}/hard")]
        public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
        {
            var result = await _service.HardDeleteAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}


