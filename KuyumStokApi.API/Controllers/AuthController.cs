using KuyumStokApi.Application.DTOs.Auth;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KuyumStokApi.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IUserService _users;
        private readonly IJwtService _jwt;

        public AuthController(IUserService users, IJwtService jwt)
        {
            _users = users;
            _jwt = jwt;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // basit model kontrolü
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Kullanıcı adı ve parola zorunludur.");

            Users user;
            try
            {
                user = await _users.RegisterAsync(dto);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message); // username zaten varsa 409
            }

            // İstersen kayıt sonrası token verelim
            var token = _jwt.GenerateToken(user);
            return CreatedAtAction(nameof(Register), new { id = user.Id }, token);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var token = await _users.LoginAsync(dto);
            if (token is null) return Unauthorized();
            return Ok(token);
        }
        /// <summary>Parola gücü ve kuralları için granüler doğrulama.</summary>
        [HttpPost("validate-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidatePassword([FromBody] PasswordCheckRequestDto dto, CancellationToken ct)
        {
            var r = await _users.ValidatePasswordAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }

        /// <summary>Register öncesi tüm alanların granüler doğrulaması.</summary>
        [HttpPost("validate-register")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateRegister([FromBody] RegisterDto dto, CancellationToken ct)
        {
            var r = await _users.ValidateRegisterAsync(dto, ct);
            return StatusCode(r.StatusCode, r);
        }
    }
}
