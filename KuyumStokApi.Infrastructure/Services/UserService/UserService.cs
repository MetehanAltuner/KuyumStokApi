using KuyumStokApi.Application.DTOs.Auth;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Domain.Entities;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            return await _db.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<Users> RegisterAsync(RegisterDto dto)
        {

            if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
                throw new InvalidOperationException("Kullanıcı adı zaten mevcut.");

            var salt = _hasher.GenerateSalt();
            var hash = _hasher.Hash(dto.Password, salt);

            var now = DateTime.UtcNow;
            var user = new Users
            {
                Username = dto.Username,
                PasswordHash = hash,
                PasswordSalt = salt,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
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
            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (user is null) return null;
            if (!user.IsActive) return null;

            var ok = _hasher.Verify(dto.Password, user.PasswordSalt, user.PasswordHash);
            if (!ok) return null;

            return _jwt.GenerateToken(user);
        }
    }
}
