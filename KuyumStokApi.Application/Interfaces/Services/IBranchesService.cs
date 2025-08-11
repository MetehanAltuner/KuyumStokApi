using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Branches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    /// <summary>Şube servis sözleşmesi.</summary>
    public interface IBranchesService
    {
        Task<ApiResult<PagedResult<BranchDto>>> GetPagedAsync(BranchFilter filter, CancellationToken ct = default);
        Task<ApiResult<BranchDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<BranchDto>> CreateAsync(BranchCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> UpdateAsync(int id, BranchUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
    }
}
