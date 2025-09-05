using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.ProductLifecycles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IProductLifecyclesService
    {
        Task<ApiResult<PagedResult<ProductLifecycleDto>>> GetPagedAsync(ProductLifecycleFilter filter, CancellationToken ct = default);
        Task<ApiResult<ProductLifecycleDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<ProductLifecycleDto>> CreateAsync(ProductLifecycleCreateDto dto, CancellationToken ct = default);
    }
}
