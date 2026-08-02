namespace Construction.API.Middleware;

/// <summary>
/// Adds the response headers a browser needs in order to defend the admin
/// panel on the API's behalf.
///
/// <para>
/// These matter more than usual here because the admin panel keeps its refresh
/// token in <c>localStorage</c>, where any script running on the page can read
/// it. Until that moves to an http-only cookie, a content-security policy is
/// the control that stops injected script from running in the first place.
/// </para>
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // The API serves JSON, never markup, so the policy can be as narrow as
        // it gets: nothing may load, and nothing may frame it.
        //
        // Swagger UI is the one exception — it is a real page that loads its
        // own script and styles from this origin, and needs inline ones. It is
        // only mapped in development, so the looser policy never reaches a
        // deployed environment.
        headers["Content-Security-Policy"] = IsSwagger(context.Request.Path)
            ? "default-src 'self'; script-src 'self' 'unsafe-inline'; " +
              "style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
              "frame-ancestors 'none'; base-uri 'none'"
            : "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

        // Stops a browser from guessing a different content type for a
        // response, which is how a JSON endpoint ends up executed as script.
        headers["X-Content-Type-Options"] = "nosniff";

        // Belt and braces with frame-ancestors, for older browsers.
        headers["X-Frame-Options"] = "DENY";

        // Tokens and ids appear in paths; do not leak them to third parties.
        headers["Referrer-Policy"] = "no-referrer";

        // The API has no use for any of these device capabilities.
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        return _next(context);
    }

    private static bool IsSwagger(PathString path) =>
        path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase);
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
