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
        public async Task<IActionResult> Create(LifecycleActionCreateDto dto, CancellationToken ct)
        {
            var r = await _svc.CreateAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, LifecycleActionUpdateDto dto, CancellationToken ct)
        {
            var r = await _svc.UpdateAsync(id, dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var r = await _svc.DeleteAsync(id, ct);
            return StatusCode(r.StatusCode, r);
        }
    }
}
