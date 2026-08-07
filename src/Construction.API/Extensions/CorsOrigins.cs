namespace Construction.API.Extensions;

/// <summary>
/// Checks the shape of a configured CORS origin before the policy is built.
/// </summary>
/// <remarks>
/// <para>
/// A CORS origin is matched against the browser's <c>Origin</c> header by
/// string comparison, and that header is always exactly
/// <c>scheme://host[:port]</c> — no trailing slash, no path, and no port when
/// it is the default one for the scheme. Anything else configured here does
/// not throw and does not warn; it simply never matches. The symptom is an
/// admin panel that cannot reach the API, with a browser console error and
/// nothing at all in the server log, because as far as the server is concerned
/// the policy is loaded and working.
/// </para>
/// <para>
/// <c>https://admin.example.com/</c> is the one that catches people, because
/// it is what a browser puts in the address bar and what every other URL
/// setting in this file wants. It is measurably wrong here: a probe against
/// the real <c>CorsService</c> confirmed that a configured
/// <c>https://a.example.com/</c> does not allow an <c>Origin</c> of
/// <c>https://a.example.com</c>. Case is the exception — the host is
/// normalised, so <c>HTTPS://A.Example.COM</c> does match, and is accepted
/// here rather than reported as an error nobody would have to fix.
/// </para>
/// <para>
/// This validates in every environment, not only outside development. A
/// malformed origin is never intentional, and finding out locally is the whole
/// point.
/// </para>
/// </remarks>
public static class CorsOrigins
{
    public const string ConfigurationKey = "Cors:AllowedOrigins";

    /// <summary>
    /// Reads the configured origins and throws if any of them cannot match.
    /// </summary>
    /// <returns>The origins, unchanged, so the caller can hand them to the policy.</returns>
    public static string[] ReadAndValidate(IConfiguration configuration)
    {
        var origins = configuration.GetSection(ConfigurationKey).Get<string[]>() ?? [];

        var failures = origins
            .Select((origin, index) => (origin, index, problem: Describe(origin)))
            .Where(x => x.problem is not null)
            .Select(x => $"{ConfigurationKey}[{x.index}] '{x.origin}': {x.problem}")
            .ToList();

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "CORS origins must be written as scheme://host[:port], with no trailing slash "
                + "and no path, or the browser's Origin header will never match them:"
                + Environment.NewLine + " - "
                + string.Join(Environment.NewLine + " - ", failures));
        }

        return origins;
    }

    /// <summary>
    /// Returns null when the origin is usable, or a sentence saying what is
    /// wrong with it and what to write instead.
    /// </summary>
    public static string? Describe(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return "the entry is empty.";
        }

        if (origin == "*")
        {
            // AllowCredentials is on, and ASP.NET Core throws on the
            // combination at request time rather than here. Say so now, with
            // the reason, instead of leaving a 500 to be explained later.
            return "a wildcard cannot be used with credentials; name each origin.";
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return "not an absolute URL. Write it as https://admin.example.com.";
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return $"scheme '{uri.Scheme}' is not http or https.";
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return "an origin cannot carry credentials.";
        }

        // Everything else — trailing slash, path, query, fragment, an
        // explicitly written default port — falls out of this one comparison,
        // and the message can name the exact string that would have worked.
        var normalized = uri.GetLeftPart(UriPartial.Authority);

        if (!string.Equals(origin, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return $"a browser sends '{normalized}'. Use that, exactly.";
        }

        return null;
    }
}
