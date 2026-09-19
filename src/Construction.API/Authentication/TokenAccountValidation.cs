using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace Construction.API.Authentication;

/// <summary>
/// Checks, on every request, that the account behind a valid token still is
/// what the token says it is.
/// </summary>
/// <remarks>
/// <para>
/// A signed token proves the API issued it, not that it is still true. Its
/// role, and the employee or customer it is linked to, were copied in at
/// sign-in and stay valid until it expires. Revoking the refresh token — which
/// changing a role or deactivating an account already does — only stops the
/// <em>next</em> token from being issued: the one already in the browser or on
/// the phone kept working, with the old permissions, for up to fifteen
/// minutes. A demoted administrator kept administrating; a deactivated account
/// kept signing in to nothing but doing everything.
/// </para>
/// <para>
/// So the token is checked against the account. One primary-key lookup per
/// request, on a table with a few dozen rows. It is deliberately not cached:
/// a cache would bring the delay back by another name, and the invalidation
/// that would avoid that has to reach every place a role can change.
/// </para>
/// A failed check answers 401, which both clients already treat as "try to
/// refresh" — and the refresh fails for the same reason, ending the session.
/// </remarks>
public static class TokenAccountValidation
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;

        if (!Guid.TryParse(principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId))
        {
            context.Fail("The token does not name an account.");
            return;
        }

        var database = context.HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();

        var account = await database.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.IsActive, u.Role, u.EmployeeId, u.CustomerId })
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

        if (account is null || !account.IsActive)
        {
            context.Fail("The account no longer exists or has been deactivated.");
            return;
        }

        var roleMatches = Enum.TryParse<UserRole>(principal!.FindFirstValue(ClaimTypes.Role), out var claimedRole)
            && claimedRole == account.Role;

        if (!roleMatches
            || ClaimedId(principal, "employeeId") != account.EmployeeId
            || ClaimedId(principal, "customerId") != account.CustomerId)
        {
            context.Fail("The account has changed since this token was issued.");
        }
    }

    private static Guid? ClaimedId(ClaimsPrincipal principal, string type) =>
        Guid.TryParse(principal.FindFirstValue(type), out var id) ? id : null;
}
