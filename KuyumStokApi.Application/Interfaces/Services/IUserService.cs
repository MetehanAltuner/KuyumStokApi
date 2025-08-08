using KuyumStokApi.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KuyumStokApi.Domain.Entities;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<AuthResponseDto?> LoginAsync(LoginDto dto);
        Task<Users> RegisterAsync(RegisterDto dto);
        Task<bool> UserExistsAsync(string username);
    }
}
