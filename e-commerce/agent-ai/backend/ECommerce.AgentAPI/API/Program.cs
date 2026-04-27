using ECommerce.AgentAPI.API.Config;
using ECommerce.AgentAPI.API.Endpoints;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

var shutdownSec = builder.Configuration.GetValue("Agent:Hosting:ShutdownTimeoutSeconds", 30);
builder.Host.ConfigureHostOptions(o => o.ShutdownTimeout = TimeSpan.FromSeconds(shutdownSec));

if (builder.Configuration.GetValue("Agent:Hosting:UseForwardedHeaders", false))
{
    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });
}

builder.WebHost.ConfigureKestrel((context, o) =>
{
    var c = context.Configuration;
    var maxBytes = c.GetValue<long?>("Kestrel:Limits:MaxRequestBodySize");
    if (maxBytes is > 0L)
    {
        o.Limits.MaxRequestBodySize = maxBytes;
    }

    var maxConn = c.GetValue<int?>("Agent:Hosting:KestrelMaxConcurrentConnections");
    if (maxConn is > 0)
    {
        o.Limits.MaxConcurrentConnections = maxConn;
    }
});

builder.Services.AddAgentApi(builder.Configuration);
builder.Services.AddAgentOpenTelemetry(builder.Configuration, builder.Environment);

var app = builder.Build();

if (app.Configuration.GetValue("Agent:Hosting:UseForwardedHeaders", false))
{
    app.UseForwardedHeaders();
}

app.MapHealthChecks("/health");
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = r => r.Tags is not null
            && r.Tags.Any(t => t.Equals("live", StringComparison.OrdinalIgnoreCase))
    });
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = r => r.Tags is not null
            && r.Tags.Any(t => t.Equals("ready", StringComparison.OrdinalIgnoreCase))
    });

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestTimeouts();
app.UseRateLimiter();

app.MapGet("/", () => Results.Ok());
ChatEndpoint.Map(app, AgentApiDependencyInjection.ChatRateLimitPolicy);

app.Run();
