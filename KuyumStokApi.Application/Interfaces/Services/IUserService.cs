using KuyumStokApi.Application.DTOs.Auth;
using KuyumStokApi.Domain.Entities;
using KuyumStokApi.Application.Common;
using KuyumStokApi.Application.DTOs.Users;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
        Task<Users> RegisterAsync(RegisterDto dto);
        Task<bool> UserExistsAsync(string username);
        Task<ApiResult<PasswordCheckResultDto>> ValidatePasswordAsync(PasswordCheckRequestDto dto, CancellationToken ct = default);
        Task<ApiResult<RegisterValidationResultDto>> ValidateRegisterAsync(RegisterDto dto, CancellationToken ct = default);
        Task<ApiResult<PagedResult<UserDto>>> GetPagedAsync(UserFilter filter, CancellationToken ct = default);
        Task<ApiResult<UserDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResult<UserDto>> UpdateAsync(int id, UserUpdateDto dto, CancellationToken ct = default);
        Task<ApiResult<bool>> DeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> HardDeleteAsync(int id, CancellationToken ct = default);
        Task<ApiResult<bool>> SetActiveAsync(int id, bool value, CancellationToken ct = default);
    }
}
