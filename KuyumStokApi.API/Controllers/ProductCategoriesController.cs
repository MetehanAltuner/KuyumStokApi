using KuyumStokApi.Application.DTOs.ProductCategory;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ProductCategoriesController : ControllerBase
    {
        private readonly IProductCategoryService _svc;
        public ProductCategoriesController(IProductCategoryService svc) => _svc = svc;

        /// <summary>Kategorileri filtreleyerek sayfalı listeler.</summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged([FromQuery] ProductCategoryFilter filter, CancellationToken ct)
        {
            var r = await _svc.GetPagedAsync(filter, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Id’ye göre kategori detayını getirir.</summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var r = await _svc.GetByIdAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Yeni bir kategori oluşturur.</summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] ProductCategoryCreateDto dto, CancellationToken ct)
        {
            var r = await _svc.CreateAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Kategoriyi günceller.</summary>
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] ProductCategoryUpdateDto dto, CancellationToken ct)
        {
            var r = await _svc.UpdateAsync(id, dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Kategoriyi soft delete yapar.</summary>
        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var r = await _svc.DeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Kategoriyi kalıcı olarak siler (hard delete).</summary>
        [HttpDelete("{id:int}/hard")]
        [Authorize]
        public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
        {
            var r = await _svc.HardDeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Kategoriyi aktif/pasif yapar.</summary>
        [HttpPut("{id:int}/active")]
        [Authorize]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool value = true, CancellationToken ct = default)
        {
            var r = await _svc.SetActiveAsync(id, value, ct);
            return StatusCode(r.StatusCode, r);
        }
    }
}
