using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.LifeCycleActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface ILifecycleActionsService
    {
        Task<ApiResult<PagedResult<LifecycleActionDto>>> GetPagedAsync(LifecycleActionFilter filter, CancellationToken ct = default);
        Task<ApiResult<LifecycleActionDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<LifecycleActionDto>> CreateAsync(LifecycleActionCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> UpdateAsync(int id, LifecycleActionUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
    }
}
