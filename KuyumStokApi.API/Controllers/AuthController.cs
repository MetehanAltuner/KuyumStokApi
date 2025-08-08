using KuyumStokApi.Application.DTOs.Auth;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Domain.Entities;
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
    }
}
