using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Banks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IBanksService
    {
        Task<ApiResult<PagedResult<BankDto>>> GetPagedAsync(BankFilter filter, CancellationToken ct = default);
        Task<ApiResult<BankDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<BankDto>> CreateAsync(BankCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> UpdateAsync(int id, BankUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
    }
}
