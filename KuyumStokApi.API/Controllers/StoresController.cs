using KuyumStokApi.Application.DTOs.Stores;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    /// <summary>Mağaza işlemleri API uçları.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class StoresController : ControllerBase
    {
        private readonly IStoresService _svc;
        public StoresController(IStoresService svc) => _svc = svc;

        /// <summary>Mağazaları filtreleyerek sayfalı listeler (şube sayısı özet bilgiyle).</summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged([FromQuery] StoreFilter filter, CancellationToken ct)
        {
            var r = await _svc.GetPagedAsync(filter, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Id’ye göre mağaza detayını getirir.</summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var r = await _svc.GetByIdAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Yeni bir mağaza oluşturur.</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] StoreCreateDto dto, CancellationToken ct)
        {
            var r = await _svc.CreateAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Mağazayı günceller.</summary>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] StoreUpdateDto dto, CancellationToken ct)
        {
            var r = await _svc.UpdateAsync(id, dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Mağazayı soft delete yapar.</summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var r = await _svc.DeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Mağazayı kalıcı olarak siler (hard delete).</summary>
        [HttpDelete("{id:int}/hard")]
        [Authorize]
        public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
        {
            var r = await _svc.HardDeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Mağazayı aktif/pasif yapar.</summary>
        [HttpPut("{id:int}/active")]
        [Authorize]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool value = true, CancellationToken ct = default)
        {
            var r = await _svc.SetActiveAsync(id, value, ct);
            return StatusCode(r.StatusCode, r);
        }
    }
}
