using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken) =>
        db.RefreshTokens.Include(x => x.User).FirstOrDefaultAsync(x => x.Token == token, cancellationToken);

    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken) =>
        db.RefreshTokens.AddAsync(token, cancellationToken).AsTask();

    public async Task RevokeAllActiveByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tokens = await db.RefreshTokens
            .Where(x => x.UserId == userId && !x.IsUsed && !x.IsRevoked && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.IsUsed = true; token.IsRevoked = true;
        }
    }
}
