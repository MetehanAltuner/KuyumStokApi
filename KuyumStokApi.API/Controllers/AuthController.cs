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
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ITokenBlacklistService _tokenBlacklistService;

        public AuthController(IUserService users, IJwtService jwt, IRefreshTokenService refreshTokenService, ITokenBlacklistService tokenBlacklistService)
        {
            _users = users;
            _jwt = jwt;
            _refreshTokenService = refreshTokenService;
            _tokenBlacklistService = tokenBlacklistService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Password ve Username artık optional - kontrol kaldırıldı
            Users user;
            try
            {
                user = await _users.RegisterAsync(dto);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }

            // Kayıt sonrası token ve refresh token oluştur
            var response = _jwt.GenerateToken(user);
            var (refreshToken, refreshTokenExpiration) = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id);
            response.RefreshToken = refreshToken;
            response.RefreshTokenExpiration = refreshTokenExpiration;
            response.MustChangePassword = user.MustChangePassword;

            return CreatedAtAction(nameof(Register), new { id = user.Id }, response);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var token = await _users.LoginAsync(dto);
            if (token is null) return Unauthorized();
            return Ok(token);
        }

        /// <summary>Refresh token ile yeni access token al (refresh token NOT a JWT, just a random string)</summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return BadRequest("Refresh token gereklidir.");

            // Validate refresh token ve get user (includes role and branch)
            var user = await _refreshTokenService.GetUserByRefreshTokenAsync(dto.RefreshToken, ct);
            if (user == null)
                return Unauthorized("Geçersiz veya süresi dolmuş refresh token.");

            // Eski refresh token'ı revoke et (rotation: her refresh'te yeni token)
            await _refreshTokenService.RevokeRefreshTokenAsync(dto.RefreshToken, ct);

            // Yeni access token oluştur
            var response = _jwt.GenerateToken(user);
            
            // Yeni refresh token oluştur (8 saat expire)
            var (newRefreshToken, refreshTokenExpiration) = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id, ct);
            
            response.RefreshToken = newRefreshToken;
            response.RefreshTokenExpiration = refreshTokenExpiration;
            response.MustChangePassword = user.MustChangePassword;

            return Ok(response);
        }

        /// <summary>Logout - JWT'yi blacklist'e ekle ve kullanıcının tüm refresh token'larını revoke et</summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(CancellationToken ct = default)
        {
            // JWT'den jti ve userId claim'lerini al
            var jtiClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti);
            var expClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp);
            var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            // JWT'yi blacklist'e ekle
            if (jtiClaim != null && expClaim != null)
            {
                var jti = jtiClaim.Value;
                var exp = long.Parse(expClaim.Value);
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;

                await _tokenBlacklistService.InvalidateTokenAsync(jti, expiresAt, ct);
            }

            // Kullanıcının tüm refresh token'larını revoke et (best practice)
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                await _refreshTokenService.RevokeAllUserTokensAsync(userId, ct);
            }

            return Ok(new { message = "Başarıyla çıkış yapıldı." });
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
