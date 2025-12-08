using KuyumStokApi.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Application.Interfaces.Services
{
    public interface IRefreshTokenService
    {
        Task<(string Token, DateTime ExpiresAt)> GenerateRefreshTokenAsync(int userId, CancellationToken ct = default);
        Task<Users?> GetUserByRefreshTokenAsync(string token, CancellationToken ct = default);
        Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default);
        Task RevokeAllUserTokensAsync(int userId, CancellationToken ct = default);
    }
}

