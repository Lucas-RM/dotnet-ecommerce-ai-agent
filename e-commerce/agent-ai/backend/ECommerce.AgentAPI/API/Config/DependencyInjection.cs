using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using ECommerce.AgentAPI.Application.Abstractions;
using ECommerce.AgentAPI.Application.Agents;
using ECommerce.AgentAPI.Application.Agents.Prompting;
using ECommerce.AgentAPI.Application.Agents.Routing;
using ECommerce.AgentAPI.Application.Options;
using ECommerce.AgentAPI.Application.Tools;
using ECommerce.AgentAPI.Configuration.Options;
using ECommerce.AgentAPI.Application.UseCases;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.API.Middleware;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.Approval.Capabilities.Cart;
using ECommerce.AgentAPI.Infrastructure.ErrorHandling;
using ECommerce.AgentAPI.Infrastructure.LLM;
using ECommerce.AgentAPI.Infrastructure.LLM.Google;
using ECommerce.AgentAPI.Infrastructure.LLM.OpenAI;
using ECommerce.AgentAPI.Infrastructure.LLM.Plugins;
using ECommerce.AgentAPI.Infrastructure.Memory;
using ECommerce.AgentAPI.Infrastructure.Tools;
using ECommerce.AgentAPI.Infrastructure.Observability;
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
    /// <summary>Política de rate limit usada em <c>POST /api/agent/chat</c> (120 req / 5 min por <c>sub</c> do JWT).</summary>
    public const string ChatRateLimitPolicy = "ChatPerJwtSub";

    public static IServiceCollection AddAgentApi(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Catálogo de ITool (uma tool, uma classe) — fonte canónica de approval/envelope para tools migradas ──
        services.AddToolCatalog(typeof(AgentApiDependencyInjection).Assembly);
        services.AddHostedService<ToolContractValidationHostedService>();

        // ── Aprovação, Kernel, plugins SK (instâncias também criadas em OpenAIKernelFactory; registo alinha evolução / testes) ──
        services.AddSingleton<ApprovalStateStore>();
        services.AddSingleton<ToolApprovalService>();
        services.AddScoped<OpenAIKernelFactory>();
        services.AddScoped<GoogleKernelFactory>();
        services.AddScoped<IKernelFactoryProviderStrategy, OpenAIKernelFactoryProviderStrategy>();
        services.AddScoped<IKernelFactoryProviderStrategy, GoogleKernelFactoryProviderStrategy>();
        services.AddScoped<ILLMProviderResolver, LLMProviderResolver>();
        services.AddScoped<IKernelFactory, ProviderKernelFactory>();
        services.AddScoped<IPluginFactory, PluginFactory>();
        services.AddScoped<ProductPlugin>();
        services.AddScoped<CartPlugin>();
        services.AddScoped<OrderPlugin>();

        // ── Refit + Polly + JWT no outbound (antes dos serviços que dependem de IECommerceApi) ──
        services.AddECommerceApi(configuration);

        // ── LLM Layer (Scoped — kernel/plugins respeitam dependências scoped por request) ──
        services.AddScoped<OpenAILLMService>();
        services.AddScoped<GoogleLLMService>();
        services.AddScoped<ILLMServiceProviderStrategy, OpenAILLMServiceProviderStrategy>();
        services.AddScoped<ILLMServiceProviderStrategy, GoogleLLMServiceProviderStrategy>();
        services.AddSingleton<ILLMProviderConfigurationValidationStrategy, OpenAILLMProviderConfigurationValidationStrategy>();
        services.AddSingleton<ILLMProviderConfigurationValidationStrategy, GoogleLLMProviderConfigurationValidationStrategy>();
        services.AddHostedService<LLMProviderConfigurationValidationHostedService>();
        services.AddScoped<ILLMFactory, LLMFactory>();

        // ── Aprovação (domínio) — adaptador sobre ToolApprovalService persistente em memória ──
        services.AddScoped<IToolApprovalService, ToolApprovalServiceAdapter>();

        // ── Pré-resolução de produto antes da aprovação: a pergunta ao usuário passa a refletir
        //    o item canônico (nome, preço, UUID), não o palpite cru do LLM. Depende de IECommerceApi,
        //    por isso scoped — segue a mesma lifetime dos plugins do SK.
        services.AddScoped<IToolApprovalArgumentEnrichmentStrategy, AddCartItemApprovalEnrichmentStrategy>();
        services.AddScoped<IToolApprovalArgumentEnrichmentStrategy, CartItemApprovalEnrichmentStrategy>();
        services.AddScoped<IApprovalArgumentEnricher, ApprovalArgumentEnricher>();

        // ── Memória (IMemoryService):
        //   • volatile (padrão): histórico em processo via ChatHistory (Microsoft.SemanticKernel) em
        //     AgentMemoryStore; Kernel/plugins SK são usados no pipeline LLM — não na serialização Redis.
        //   • redis: histórico como JSON numa chave por sessão (StackExchange.Redis); sem ChatHistory no API.
        //   Lifetimes: VolatileMemoryService = singleton (estado partilhado no processo); RedisMemoryService =
        //   scoped (sem estado próprio — instância efémera por pedido; útil em testes com scope por teste).
        var memProvider = configuration["Memory:Provider"]?.ToLowerInvariant() ?? "volatile";
        if (memProvider is "redis")
        {
            var conn = configuration["Memory:Redis:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Configuração 'Memory:Redis:ConnectionString' ausente para Memory:Provider=redis.");
            var redisOptions = ConfigurationOptions.Parse(conn);
            // Equiv. a abortConnect=false: não falha no arranque se o broker ainda não responde.
            redisOptions.AbortOnConnectFail = false;
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
            services.AddScoped<IMemoryService, RedisMemoryService>();
        }
        else
        {
            services.AddSingleton<AgentMemoryStore>();
            services.AddSingleton<IMemoryService, VolatileMemoryService>();
        }

        services.Configure<AgentOptions>(configuration.GetSection(AgentOptions.SectionName));
        services.Configure<AgentPromptOptions>(configuration.GetSection(AgentPromptOptions.SectionName));
        services.Configure<AgentCatalogOptions>(configuration.GetSection(AgentCatalogOptions.SectionName));
        services.Configure<AgentObservabilityOptions>(configuration.GetSection(AgentObservabilityOptions.SectionName));
        services.AddSingleton<IAgentObservability, AgentObservability>();
        services.AddSingleton<IAgentRouter, AgentRouter>();
        services.AddScoped<IAgentExecutionContext, AgentExecutionContext>();
        services.AddSingleton<IPromptComposer, PromptComposer>();
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
                            Window = TimeSpan.FromMinutes(5),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        }));
                }

                return RateLimitPartition.Get(
                    sub,
                    _ => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
            });
        });

        services.AddAgentRuntimeHardening(configuration);

        return services;
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
