using KuyumStokApi.Application.DTOs.ProductLifecycles;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class ProductLifecyclesController : ControllerBase
    {
        private readonly IProductLifecyclesService _svc;
        public ProductLifecyclesController(IProductLifecyclesService svc) => _svc = svc;

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged([FromQuery] ProductLifecycleFilter f, CancellationToken ct)
        {
            var r = await _svc.GetPagedAsync(f, ct);
            return StatusCode(r.StatusCode, r);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var r = await _svc.GetByIdAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(ProductLifecycleCreateDto dto, CancellationToken ct)
        {
            var r = await _svc.CreateAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }
    }
}
