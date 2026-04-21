using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using ECommerce.AgentAPI.Approval;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Endpoints;
using ECommerce.AgentAPI.Kernel;
using ECommerce.AgentAPI.Middleware;
using ECommerce.AgentAPI.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

var builder = WebApplication.CreateBuilder(args);

// --- Domínio: Approval, orquestração, memória multi-turn, Kernel (Semantic Kernel montado em KernelFactory) ---
builder.Services.AddSingleton<ApprovalStateStore>();
builder.Services.AddSingleton<ToolApprovalService>();
builder.Services.AddSingleton<KernelFactory>();
builder.Services.AddSingleton<AgentMemoryStore>();
builder.Services.AddScoped<AgentOrchestratorMiddleware>();

// --- Refit (IECommerceApi) + Polly (retry + circuit breaker) + handler JWT outbound ---
builder.Services.AddECommerceApi(builder.Configuration);

// --- FluentValidation ---
builder.Services.AddValidatorsFromAssemblyContaining<ChatRequestValidator>();

// --- JWT Bearer (Issuer, Audience, SecretKey em appsettings) ---
var jwtSecret = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Configuração 'Jwt:SecretKey' ausente.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Configuração 'Jwt:Issuer' ausente.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Configuração 'Jwt:Audience' ausente.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });
builder.Services.AddAuthorization();

// --- CORS: somente origens declaradas (padrão: http://localhost:4200) ---
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

// --- Rate limiting: 30 requisições/minuto por usuário (claim `sub` do JWT) ---
const string ChatRateLimitPolicy = "ChatPerJwtSub";
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(ChatRateLimitPolicy, httpContext =>
    {
        var sub = JwtSubResolver.Resolve(httpContext.User);
        if (string.IsNullOrEmpty(sub))
        {
            return RateLimitPartition.Get(
                "no-sub",
                _ => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 0,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        }

        return RateLimitPartition.Get(
            sub,
            _ => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    });
});

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/", () => Results.Ok());
ChatEndpoint.Map(app, ChatRateLimitPolicy);

app.Run();

/// <summary>Extrai o <c>sub</c> do JWT (ou o mapeamento padrão para NameIdentifier).</summary>
internal static class JwtSubResolver
{
    public static string? Resolve(ClaimsPrincipal user)
    {
        return user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
