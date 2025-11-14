using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Auth;
using KuyumStokApi.Application.DTOs.Users;
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

        public async Task<ApiResult<PagedResult<UserDto>>> GetPagedAsync(UserFilter filter, CancellationToken ct = default)
        {
            var page = Math.Max(1, filter.Page);
            var pageSize = Math.Clamp(filter.PageSize, 1, 200);

            IQueryable<Users> query = _db.Users.AsNoTracking();

            if (filter.IncludeDeleted)
                query = query.IgnoreQueryFilters();

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var qstr = filter.Query.Trim();
                query = query.Where(u =>
                    EF.Functions.ILike(u.Username, $"%{qstr}%")
                    || EF.Functions.ILike(u.FirstName ?? string.Empty, $"%{qstr}%")
                    || EF.Functions.ILike(u.LastName ?? string.Empty, $"%{qstr}%"));
            }

            if (filter.RoleId.HasValue)
                query = query.Where(u => u.RoleId == filter.RoleId);

            if (filter.BranchId.HasValue)
                query = query.Where(u => u.BranchId == filter.BranchId);

            if (filter.IsActive.HasValue)
                query = query.Where(u => (u.IsActive ?? false) == filter.IsActive.Value);

            if (filter.UpdatedFromUtc is not null)
                query = query.Where(u => u.UpdatedAt == null || u.UpdatedAt >= filter.UpdatedFromUtc);

            if (filter.UpdatedToUtc is not null)
                query = query.Where(u => u.UpdatedAt == null || u.UpdatedAt <= filter.UpdatedToUtc);

            var total = await query.LongCountAsync(ct);

            query = query
                .OrderByDescending(u => u.UpdatedAt ?? u.CreatedAt ?? DateTime.MinValue)
                .ThenBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var items = await ProjectToDto(query).ToListAsync(ct);

            var paged = new PagedResult<UserDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };

            return ApiResult<PagedResult<UserDto>>.Ok(paged, "Kullanıcılar listelendi.", 200);
        }

        public async Task<ApiResult<UserDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var user = await ProjectToDto(_db.Users.AsNoTracking()
                                                   .IgnoreQueryFilters()
                                                   .Where(u => u.Id == id))
                                 .FirstOrDefaultAsync(ct);

            if (user is null)
                return ApiResult<UserDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

            return ApiResult<UserDto>.Ok(user, "Kullanıcı bulundu.", 200);
        }

        public async Task<ApiResult<UserDto>> UpdateAsync(int id, UserUpdateDto dto, CancellationToken ct = default)
        {
            var entity = await _db.Users.IgnoreQueryFilters()
                                        .FirstOrDefaultAsync(u => u.Id == id, ct);
            if (entity is null)
                return ApiResult<UserDto>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

            if (dto.RoleId.HasValue && !await _db.Roles.AnyAsync(r => r.Id == dto.RoleId.Value, ct))
                return ApiResult<UserDto>.Fail("Geçersiz rol.", statusCode: 400);

            if (dto.BranchId.HasValue && !await _db.Branches.AnyAsync(b => b.Id == dto.BranchId.Value, ct))
                return ApiResult<UserDto>.Fail("Geçersiz şube.", statusCode: 400);

            var newFirst = dto.FirstName?.Trim();
            var newLast = dto.LastName?.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                PasswordPolicy.EnsureStrong(dto.Password, entity.Username, newFirst ?? entity.FirstName, newLast ?? entity.LastName);
                var salt = _hasher.GenerateSalt();
                entity.PasswordSalt = salt;
                entity.PasswordHash = _hasher.Hash(dto.Password, salt);
            }

            entity.FirstName = newFirst;
            entity.LastName = newLast;
            entity.RoleId = dto.RoleId;
            entity.BranchId = dto.BranchId;
            if (dto.IsActive.HasValue)
                entity.IsActive = dto.IsActive.Value;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            var updated = await ProjectToDto(_db.Users.AsNoTracking()
                                                      .IgnoreQueryFilters()
                                                      .Where(u => u.Id == entity.Id))
                                      .FirstAsync(ct);

            return ApiResult<UserDto>.Ok(updated, "Kullanıcı güncellendi.", 200);
        }

        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (entity is null)
                return ApiResult<bool>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

            _db.Users.Remove(entity);
            await _db.SaveChangesAsync(ct);

            return ApiResult<bool>.Ok(true, "Kullanıcı silindi (soft).", 200);
        }

        public async Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default)
        {
            var affected = await _db.Users.IgnoreQueryFilters()
                                          .Where(u => u.Id == id)
                                          .ExecuteDeleteAsync(ct);
            if (affected == 0)
                return ApiResult<bool>.Fail("Kullanıcı bulunamadı.", statusCode: 404);

            return ApiResult<bool>.Ok(true, "Kullanıcı kalıcı olarak silindi.", 200);
        }

        // ---- helpers ----
        private static string NormalizeUsername(string username)
            => (username ?? string.Empty).Trim().ToLowerInvariant();

        private static IQueryable<UserDto> ProjectToDto(IQueryable<Users> query)
            => query.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive ?? false,
                IsDeleted = u.IsDeleted,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                Role = u.Role == null
                    ? null
                    : new UserDto.RoleBrief
                    {
                        Id = u.Role.Id,
                        Name = u.Role.Name
                    },
                Branch = u.Branch == null
                    ? null
                    : new UserDto.BranchBrief
                    {
                        Id = u.Branch.Id,
                        Name = u.Branch.Name,
                        StoreId = u.Branch.StoreId,
                        StoreName = u.Branch.Store != null ? u.Branch.Store.Name : null
                    }
            });
    }
}
