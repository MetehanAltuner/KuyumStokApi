using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.ProductCategories;
using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Persistence.Contexts;
using KuyumStokApi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace KuyumStokApi.Infrastructure.Services.ProductCategoryService
{
    public sealed class ProductCategoryService : IProductCategoryService
    {
        private readonly AppDbContext _db;
        public ProductCategoryService(AppDbContext db) => _db = db;

        /// <summary>
        /// Tüm kategori kayıtlarını listeler.
        /// </summary>
        /// <returns>Ürün kategorisi listesi</returns>
        public async Task<ApiResult<List<ProductCategoryDto>>> GetAllAsync()
        {
            var list = await _db.ProductCategories.AsNoTracking()
                .Select(x => new ProductCategoryDto
                {
                    Id = x.Id,
                    Name = x.Name!,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync();

            return ApiResult<List<ProductCategoryDto>>.Ok(list, "Liste getirildi", 200);
        }

        /// <summary>
        /// ID'ye göre kategori bilgilerini getirir.
        /// </summary>
        /// <param name="id">Kategori ID</param>
        public async Task<ApiResult<ProductCategoryDto>> GetByIdAsync(int id)
        {
            var x = await _db.ProductCategories.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (x is null)
                return ApiResult<ProductCategoryDto>.Fail("Kategori bulunamadı", statusCode: 404);

            var dto = new ProductCategoryDto
            {
                Id = x.Id,
                Name = x.Name!,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            };
            return ApiResult<ProductCategoryDto>.Ok(dto, "Bulundu", 200);
        }
        /// <summary>
        /// Yeni bir ürün kategorisi oluşturur.
        /// </summary>
        /// <remarks>
        /// İstek gövdesinde <see cref="ProductCategoryCreateDto"/> tipinde veriler gönderilmelidir.
        /// </remarks>
        /// <param name="dto">Oluşturulacak kategori bilgileri</param>
        /// <returns>Oluşturulan kategori bilgileri</returns>
        public async Task<ApiResult<ProductCategoryDto>> CreateAsync(ProductCategoryCreateDto dto)
        {
            // (İstersen unique kontrolü ekleyebilirsin)
            var now = DateTime.UtcNow;
            var entity = new KuyumStokApi.Domain.Entities.ProductCategories
            {
                Name = dto.Name,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.ProductCategories.Add(entity);
            await _db.SaveChangesAsync();

            var created = new ProductCategoryDto
            {
                Id = entity.Id,
                Name = entity.Name!,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
            return ApiResult<ProductCategoryDto>.Ok(created, "Oluşturuldu", 201);
        }
        /// <summary>
        /// Var olan bir ürün kategorisini günceller.
        /// </summary>
        /// <remarks>
        /// İstek gövdesinde <see cref="ProductCategoryUpdateDto"/> tipinde veriler gönderilmelidir.
        /// </remarks>
        /// <param name="id">Güncellenecek kategori ID'si</param>
        /// <param name="dto">Güncellenecek kategori bilgileri</param>
        /// <returns>Güncellenmiş kategori bilgileri</returns>
        public async Task<ApiResult<bool>> UpdateAsync(int id, ProductCategoryUpdateDto dto)
        {
            var entity = await _db.ProductCategories.FirstOrDefaultAsync(p => p.Id == id);
            if (entity is null)
                return ApiResult<bool>.Fail("Kategori bulunamadı", statusCode: 404);

            entity.Name = dto.Name;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return ApiResult<bool>.Ok(true, "Güncellendi", 200);
        }
        /// <summary>
        /// Belirtilen ID’ye sahip ürün kategorisini siler.
        /// </summary>
        /// <param name="id">Silinecek kategori ID'si</param>
        /// <returns>Silme işleminin sonucu</returns>
        public async Task<ApiResult<bool>> DeleteAsync(int id)
        {
            var entity = await _db.ProductCategories.FirstOrDefaultAsync(p => p.Id == id);
            if (entity is null)
                return ApiResult<bool>.Fail("Kategori bulunamadı", statusCode: 404);

            _db.ProductCategories.Remove(entity);
            await _db.SaveChangesAsync();
            return ApiResult<bool>.Ok(true, "Silindi", 200);
        }
    }
}
