using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Roles;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.RolesService
{
    /// <summary>
    /// Rol CRUD işlemleri. Basit referans verisi olduğundan sadece ad alanı yönetilir.
    /// </summary>
    public sealed class RolesService : IRolesService
    {
        private readonly AppDbContext _db;
        public RolesService(AppDbContext db) => _db = db;

        /// <summary>Tüm rol kayıtlarını listeler.</summary>
        public async Task<ApiResult<List<RoleDto>>> GetAllAsync(CancellationToken ct = default)
        {
            var list = await _db.Roles.AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new RoleDto
                {
                    Id = x.Id,
                    Name = x.Name!,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync(ct);

            return ApiResult<List<RoleDto>>.Ok(list, "Roller listelendi", 200);
        }

        /// <summary>Id’ye göre rol detayını getirir.</summary>
        public async Task<ApiResult<RoleDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var x = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
            if (x is null) return ApiResult<RoleDto>.Fail("Rol bulunamadı", statusCode: 404);

            var dto = new RoleDto
            {
                Id = x.Id,
                Name = x.Name!,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            };
            return ApiResult<RoleDto>.Ok(dto, "Bulundu", 200);
        }

        /// <summary>Yeni rol oluşturur.</summary>
        public async Task<ApiResult<RoleDto>> CreateAsync(RoleCreateDto dto, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var exists = await _db.Roles.AnyAsync(r => r.Name == dto.Name, ct);
            if (exists) return ApiResult<RoleDto>.Fail("Aynı isimde rol zaten var.", statusCode: 409);

            var entity = new KuyumStokApi.Domain.Entities.Roles
            {
                Name = dto.Name,
                CreatedAt = now,
                UpdatedAt = now,
                IsActive = true
                
            };

            _db.Roles.Add(entity);
            await _db.SaveChangesAsync(ct);

            return ApiResult<RoleDto>.Ok(new RoleDto
            {
                Id = entity.Id,
                Name = entity.Name!,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            }, "Oluşturuldu", 201);
        }

        /// <summary>Rol günceller.</summary>
        public async Task<ApiResult<bool>> UpdateAsync(int id, RoleUpdateDto dto, CancellationToken ct = default)
        {
            var entity = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (entity is null) return ApiResult<bool>.Fail("Rol bulunamadı", statusCode: 404);

            var nameTaken = await _db.Roles.AnyAsync(r => r.Id != id && r.Name == dto.Name, ct);
            if (nameTaken) return ApiResult<bool>.Fail("Bu isim başka bir rolde kullanılıyor.", statusCode: 409);

            entity.Name = dto.Name;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }

        /// <summary>Rol siler. Kullanıcılar bu rolle bağlıysa veritabanı FK kuralına göre engellenir ya da kademeli ele alınır.</summary>
        public async Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var entity = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (entity is null) return ApiResult<bool>.Fail("Rol bulunamadı", statusCode: 404);

            _db.Roles.Remove(entity);
            await _db.SaveChangesAsync(ct);
            return ApiResult<bool>.Ok(true, "Silindi", 200);
        }
    }
}
