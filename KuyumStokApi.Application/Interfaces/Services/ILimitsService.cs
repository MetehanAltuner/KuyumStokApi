using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Limits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface ILimitsService
    {
        Task<ApiResult<PagedResult<LimitDto>>> GetPagedAsync(LimitFilter filter, CancellationToken ct = default);
        Task<ApiResult<LimitDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<LimitDto>> CreateAsync(LimitCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> UpdateAsync(int id, LimitUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
    }
}
