using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Stocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    /// <summary>Stok servis sözleşmesi.</summary>
    public interface IStocksService
    {
        Task<ApiResult<PagedResult<StockDto>>> GetPagedAsync(StockFilter filter, CancellationToken ct = default);
        Task<ApiResult<StockDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<StockDto>> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
        Task<ApiResult<StockDto>> CreateAsync(StockCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> UpdateAsync(int id, StockUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default);
    }
}
