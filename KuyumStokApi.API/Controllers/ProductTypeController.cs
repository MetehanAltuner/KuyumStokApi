using KuyumStokApi.Application.DTOs.ProductType;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ProductTypesController : ControllerBase
    {
        private readonly IProductTypeService _svc;
        public ProductTypesController(IProductTypeService svc) => _svc = svc;

        /// <summary>Ürün türlerini filtreleyerek sayfalı listeler (kategori bilgisiyle).</summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged([FromQuery] ProductTypeFilter filter, CancellationToken ct)
        {
            var r = await _svc.GetPagedAsync(filter, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Id’ye göre ürün türü detayını getirir (kategori bilgisiyle).</summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var r = await _svc.GetByIdAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Yeni bir ürün türü oluşturur.</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] ProductTypeCreateDto dto, CancellationToken ct)
        {
            var r = await _svc.CreateAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Ürün türünü günceller.</summary>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] ProductTypeUpdateDto dto, CancellationToken ct)
        {
            var r = await _svc.UpdateAsync(id, dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Ürün türünü soft delete yapar.</summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var r = await _svc.DeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Ürün türünü kalıcı olarak siler (hard delete).</summary>
        [HttpDelete("{id:int}/hard")]
        [Authorize]
        public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
        {
            var r = await _svc.HardDeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Ürün türünü aktif/pasif yapar.</summary>
        [HttpPut("{id:int}/active")]
        [Authorize]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool value = true, CancellationToken ct = default)
        {
            var r = await _svc.SetActiveAsync(id, value, ct);
            return StatusCode(r.StatusCode, r);
        }
    }
}
