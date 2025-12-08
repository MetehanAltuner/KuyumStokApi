using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Domain.Entities;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.RefreshTokenService
{
    public sealed class RefreshTokenService : IRefreshTokenService
    {
        private readonly AppDbContext _db;

        public RefreshTokenService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<(string Token, DateTime ExpiresAt)> GenerateRefreshTokenAsync(int userId, CancellationToken ct = default)
        {
            // Generate random token (32 bytes -> Base64) - NOT a JWT, just a random string
            var tokenBytes = new byte[32];
            RandomNumberGenerator.Fill(tokenBytes);
            var token = Convert.ToBase64String(tokenBytes);

            // Expires in 8 hours (best practice: shorter than access token but allows session persistence)
            var expiresAt = DateTime.UtcNow.AddHours(8);

            var refreshToken = new RefreshTokens
            {
                UserId = userId,
                Token = token,
                ExpiresAt = expiresAt,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync(ct);

            return (token, expiresAt);
        }

        public async Task<Domain.Entities.Users?> GetUserByRefreshTokenAsync(string token, CancellationToken ct = default)
        {
            var refreshToken = await _db.RefreshTokens
                .AsNoTracking()
                .Include(rt => rt.User)
                    .ThenInclude(u => u.Role)
                .Include(rt => rt.User)
                    .ThenInclude(u => u.Branch)
                .FirstOrDefaultAsync(rt => rt.Token == token, ct);

            if (refreshToken == null)
                return null;

            // Check if expired
            if (refreshToken.ExpiresAt < DateTime.UtcNow)
                return null;

            // Check if revoked
            if (refreshToken.IsRevoked)
                return null;

            // Check if user is active
            if (refreshToken.User == null || !(refreshToken.User.IsActive ?? false))
                return null;

            return refreshToken.User;
        }

        public async Task RevokeRefreshTokenAsync(string token, CancellationToken ct = default)
        {
            var refreshToken = await _db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token, ct);

            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }

        public async Task RevokeAllUserTokensAsync(int userId, CancellationToken ct = default)
        {
            var refreshTokens = await _db.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync(ct);

            var now = DateTime.UtcNow;
            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = now;
            }

            if (refreshTokens.Any())
            {
                await _db.SaveChangesAsync(ct);
            }
        }
    }
}

