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
            => StatusCode((await _svc.GetPagedAsync(f, ct)).StatusCode, await _svc.GetPagedAsync(f, ct));

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
            => StatusCode((await _svc.GetByIdAsync(id, ct)).StatusCode, await _svc.GetByIdAsync(id, ct));

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(ProductLifecycleCreateDto dto, CancellationToken ct)
            => StatusCode((await _svc.CreateAsync(dto, ct)).StatusCode, await _svc.CreateAsync(dto, ct));
    }
}
