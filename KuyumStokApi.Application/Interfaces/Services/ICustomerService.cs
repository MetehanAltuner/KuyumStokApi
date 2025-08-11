using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface ICustomersService
    {
        Task<ApiResult<PagedResult<CustomerDto>>> GetPagedAsync(CustomerFilter filter, CancellationToken ct = default);
        Task<ApiResult<CustomerDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<CustomerDto>> CreateAsync(CustomerCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> UpdateAsync(int id, CustomerUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
    }
}
