using Microsoft.OpenApi;

namespace ECommerce.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddECommerceSwagger(this IServiceCollection services)
    {
        const string schemeId = "bearer";

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "E-Commerce API",
                Version = "v1",
                Description = "API REST versionada (v1) com JWT."
            });

            options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
            {
                Description = "JWT no header Authorization. Informe apenas o token (Swagger adiciona \"Bearer\").",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(schemeId, document)] = []
            });
        });

        return services;
    }
}
