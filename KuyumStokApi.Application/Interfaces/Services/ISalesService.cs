using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Sales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface ISalesService
    {
        Task<ApiResult<SaleResultDto>> CreateAsync(SaleCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<PagedResult<SaleListDto>>> GetPagedAsync(SaleFilter filter, CancellationToken ct = default);

        Task<ApiResult<SaleLineDetailDto>> GetLineByIdAsync(int lineId, CancellationToken ct = default);
    }
}
