using KuyumStokApi.Application.DTOs.Branches;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    /// <summary>Şube işlemleri API uçları.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class BranchesController : ControllerBase
    {
        private readonly IBranchesService _svc;
        public BranchesController(IBranchesService svc) => _svc = svc;

        /// <summary>Şubeleri filtreleyerek sayfalı listeler (mağaza bilgisiyle).</summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged([FromQuery] BranchFilter filter, CancellationToken ct)
        {
            var r = await _svc.GetPagedAsync(filter, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Id’ye göre şube detayını getirir (mağaza bilgisiyle).</summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var r = await _svc.GetByIdAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Yeni bir şube oluşturur.</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] BranchCreateDto dto, CancellationToken ct)
        {
            var r = await _svc.CreateAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Şubeyi günceller.</summary>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] BranchUpdateDto dto, CancellationToken ct)
        {
            var r = await _svc.UpdateAsync(id, dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Şubeyi soft delete yapar.</summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var r = await _svc.DeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Şubeyi kalıcı olarak siler (hard delete).</summary>
        [HttpDelete("{id:int}/hard")]
        [Authorize]
        public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
        {
            var r = await _svc.HardDeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Şubeyi aktif/pasif yapar.</summary>
        [HttpPut("{id:int}/active")]
        [Authorize]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool value = true, CancellationToken ct = default)
        {
            var r = await _svc.SetActiveAsync(id, value, ct);
            return StatusCode(r.StatusCode, r);
        }
    }
}
