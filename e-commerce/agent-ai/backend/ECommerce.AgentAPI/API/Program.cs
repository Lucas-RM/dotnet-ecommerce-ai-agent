using ECommerce.AgentAPI.API.Config;
using ECommerce.AgentAPI.API.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAgentApi(builder.Configuration);

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/", () => Results.Ok());
ChatEndpoint.Map(app, AgentApiDependencyInjection.ChatRateLimitPolicy);

app.Run();
