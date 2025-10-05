using KuyumStokApi.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KuyumStokApi.Domain.Entities;
using KuyumStokApi.Application.Common;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
        Task<Users> RegisterAsync(RegisterDto dto);
        Task<bool> UserExistsAsync(string username);
        Task<ApiResult<PasswordCheckResultDto>> ValidatePasswordAsync(PasswordCheckRequestDto dto, CancellationToken ct = default);
        Task<ApiResult<RegisterValidationResultDto>> ValidateRegisterAsync(RegisterDto dto, CancellationToken ct = default);
    }
}
