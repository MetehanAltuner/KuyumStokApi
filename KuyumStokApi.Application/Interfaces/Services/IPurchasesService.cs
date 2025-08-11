using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Purchase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IPurchasesService
    {
        Task<ApiResult<PurchaseResultDto>> CreateAsync(PurchaseCreateDto dto, CancellationToken ct = default);
    }
}
