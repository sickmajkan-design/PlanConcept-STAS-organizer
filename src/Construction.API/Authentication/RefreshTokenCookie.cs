using Construction.Application.Features.Authentication.Models;
using Microsoft.Extensions.Options;

namespace Construction.API.Authentication;

/// <summary>
/// How the refresh token reaches a browser.
/// </summary>
/// <remarks>
/// <para>
/// The admin panel kept a seven-day refresh token in <c>localStorage</c>,
/// where any script running on the page can read it. That turns a single XSS —
/// from a dependency, a future rich-text field, an embedded map widget — into
/// a persistent account takeover rather than a session-length one: the
/// attacker walks away with a credential that keeps minting access tokens for
/// a week, from anywhere, after the tab is closed.
/// </para>
/// <para>
/// A cookie marked <c>HttpOnly</c> is not readable by script at all. The
/// browser still sends it, so the attacker can still act *through* the page
/// while it is open, which is a much smaller and much more recoverable
/// problem.
/// </para>
/// </remarks>
public class RefreshTokenCookieSettings
{
    public const string SectionName = "Auth:RefreshCookie";

    public string Name { get; set; } = "construction.refresh";

    /// <summary>
    /// Where the cookie is sent. Only the endpoints that need it.
    /// </summary>
    /// <remarks>
    /// Scoped rather than site-wide so it is not attached to every image and
    /// API call the page makes — fewer places for it to be logged by a proxy,
    /// and nothing else can be tricked into acting on it.
    /// </remarks>
    public string Path { get; set; } = "/api/auth";

    /// <summary>
    /// <c>Strict</c>, <c>Lax</c> or <c>None</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Strict</c> is the default and is what makes the cookie its own CSRF
    /// defence: a page on another site cannot cause the browser to send it at
    /// all, so the refresh endpoint cannot be invoked by a forged request.
    /// </para>
    /// <para>
    /// This works when the API and the panel share a registrable domain —
    /// <c>api.example.com</c> and <c>admin.example.com</c> are same-site even
    /// though they are different origins. Genuinely cross-site deployments
    /// need <c>None</c>, which requires HTTPS and re-opens CSRF, and which
    /// browsers are in the process of blocking as a third-party cookie. If the
    /// two are on different domains, putting them on one is the better fix.
    /// </para>
    /// </remarks>
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;

    /// <summary>
    /// Optional cookie domain, for sharing between subdomains.
    /// </summary>
    /// <remarks>
    /// Read through <see cref="EffectiveDomain"/>, never directly. Left blank
    /// in configuration it binds to an empty string rather than null, and an
    /// empty string reaches the wire as <c>Domain=</c> — which every browser
    /// rejects, so the cookie is silently never stored and the operator is
    /// signed out on the next refresh. The default configuration leaves it
    /// blank, so this is the normal case rather than an edge one.
    /// </remarks>
    public string? Domain { get; set; }

    /// <summary>The domain to actually send, or null to scope it to the host.</summary>
    public string? EffectiveDomain =>
        string.IsNullOrWhiteSpace(Domain) ? null : Domain;
}

/// <summary>Writes, reads and clears the refresh cookie.</summary>
public class RefreshTokenCookie
{
    /// <summary>
    /// A client sends this to say "give me the token as a cookie, and keep it
    /// out of the body".
    /// </summary>
    /// <remarks>
    /// Explicit rather than sniffed. The same API serves a browser and a
    /// phone, and guessing which one is asking — from a user agent, from an
    /// origin header — is the kind of inference that is right until it is not.
    /// The mobile app has platform secure storage and no cookie jar, so it
    /// says nothing and keeps getting the token in the body.
    /// </remarks>
    public const string ModeHeader = "X-Auth-Mode";

    public const string CookieMode = "cookie";

    private readonly RefreshTokenCookieSettings _settings;

    public RefreshTokenCookie(IOptions<RefreshTokenCookieSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <summary>True when the caller asked to be given a cookie.</summary>
    public static bool WantsCookie(HttpContext context) =>
        string.Equals(
            context.Request.Headers[ModeHeader].ToString(),
            CookieMode,
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Sets the cookie and returns the response with the token removed from
    /// the body.
    /// </summary>
    /// <remarks>
    /// Removing it from the body is the point. Leaving it there as well would
    /// mean a script on the page could still read it out of the login
    /// response — the cookie would be a second copy rather than a safer
    /// place, and the vulnerability would be exactly where it was.
    /// </remarks>
    public AuthResponse Issue(HttpContext context, AuthResponse response)
    {
        context.Response.Cookies.Append(_settings.Name, response.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            // Follows the scheme the request arrived on, so a developer on
            // plain HTTP still gets a working cookie while a deployment on
            // HTTPS gets one the browser will not send in the clear.
            Secure = context.Request.IsHttps,
            SameSite = _settings.SameSite,
            Path = _settings.Path,
            Domain = _settings.EffectiveDomain,
            // Expiring with the token itself: a cookie that outlives what it
            // carries is a request the server can only answer with 401.
            Expires = response.RefreshTokenExpiresAt,
            IsEssential = true,
        });

        return new AuthResponse
        {
            AccessToken = response.AccessToken,
            AccessTokenExpiresAt = response.AccessTokenExpiresAt,
            RefreshToken = string.Empty,
            RefreshTokenExpiresAt = response.RefreshTokenExpiresAt,
            User = response.User,
        };
    }

    /// <summary>
    /// The token to act on: the body's if it has one, otherwise the cookie's.
    /// </summary>
    /// <remarks>
    /// Body first so the mobile app is unaffected, and so a cookie left over
    /// from an earlier session cannot quietly override a token a caller
    /// deliberately supplied.
    /// </remarks>
    public string? Read(HttpContext context, string? fromBody)
    {
        if (!string.IsNullOrWhiteSpace(fromBody))
        {
            return fromBody;
        }

        return context.Request.Cookies.TryGetValue(_settings.Name, out var cookie)
            && !string.IsNullOrWhiteSpace(cookie)
                ? cookie
                : null;
    }

    /// <summary>
    /// Removes the cookie.
    /// </summary>
    /// <remarks>
    /// The attributes have to match the ones it was written with or the
    /// browser treats it as a different cookie and keeps the original — a
    /// sign-out that leaves the credential in place, which is the worst
    /// possible outcome for the one operation whose entire job is removal.
    /// </remarks>
    public void Clear(HttpContext context)
    {
        context.Response.Cookies.Delete(_settings.Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = _settings.SameSite,
            Path = _settings.Path,
            Domain = _settings.EffectiveDomain,
        });
    }
}
