using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.ProductCategories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IProductCategoryService
    {
        Task<ApiResult<PagedResult<ProductCategoryDto>>> GetPagedAsync(
            ProductCategoryFilter filter,
            CancellationToken ct = default);
        Task<ApiResult<ProductCategoryDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<ProductCategoryDto>> CreateAsync(ProductCategoryCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> UpdateAsync(int id, ProductCategoryUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
        Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default);
    }
}
