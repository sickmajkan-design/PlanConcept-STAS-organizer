using Microsoft.OpenApi.Models;

namespace Construction.API.Extensions;

public static class SwaggerExtensions
{
    private static string SchemaId(Type type)
    {
        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            var arguments = string.Join("And", type.GetGenericArguments().Select(SchemaId));

            return $"{name}Of{arguments}";
        }

        return (type.FullName ?? type.Name)
            .Replace("Construction.Application.Features.", string.Empty)
            .Replace("Construction.Application.", string.Empty)
            .Replace('.', '_')
            .Replace('+', '_');
    }

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            // Two different types are called UserDto (the sign-in response's and the
            // user list's), and Swashbuckle refuses to build the document when two
            // types want the same name — the whole page answered 500. Naming a schema
            // by its namespace as well keeps them apart.
            options.CustomSchemaIds(SchemaId);

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Construction Workforce Management API",
                Version = "v1",
                Description =
                    "Phase 1 API: authentication, employees, projects, vehicles, tools, " +
                    "materials, GPS tracking and push notifications."
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter your JWT access token (without the 'Bearer ' prefix).",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
