using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

public sealed class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IUnitOfWork uow,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    IDateTimeProvider clock,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<UserDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken)
    {
        if (dto.Password != dto.ConfirmPassword)
        {
            throw new DomainException("Senha e confirmação não conferem.");
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new ConflictDomainException("Email já cadastrado.");
        }

        var user = new User { Name = dto.Name.Trim(), Email = email, PasswordHash = hasher.Hash(dto.Password) };
        await users.AddAsync(user, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Usuário registrado: {Email}", email);
        return new UserDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt, user.IsActive);
    }

    public async Task<(AuthResponseDto Response, string RefreshToken)> LoginAsync(LoginDto dto, CancellationToken cancellationToken)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await users.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("Login falhou (usuário inexistente): {Email}", dto.Email);
            throw new UnauthorizedDomainException("Credenciais inválidas.");
        }

        if (!hasher.Verify(dto.Password, user.PasswordHash) || !user.IsActive)
        {
            logger.LogWarning("Login falhou (senha inválida ou conta inativa): {Email}", dto.Email);
            throw new UnauthorizedDomainException("Credenciais inválidas.");
        }

        var rt = NewRefreshToken(user.Id);
        await refreshTokens.AddAsync(rt, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Login bem-sucedido e refresh emitido para usuário {UserId}", user.Id);
        return (new AuthResponseDto(jwt.GenerateAccessToken(user.Id, user.Email, user.Role.ToString()), jwt.ExpiresInSeconds, user.Role.ToString()), rt.Token);
    }

    public async Task<(AuthResponseDto Response, string RefreshToken)> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var current = await refreshTokens.GetByTokenAsync(refreshToken, cancellationToken);
        if (current is null)
        {
            logger.LogWarning("Refresh falhou: token não encontrado");
            throw new UnauthorizedDomainException("Refresh token inválido.");
        }

        if (current.IsUsed || current.IsRevoked || current.ExpiresAt <= clock.UtcNow || !current.User.IsActive)
        {
            logger.LogWarning("Refresh falhou: token revogado, usado, expirado ou usuário inativo (UserId={UserId})", current.UserId);
            throw new UnauthorizedDomainException("Refresh token inválido.");
        }

        current.IsUsed = true;
        current.IsRevoked = true;
        var next = NewRefreshToken(current.UserId);
        await refreshTokens.AddAsync(next, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Refresh rotacionado para usuário {UserId}", current.UserId);
        return (new AuthResponseDto(jwt.GenerateAccessToken(current.UserId, current.User.Email, current.User.Role.ToString()), jwt.ExpiresInSeconds, current.User.Role.ToString()), next.Token);
    }

    public async Task RevokeAsync(Guid userId, CancellationToken cancellationToken)
    {
        await refreshTokens.RevokeAllActiveByUserAsync(userId, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Refresh tokens revogados para usuário {UserId}", userId);
    }

    private RefreshToken NewRefreshToken(Guid userId) => new()
    {
        UserId = userId,
        Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
        ExpiresAt = clock.UtcNow.AddDays(7)
    };
}
