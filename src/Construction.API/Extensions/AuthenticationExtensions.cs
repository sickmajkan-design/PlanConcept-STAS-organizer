using System.Security.Claims;
using System.Text;
using Construction.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Construction.API.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Registers the bearer scheme that guards the HTTP surface.
    ///
    /// This lives in the API rather than Infrastructure on purpose: validating
    /// an incoming Authorization header is a property of the web host, not of
    /// how tokens are produced or persisted. Infrastructure still owns
    /// <see cref="JwtSettings"/> and <c>IJwtProvider</c>, so a non-web host
    /// (the integration tests) can compose the application without pulling in
    /// the ASP.NET Core authentication stack.
    /// </summary>
    public static IServiceCollection AddJwtBearerAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings section is not configured.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(settings.SecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = ClaimTypes.Role
                };
            });

        return services;
    }
}
