using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Auth;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Domain.Entities;
using KuyumStokApi.Infrastructure.Security;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.UserService
{
    public sealed class UserService : IUserService
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher _hasher;
        private readonly IJwtService _jwt; // Login'de token üreteceğiz

        public UserService(AppDbContext db, IPasswordHasher hasher, IJwtService jwt)
        {
            _db = db;
            _hasher = hasher;
            _jwt = jwt;
        }

        public async Task<bool> UserExistsAsync(string username)
        {
            var norm = NormalizeUsername(username);
            return await _db.Users.AnyAsync(u => u.Username.ToLower() == norm);
        }

        public async Task<Users> RegisterAsync(RegisterDto dto)
        {
            if (dto is null) throw new ArgumentNullException(nameof(dto));

            // --- Username doğrulama + normalize ---
            PasswordPolicy.EnsureValidUsername(dto.Username);
            var username = NormalizeUsername(dto.Username);

            var exists = await _db.Users.AnyAsync(u => u.Username.ToLower() == username);
            if (exists)
                throw new InvalidOperationException("Kullanıcı adı zaten mevcut.");

            // --- Rol / Şube var mı? (varsalar kontrol et) ---
            if (dto.RoleId.HasValue)
            {
                var roleOk = await _db.Roles.AnyAsync(r => r.Id == dto.RoleId.Value);
                if (!roleOk) throw new InvalidOperationException("Geçersiz rol.");
            }

            if (dto.BranchId.HasValue)
            {
                var branchOk = await _db.Branches.AnyAsync(b => b.Id == dto.BranchId.Value);
                if (!branchOk) throw new InvalidOperationException("Geçersiz şube.");
            }

            // --- Parola politikası ---
            PasswordPolicy.EnsureStrong(dto.Password, username, dto.FirstName, dto.LastName);

            // --- Hash & persist ---
            var salt = _hasher.GenerateSalt();
            var hash = _hasher.Hash(dto.Password, salt);

            var now = DateTime.UtcNow;
            var user = new Users
            {
                Username = username,  // normalize edilmiş
                PasswordHash = hash,
                PasswordSalt = salt,
                FirstName = dto.FirstName?.Trim(),
                LastName = dto.LastName?.Trim(),
                RoleId = dto.RoleId,
                BranchId = dto.BranchId,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            if (dto is null) return null;
            var username = NormalizeUsername(dto.Username);

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username);

            if (user is null) return null;
            if (!(user.IsActive ?? false)) return null;

            var ok = _hasher.Verify(dto.Password, user.PasswordSalt, user.PasswordHash);
            if (!ok) return null;

            return _jwt.GenerateToken(user);
        }

        public Task<ApiResult<PasswordCheckResultDto>> ValidatePasswordAsync(PasswordCheckRequestDto dto, CancellationToken ct = default)
        {
            var res = PasswordPolicy.Validate(dto.Password, dto.Username, dto.FirstName, dto.LastName);

            var payload = new PasswordCheckResultDto
            {
                IsValid = res.IsValid,
                Score = res.Score,
                Errors = res.Errors
            };

            // Geçerli değilse 400 döndürüyoruz (ApiResult ile)
            return Task.FromResult(
                ApiResult<PasswordCheckResultDto>.Ok(payload, res.IsValid ? "OK" : "Parola geçersiz.", res.IsValid ? 200 : 400)
            );
        }

        public async Task<ApiResult<RegisterValidationResultDto>> ValidateRegisterAsync(RegisterDto dto, CancellationToken ct = default)
        {
            var errors = PasswordPolicy.ValidateUsername(dto.Username);

            var username = NormalizeUsername(dto.Username);

            // benzersizlik
            if (await _db.Users.AnyAsync(u => u.Username.ToLower() == username, ct))
                errors.Add("Kullanıcı adı zaten mevcut.");

            // rol/şube mevcudiyeti (gönderildiyse)
            if (dto.RoleId.HasValue && !await _db.Roles.AnyAsync(r => r.Id == dto.RoleId.Value, ct))
                errors.Add("Geçersiz rol.");

            if (dto.BranchId.HasValue && !await _db.Branches.AnyAsync(b => b.Id == dto.BranchId.Value, ct))
                errors.Add("Geçersiz şube.");

            // şifre kuralları
            var passRes = PasswordPolicy.Validate(dto.Password, username, dto.FirstName, dto.LastName);
            if (!passRes.IsValid) errors.AddRange(passRes.Errors);

            var payload = new RegisterValidationResultDto
            {
                IsValid = errors.Count == 0,
                PasswordScore = passRes.Score,
                Errors = errors
            };

            return ApiResult<RegisterValidationResultDto>.Ok(
                payload,
                payload.IsValid ? "OK" : "Doğrulama hataları mevcut.",
                payload.IsValid ? 200 : 400
            );
        }

        // ---- helpers ----
        private static string NormalizeUsername(string username)
            => (username ?? string.Empty).Trim().ToLowerInvariant();
    }
}
