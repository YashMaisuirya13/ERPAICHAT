using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Shared.Events.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(
        this IServiceCollection services,
        string apiTitle,
        string apiVersion = "v1")
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(apiVersion, new OpenApiInfo
            {
                Title = apiTitle,
                Version = apiVersion
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT Bearer token only (do NOT type 'Bearer ', Swagger UI will add it automatically).",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            options.AddSecurityDefinition("Bearer", securityScheme);

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    securityScheme,
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}

