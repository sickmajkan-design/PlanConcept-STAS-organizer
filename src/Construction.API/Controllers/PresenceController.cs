using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Construction.API.Authorization;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Presence;
using Construction.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Construction.API.Controllers;

public partial class PresenceController : ApiControllerBase
{
    public record HeartbeatRequest(string? Screen);

    /// <summary>
    /// "I am here" from an open panel tab. Cheap by design: no database access,
    /// only the in-memory tracker, so it can be sent once a minute by everybody.
    /// </summary>
    [HttpPost("heartbeat")]
    [Authorize(Policy = Policies.AllEmployees)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Heartbeat(
        HeartbeatRequest? request,
        [FromServices] IPresenceTracker tracker,
        [FromServices] IDateTimeProvider clock)
    {
        if (Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            && Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var role))
        {
            tracker.Touch(userId, role, "web", Clean(request?.Screen), clock.UtcNow);
        }

        return NoContent();
    }

    /// <summary>Who is online right now (never SuperAdmin accounts).</summary>
    [HttpGet("online")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    public async Task<ActionResult<List<OnlineUserDto>>> Online(CancellationToken cancellationToken) =>
        Ok(await Mediator.Send(new GetOnlineUsersQuery(), cancellationToken));

    /// <summary>Recent sign-ins; a SuperAdmin also sees other SuperAdmins' sign-ins.</summary>
    [HttpGet("sessions")]
    [Authorize(Policy = Policies.AdminAndAbove)]
    public async Task<ActionResult<List<UserSessionDto>>> Sessions(
        [FromQuery] Guid? userId,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default) =>
        Ok(await Mediator.Send(
            new GetUserSessionsQuery { UserId = userId, Days = days }, cancellationToken));

    /// <summary>
    /// The screen name as a path with ids removed, capped in length: it is shown
    /// to administrators, and an id in it would say which record somebody is on.
    /// </summary>
    private static string? Clean(string? screen)
    {
        if (string.IsNullOrWhiteSpace(screen)) return null;

        var path = IdPattern().Replace(screen.Trim(), ":id");

        return path.Length > 80 ? path[..80] : path;
    }

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}|\b\d+\b")]
    private static partial Regex IdPattern();
}
