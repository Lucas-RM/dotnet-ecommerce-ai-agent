using Asp.Versioning;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace ECommerce.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(dto, cancellationToken);
        return Created(string.Empty, ApiResponse<UserDto>.Ok(result));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        var (response, refreshToken) = await authService.LoginAsync(dto, cancellationToken);
        WriteRefreshCookie(refreshToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(response));
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
        {
            return Unauthorized(ApiResponse<string>.Fail("Refresh token ausente."));
        }

        var (response, nextRefreshToken) = await authService.RefreshAsync(refreshToken!, cancellationToken);
        WriteRefreshCookie(nextRefreshToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(response));
    }

    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await authService.RevokeAsync(userId, cancellationToken);
        DeleteRefreshCookie();
        return NoContent();
    }

    private void WriteRefreshCookie(string token)
    {
        Response.Cookies.Append("refreshToken", token, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/api/v1/auth/refresh", MaxAge = TimeSpan.FromDays(7) });
    }

    private void DeleteRefreshCookie()
    {
        Response.Cookies.Delete("refreshToken", new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/api/v1/auth/refresh" });
    }
}
