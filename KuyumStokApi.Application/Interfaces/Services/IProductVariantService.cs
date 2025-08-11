using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.ProductVariant.KuyumStokApi.Application.DTOs.ProductVariants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    /// <summary>Ürün varyantı servis sözleşmesi.</summary>
    public interface IProductVariantService
    {
        Task<ApiResult<PagedResult<ProductVariantDto>>> GetPagedAsync(ProductVariantFilter filter, CancellationToken ct = default);
        Task<ApiResult<ProductVariantDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<ProductVariantDto>> CreateAsync(ProductVariantCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> UpdateAsync(int id, ProductVariantUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
    }
}
