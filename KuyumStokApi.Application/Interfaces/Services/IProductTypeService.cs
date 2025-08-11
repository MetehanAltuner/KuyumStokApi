using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.ProductType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IProductTypeService
    {
        Task<ApiResult<PagedResult<ProductTypeDto>>> GetPagedAsync(ProductTypeFilter filter, CancellationToken ct = default);
        Task<ApiResult<ProductTypeDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<ProductTypeDto>> CreateAsync(ProductTypeCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> UpdateAsync(int id, ProductTypeUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
    }
}
