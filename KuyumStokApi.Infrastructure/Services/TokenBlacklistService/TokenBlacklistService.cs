using KuyumStokApi.Application.Interfaces.Services;
using KuyumStokApi.Domain.Entities;
using KuyumStokApi.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KuyumStokApi.Infrastructure.Services.TokenBlacklistService
{
    public sealed class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly AppDbContext _db;

        public TokenBlacklistService(AppDbContext db)
        {
            _db = db;
        }

        public async Task InvalidateTokenAsync(string jti, DateTime expiresAt, CancellationToken ct = default)
        {
            // Check if already invalidated
            var existing = await _db.InvalidatedTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(it => it.Jti == jti, ct);

            if (existing != null)
                return; // Already invalidated

            var invalidatedToken = new InvalidatedTokens
            {
                Jti = jti,
                ExpiresAt = expiresAt,
                InvalidatedAt = DateTime.UtcNow
            };

            _db.InvalidatedTokens.Add(invalidatedToken);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> IsTokenInvalidatedAsync(string jti, CancellationToken ct = default)
        {
            var invalidated = await _db.InvalidatedTokens
                .AsNoTracking()
                .AnyAsync(it => it.Jti == jti, ct);

            return invalidated;
        }
    }
}

