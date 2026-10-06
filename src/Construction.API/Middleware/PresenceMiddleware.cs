using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Presence;
using Construction.Domain.Enums;

namespace Construction.API.Middleware;

/// <summary>
/// Notes that an authenticated account just made a request. In memory only, so
/// it costs a dictionary write and nothing against the database.
/// </summary>
public class PresenceMiddleware
{
    private readonly RequestDelegate _next;

    public PresenceMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context, IPresenceTracker tracker, IDateTimeProvider clock)
    {
        var principal = context.User;

        if (principal.Identity?.IsAuthenticated == true
            && Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            && Enum.TryParse<UserRole>(principal.FindFirstValue(ClaimTypes.Role), out var role))
        {
            tracker.Touch(
                userId,
                role,
                PresenceClient.FromUserAgent(context.Request.Headers.UserAgent.ToString()),
                screen: null,
                clock.UtcNow);
        }

        await _next(context);
    }
}
