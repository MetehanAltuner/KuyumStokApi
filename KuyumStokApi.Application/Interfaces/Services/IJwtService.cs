using KuyumStokApi.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KuyumStokApi.Domain.Entities;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IJwtService
    {
        AuthResponseDto GenerateToken(Users user);
    }
}
