using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    /// <summary>Mağaza servis sözleşmesi.</summary>
    public interface IStoresService
    {
        Task<ApiResult<PagedResult<StoreDto>>> GetPagedAsync(StoreFilter filter, CancellationToken ct = default);
        Task<ApiResult<StoreDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<StoreDto>> CreateAsync(StoreCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> UpdateAsync(int id, StoreUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> SetActiveAsync(int id, bool isActive, CancellationToken ct = default);
    }
}
