using System;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface ITokenBlacklistService
    {
        Task InvalidateTokenAsync(string jti, DateTime expiresAt, CancellationToken ct = default);
        Task<bool> IsTokenInvalidatedAsync(string jti, CancellationToken ct = default);
    }
}

