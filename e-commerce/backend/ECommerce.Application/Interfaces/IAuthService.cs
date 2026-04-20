using ECommerce.Application.DTOs;

namespace ECommerce.Application.Interfaces;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken);

    Task<(AuthResponseDto Response, string RefreshToken)> LoginAsync(LoginDto dto, CancellationToken cancellationToken);

    Task<(AuthResponseDto Response, string RefreshToken)> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeAsync(Guid userId, CancellationToken cancellationToken);
}
