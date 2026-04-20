using ECommerce.Application.Common;
using ECommerce.Application.DTOs;

namespace ECommerce.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetMeAsync(Guid userId, CancellationToken cancellationToken);

    Task<PagedResult<UserDto>> GetPagedForAdminAsync(AdminUserQueryParams query, CancellationToken cancellationToken);
}
