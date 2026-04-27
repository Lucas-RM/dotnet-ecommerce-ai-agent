using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace ECommerce.AgentAPI.Infrastructure.Health;

public sealed class RedisConnectionHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connection;

    public RedisConnectionHealthCheck(IConnectionMultiplexer connection) =>
        _connection = connection;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _connection.GetDatabase().Ping();
            return Task.FromResult(HealthCheckResult.Healthy("Redis acessível."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Falha ao contatar Redis.", ex));
        }
    }
}
