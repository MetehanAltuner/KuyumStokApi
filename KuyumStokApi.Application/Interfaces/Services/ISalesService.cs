using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Receipts;
using KuyumStokApi.Application.DTOs.Sales;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface ISalesService
    {
        Task<ApiResult<UnifiedReceiptResultDto>> CreateUnifiedAsync(UnifiedReceiptCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<PagedResult<SaleListDto>>> GetPagedAsync(SaleFilter filter, CancellationToken ct = default);

        Task<ApiResult<SaleLineDetailDto>> GetLineByIdAsync(int lineId, CancellationToken ct = default);
    }
}
