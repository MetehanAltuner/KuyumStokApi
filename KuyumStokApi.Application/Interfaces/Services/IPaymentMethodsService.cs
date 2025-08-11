using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.PaymentMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IPaymentMethodsService
    {
        Task<ApiResult<PagedResult<PaymentMethodDto>>> GetPagedAsync(PaymentMethodFilter filter, CancellationToken ct = default);
        Task<ApiResult<PaymentMethodDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<PaymentMethodDto>> CreateAsync(PaymentMethodCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> UpdateAsync(int id, PaymentMethodUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
    }
}
