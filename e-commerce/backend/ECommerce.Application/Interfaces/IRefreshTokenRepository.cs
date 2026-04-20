using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);

    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);

    Task RevokeAllActiveByUserAsync(Guid userId, CancellationToken cancellationToken);
}