using Microsoft.OpenApi.Models;

namespace SmartHorse.API.Extensions;

/// <summary>
/// Swagger/OpenAPI configuration (v0.1 Section 19 / Section 25 — API Design
/// Strategy). Adds JWT bearer auth support in the Swagger UI so protected
/// endpoints can be exercised directly from the docs during development.
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddSmartHorseSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Smart Horse Management System API",
                Version = "v1",
                Description = "Sprint 1+2 — Backend Foundation & Authentication (Person 1). " +
                               "Covers Identity/User Management only; other modules are implemented in later sprints."
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a valid JWT access token."
            };

            options.AddSecurityDefinition("Bearer", securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        return services;
    }
}
