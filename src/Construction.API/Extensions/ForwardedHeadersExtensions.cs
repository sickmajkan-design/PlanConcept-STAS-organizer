using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Construction.API.Extensions;

/// <summary>
/// Decides whose <c>X-Forwarded-For</c> header the API is willing to believe.
///
/// <para>
/// This is a security control, not plumbing. The client address decides which
/// partition the sign-in rate limiter counts against and what goes into the
/// refresh-token audit trail. Trusting the header unconditionally lets any
/// caller pick its own partition, which makes the brute-force limit a
/// formality — rotating a header value gives unlimited password guesses.
/// </para>
///
/// <para>
/// So the header is only honoured from addresses listed in
/// <c>Network:TrustedProxies</c>. Unset means nothing is trusted and the
/// address is taken from the connection, which is correct when the API is
/// reached directly.
/// </para>
///
/// <para>
/// <b>The middleware cannot express "trust nobody".</b> Its check reads
/// <c>KnownProxies.Count + KnownNetworks.Count &gt; 0 &amp;&amp;
/// !CheckKnownAddress(...)</c>, so clearing both lists makes the first term
/// false, the whole condition false, and the header is applied to <i>every</i>
/// caller. Emptying the lists therefore does the exact opposite of what it
/// looks like. That is why the middleware is left out of the pipeline
/// entirely when no proxy is configured, rather than added with empty lists.
/// </para>
/// </summary>
public static class ForwardedHeadersExtensions
{
    public const string TrustedProxiesKey = "Network:TrustedProxies";

    public static IServiceCollection AddTrustedProxyForwarding(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var trusted = ParseTrustedProxies(configuration);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Defaults include the loopback address; clear both lists so the
            // configured set is the whole truth rather than an addition to it.
            // Safe to clear here only because the middleware is not added at
            // all when `trusted` is empty — see UseTrustedProxyForwarding.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var proxy in trusted)
            {
                options.KnownProxies.Add(proxy);
            }

            // One hop by default. A larger value would let a trusted proxy's
            // client prepend entries and choose the address we record.
            options.ForwardLimit = 1;
        });

        return services;
    }

    /// <summary>
    /// Adds the forwarding middleware only when a proxy is actually trusted.
    /// With no proxy configured it is left out, so the client address comes
    /// from the connection and no header can override it.
    /// </summary>
    public static WebApplication UseTrustedProxyForwarding(this WebApplication app)
    {
        var trusted = ParseTrustedProxies(app.Configuration);
        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ForwardedHeadersExtensions).FullName!);

        if (trusted.Count == 0)
        {
            logger.LogInformation(
                "No trusted proxies configured ({Key} is empty): X-Forwarded-For is ignored and " +
                "the client address is taken from the connection.",
                TrustedProxiesKey);

            return app;
        }

        logger.LogInformation(
            "Trusting X-Forwarded-For from {Count} configured proxy address(es).",
            trusted.Count);

        app.UseForwardedHeaders();

        return app;
    }

    /// <summary>
    /// Reads the trusted proxy list. Accepts a comma-separated string or an
    /// array, so it can come from one environment variable or from JSON.
    /// </summary>
    public static IReadOnlyList<IPAddress> ParseTrustedProxies(IConfiguration configuration)
    {
        var values = configuration.GetSection(TrustedProxiesKey).Get<string[]>()
            ?? (configuration[TrustedProxiesKey] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var addresses = new List<IPAddress>();

        foreach (var value in values)
        {
            var trimmed = value.Trim();

            if (trimmed.Length == 0)
            {
                continue;
            }

            if (!IPAddress.TryParse(trimmed, out var address))
            {
                throw new InvalidOperationException(
                    $"'{TrustedProxiesKey}' contains '{trimmed}', which is not an IP address. " +
                    "List the addresses of the reverse proxies in front of the API, or leave it " +
                    "empty when there are none.");
            }

            addresses.Add(address);
        }

        return addresses;
    }
}
