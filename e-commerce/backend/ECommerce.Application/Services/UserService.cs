using ECommerce.Application.Common;
using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services;

public sealed class UserService(IUserRepository users) : IUserService
{
    public async Task<UserDto?> GetMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task<PagedResult<UserDto>> GetPagedForAdminAsync(AdminUserQueryParams query, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var page = Math.Max(1, query.Page);
        var (items, total) = await users.GetPagedAsync(page, pageSize, cancellationToken);
        return new PagedResult<UserDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static UserDto Map(User u) =>
        new(u.Id, u.Name, u.Email, u.Role.ToString(), u.CreatedAt, u.IsActive);
}
