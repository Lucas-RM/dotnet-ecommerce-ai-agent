using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using ECommerce.AgentAPI.Application.Abstractions;
using ECommerce.AgentAPI.Application.Agents;
using ECommerce.AgentAPI.Application.Options;
using ECommerce.AgentAPI.Application.UseCases;
using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.API.Middleware;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.ErrorHandling;
using ECommerce.AgentAPI.Infrastructure.LLM;
using ECommerce.AgentAPI.Infrastructure.LLM.Google;
using ECommerce.AgentAPI.Infrastructure.LLM.OpenAI;
using ECommerce.AgentAPI.Infrastructure.Memory;
using ECommerce.AgentAPI.Infrastructure.Tools;
using ECommerce.AgentAPI.Infrastructure.Tools.Plugins;
using ECommerce.AgentAPI.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace ECommerce.AgentAPI.API.Config;

/// <summary>Registos de DI (secção 7 — ecommerce-agent-evolution.yaml): Auth, LLM, plugins, aprovação, memória, Refit+Polly, orquestração.</summary>
public static class AgentApiDependencyInjection
{
    /// <summary>Política de rate limit usada em <c>POST /api/agent/chat</c> (30 req/min por <c>sub</c> do JWT).</summary>
    public const string ChatRateLimitPolicy = "ChatPerJwtSub";

    public static IServiceCollection AddAgentApi(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Aprovação, Kernel, plugins SK (instâncias também criadas em KernelFactory; registo alinha evolução / testes) ──
        services.AddSingleton<ApprovalStateStore>();
        services.AddSingleton<ToolApprovalService>();
        services.AddSingleton<KernelFactory>();
        services.AddSingleton<AgentMemoryStore>();
        services.AddScoped<ProductPlugin>();
        services.AddScoped<CartPlugin>();
        services.AddScoped<OrderPlugin>();

        // ── Refit + Polly + JWT no outbound (antes dos serviços que dependem de IECommerceApi) ──
        services.AddECommerceApi(configuration);

        // ── LLM Layer (Singleton — OpenAILLMService reutiliza um IECommerceApi/HttpClient) ──
        services.AddSingleton<OpenAILLMService>();
        services.AddSingleton<GoogleKernelFactory>();
        services.AddSingleton<GoogleLLMService>();
        services.AddSingleton<ILLMFactory, LLMFactory>();

        ValidateLLMProviderConfiguration(configuration);

        // ── Aprovação (domínio) — adaptador sobre ToolApprovalService persistente em memória ──
        services.AddScoped<IToolApprovalService, ToolApprovalServiceAdapter>();

        // ── Memória: volátil (padrão) com AgentMemoryStore ou Redis (multi-instância) ──
        var memProvider = configuration["Memory:Provider"]?.ToLowerInvariant() ?? "volatile";
        if (memProvider is "redis")
        {
            var conn = configuration["Memory:Redis:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Configuração 'Memory:Redis:ConnectionString' ausente para Memory:Provider=redis.");
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(conn));
            services.AddScoped<IMemoryService, RedisMemoryService>();
        }
        else
        {
            services.AddSingleton<IMemoryService, VolatileMemoryService>();
        }

        services.Configure<AgentOptions>(configuration.GetSection(AgentOptions.SectionName));
        services.AddSingleton<IChatErrorHandler, HttpChatErrorHandler>();
        services.AddScoped<IToolExecutor, ToolExecutorService>();
        services.AddScoped<ProcessUserMessageUseCase>();
        services.AddScoped<ChatAgent>();
        services.AddScoped<AgentOrchestratorMiddleware>();

        services.AddValidatorsFromAssemblyContaining<ChatRequestValidator>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(ConfigureJwtBearerOptions(configuration));
        services.AddAuthorization();

        var corsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];
        services.AddCors(o => o.AddDefaultPolicy(p =>
            p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

        services.AddRateLimiter(options =>
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

        return services;
    }

    /// <summary>
    /// Valida a configuração do provedor LLM no arranque da aplicação. Garante que,
    /// quando <c>LLM:Provider</c> aponta para um provedor que requer chave de API
    /// (OpenAI, Google), a respetiva <c>ApiKey</c> está definida — falhando rápido
    /// com uma <see cref="InvalidOperationException"/> descritiva.
    /// </summary>
    private static void ValidateLLMProviderConfiguration(IConfiguration configuration)
    {
        var providerRaw = configuration["LLM:Provider"] ?? "OpenAI";
        if (!Enum.TryParse<LLMProvider>(providerRaw, ignoreCase: true, out var provider))
        {
            return;
        }

        switch (provider)
        {
            case LLMProvider.OpenAI:
                {
                    var apiKey = configuration["LLM:OpenAI:ApiKey"] ?? configuration["OpenAI:ApiKey"];
                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        throw new InvalidOperationException(
                            "Configuração ausente: LLM:OpenAI:ApiKey (ou legado OpenAI:ApiKey) é obrigatória quando LLM:Provider=OpenAI.");
                    }
                    break;
                }

            case LLMProvider.Google:
                {
                    var apiKey = configuration["LLM:Google:ApiKey"];
                    if (string.IsNullOrWhiteSpace(apiKey))
                    {
                        throw new InvalidOperationException(
                            "Configuração ausente: LLM:Google:ApiKey é obrigatória quando LLM:Provider=Google.");
                    }
                    break;
                }
        }
    }

    private static Action<JwtBearerOptions> ConfigureJwtBearerOptions(IConfiguration configuration) => options =>
    {
        var jwtSecret = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Configuração 'Jwt:SecretKey' ausente.");
        var jwtIssuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Configuração 'Jwt:Issuer' ausente.");
        var jwtAudience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Configuração 'Jwt:Audience' ausente.");

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
    };

    private static class JwtSubResolver
    {
        public static string? Resolve(ClaimsPrincipal user) =>
            user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
