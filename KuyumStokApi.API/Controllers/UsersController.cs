using KuyumStokApi.Application.DTOs.Users;
using KuyumStokApi.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    /// <summary>Kullanıcı yönetimi uçları.</summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class UsersController : ControllerBase
    {
        private readonly IUserService _svc;
        public UsersController(IUserService svc) => _svc = svc;

        /// <summary>Kullanıcıları filtreleyerek sayfalı listeler.</summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] UserFilter filter, CancellationToken ct)
        {
            var result = await _svc.GetPagedAsync(filter, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Kimliğe göre kullanıcı detayını getirir.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _svc.GetByIdAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Kullanıcıyı günceller.</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDto dto, CancellationToken ct)
        {
            var result = await _svc.UpdateAsync(id, dto, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Kullanıcıyı soft delete yapar.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var result = await _svc.DeleteAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Kullanıcıyı kalıcı olarak siler.</summary>
        [HttpDelete("{id:int}/hard")]
        public async Task<IActionResult> HardDelete(int id, CancellationToken ct)
        {
            var result = await _svc.HardDeleteAsync(id, ct);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Kullanıcının aktiflik durumunu değiştirir.</summary>
        [HttpPut("{id:int}/active")]
        public async Task<IActionResult> SetActive(int id, [FromQuery] bool value = true, CancellationToken ct = default)
        {
            var result = await _svc.SetActiveAsync(id, value, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}

