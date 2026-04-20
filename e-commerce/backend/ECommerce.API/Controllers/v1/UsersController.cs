using Asp.Versioning;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet("api/v{version:apiVersion}/users/me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dto = await userService.GetMeAsync(userId, cancellationToken);
        return dto is null
            ? NotFound(ApiResponse<string>.Fail("Usuário não encontrado."))
            : Ok(ApiResponse<UserDto>.Ok(dto));
    }

    [HttpGet("api/v{version:apiVersion}/admin/users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ListAdmin([FromQuery] AdminUserQueryParams query, CancellationToken cancellationToken)
    {
        var page = await userService.GetPagedForAdminAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(page));
    }
}
