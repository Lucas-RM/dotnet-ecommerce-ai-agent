namespace ECommerce.Application.DTOs;
public sealed record RegisterDto(string Name, string Email, string Password, string ConfirmPassword);
public sealed record LoginDto(string Email, string Password);
public sealed record AuthResponseDto(string AccessToken, int ExpiresIn, string Role);
public sealed record UserDto(Guid Id, string Name, string Email, string Role, DateTime CreatedAt, bool IsActive);
