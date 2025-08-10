using KuyumStokApi.Application.DTOs.Auth;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.JwtService
{

    public sealed class JwtService : IJwtService
    {
        private static byte[] DecodeKey(string? b64)
        {
            if (string.IsNullOrWhiteSpace(b64))
                throw new InvalidOperationException("Jwt:Key boş!");
            var clean = b64.Trim();
            return Convert.FromBase64String(clean);
        }

        private readonly JwtOptions _opt;
        private readonly SigningCredentials _creds;
        private readonly JwtHeader _headerTemplate;

        public JwtService(IOptions<JwtOptions> options)
        {
            _opt = options.Value ?? throw new ArgumentNullException(nameof(options));

            var keyBytes = DecodeKey(_opt.Key);
            var securityKey = new SymmetricSecurityKey(keyBytes);
            _creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            _headerTemplate = new JwtHeader(_creds);
            if (!string.IsNullOrWhiteSpace(_opt.KeyId))
            {
                // kid header anahtar rotasyonu için
                _headerTemplate["kid"] = _opt.KeyId;
            }
        }

        public AuthResponseDto GenerateToken(Users user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_opt.ExpiryMinutes);

            var claims = BuildClaims(user);

            var token = new JwtSecurityToken(
                issuer: _opt.Issuer,
                audience: _opt.Audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: _creds
            );

            // Header template’teki kid vs. değerlerini uygula
            foreach (var kv in _headerTemplate)
                token.Header[kv.Key] = kv.Value;

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new AuthResponseDto
            {
                Token = tokenString,
                Expiration = expires
            };
        }

        private static IEnumerable<Claim> BuildClaims(Users user)
        {
            // PII içermeyen, minimal claim seti
            yield return new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString());
            yield return new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString());
            yield return new Claim(JwtRegisteredClaimNames.UniqueName, user.Username);

            // İsim-soyisim (varsa)
            if (!string.IsNullOrWhiteSpace(user.FirstName))
                yield return new Claim("given_name", user.FirstName);
            if (!string.IsNullOrWhiteSpace(user.LastName))
                yield return new Claim("surname", user.LastName);

            // Özel (custom) claim’ler – veritabanı alanlarına 1:1
            if (user.RoleId.HasValue)
                yield return new Claim("role_id", user.RoleId.Value.ToString());

            if (user.BranchId.HasValue)
                yield return new Claim("branch_id", user.BranchId.Value.ToString());

            yield return new Claim("is_active", user.IsActive ?? false ? "true" : "false");
        }
    }

    public sealed class JwtOptions
    {
        [Required, MinLength(1)]
        public string Issuer { get; init; } = default!;

        [Required, MinLength(1)]
        public string Audience { get; init; } = default!;

        // HS256 için en az 32 byte önerilir
        [Required, MinLength(32, ErrorMessage = "Jwt Key en az 32 karakter olmalıdır.")]
        public string Key { get; init; } = default!;

        [Range(5, 1440)]
        public int ExpiryMinutes { get; init; } = 60;

        // İsteğe bağlı: key rotation için header’a yazılır
        public string? KeyId { get; init; }
    }
}
