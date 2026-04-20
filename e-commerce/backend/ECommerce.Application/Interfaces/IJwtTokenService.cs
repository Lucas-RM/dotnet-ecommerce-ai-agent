namespace ECommerce.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string role);

    int ExpiresInSeconds { get; }
}
