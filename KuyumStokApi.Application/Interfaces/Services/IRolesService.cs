using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Roles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IRolesService
    {
        Task<ApiResult<List<RoleDto>>> GetAllAsync(CancellationToken ct = default);
        Task<ApiResult<RoleDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<RoleDto>> CreateAsync(RoleCreateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> UpdateAsync(int id, RoleUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
    }
}
