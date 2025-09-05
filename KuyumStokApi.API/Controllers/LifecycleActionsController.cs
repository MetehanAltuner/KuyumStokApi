using KuyumStokApi.Application.DTOs.LifeCycleActions;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class LifecycleActionsController : ControllerBase
    {
        private readonly ILifecycleActionsService _svc;
        public LifecycleActionsController(ILifecycleActionsService svc) => _svc = svc;

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged([FromQuery] LifecycleActionFilter f, CancellationToken ct)
            => StatusCode((await _svc.GetPagedAsync(f, ct)).StatusCode, await _svc.GetPagedAsync(f, ct));

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
            => StatusCode((await _svc.GetByIdAsync(id, ct)).StatusCode, await _svc.GetByIdAsync(id, ct));

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(LifecycleActionCreateDto dto, CancellationToken ct)
            => StatusCode((await _svc.CreateAsync(dto, ct)).StatusCode, await _svc.CreateAsync(dto, ct));

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, LifecycleActionUpdateDto dto, CancellationToken ct)
            => StatusCode((await _svc.UpdateAsync(id, dto, ct)).StatusCode, await _svc.UpdateAsync(id, dto, ct));

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
            => StatusCode((await _svc.DeleteAsync(id, ct)).StatusCode, await _svc.DeleteAsync(id, ct));
    }
}
