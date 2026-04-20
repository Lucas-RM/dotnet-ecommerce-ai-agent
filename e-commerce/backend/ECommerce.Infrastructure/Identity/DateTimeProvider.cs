using ECommerce.Application.Interfaces;

namespace ECommerce.Infrastructure.Identity;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
