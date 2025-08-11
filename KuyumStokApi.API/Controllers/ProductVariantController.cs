using KuyumStokApi.Application.DTOs.ProductVariant.KuyumStokApi.Application.DTOs.ProductVariants;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    /// <summary>Ürün varyantı işlemleri API uçları.</summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ProductVariantsController : ControllerBase
    {
        private readonly IProductVariantService _svc;
        public ProductVariantsController(IProductVariantService svc) => _svc = svc;

        /// <summary>Varyantları filtreleyerek sayfalı listeler (bağlı ürün türü bilgisiyle).</summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged([FromQuery] ProductVariantFilter filter, CancellationToken ct)
        {
            var r = await _svc.GetPagedAsync(filter, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Id’ye göre varyant detayını getirir (bağlı ürün türü bilgisiyle).</summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var r = await _svc.GetByIdAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Yeni bir varyant oluşturur.</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] ProductVariantCreateDto dto, CancellationToken ct)
        {
            var r = await _svc.CreateAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Varyantı günceller.</summary>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] ProductVariantUpdateDto dto, CancellationToken ct)
        {
            var r = await _svc.UpdateAsync(id, dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Varyantı soft delete yapar.</summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var r = await _svc.DeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Varyantı kalıcı olarak siler (hard delete).</summary>
        [HttpDelete("{id:int}/hard")]
        [Authorize]
        public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
        {
            var r = await _svc.HardDeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Varyantı aktif/pasif yapar.</summary>
        [HttpPut("{id:int}/active")]
        [Authorize]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool value = true, CancellationToken ct = default)
        {
            var r = await _svc.SetActiveAsync(id, value, ct);
            return StatusCode(r.StatusCode, r);
        }
    }
}
