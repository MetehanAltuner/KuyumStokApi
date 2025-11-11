using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.ThermalPrinters;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IThermalPrintersService
    {
        Task<ApiResult<PagedResult<ThermalPrinterDto>>> GetPagedAsync(ThermalPrinterFilter filter, CancellationToken ct = default);
        Task<ApiResult<ThermalPrinterDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<ThermalPrinterDto>> CreateAsync(ThermalPrinterCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> UpdateAsync(int id, ThermalPrinterUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default);
    }
}


