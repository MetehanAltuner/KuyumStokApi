using KuyumStokApi.Application.DTOs.ProductCategories;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class ProductCategoriesController : ControllerBase
    {
        private readonly IProductCategoryService _svc;
        public ProductCategoriesController(IProductCategoryService svc) => _svc = svc;

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var r = await _svc.GetAllAsync();
            return StatusCode(r.StatusCode, r);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var r = await _svc.GetByIdAsync(id);
            return StatusCode(r.StatusCode, r);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] ProductCategoryCreateDto dto)
        {
            var r = await _svc.CreateAsync(dto);
            return StatusCode(r.StatusCode, r);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] ProductCategoryUpdateDto dto)
        {
            var r = await _svc.UpdateAsync(id, dto);
            return StatusCode(r.StatusCode, r);
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _svc.DeleteAsync(id);
            return StatusCode(r.StatusCode, r);
        }
    }
}
