using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.HttpOverrides;

// `IPNetwork` exists in both namespaces and they are not the same type:
// `KnownNetworks` holds the ASP.NET one. Aliased rather than fully qualified
// everywhere, so the wrong one cannot be picked up by accident later.
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

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

            foreach (var proxy in trusted.Addresses)
            {
                options.KnownProxies.Add(proxy);
            }

            foreach (var network in trusted.Networks)
            {
                options.KnownNetworks.Add(network);
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

        if (trusted.IsEmpty)
        {
            logger.LogInformation(
                "No trusted proxies configured ({Key} is empty): X-Forwarded-For is ignored and " +
                "the client address is taken from the connection.",
                TrustedProxiesKey);

            return app;
        }

        logger.LogInformation(
            "Trusting X-Forwarded-For from {Addresses} proxy address(es) and {Networks} network(s).",
            trusted.Addresses.Count,
            trusted.Networks.Count);

        app.UseForwardedHeaders();

        return app;
    }

    /// <summary>
    /// Reads the trusted proxy list. Accepts a comma-separated string or an
    /// array, so it can come from one environment variable or from JSON, and
    /// accepts either a single address or a CIDR range.
    /// </summary>
    /// <remarks>
    /// The range form exists because a container has no stable address. The
    /// first deployment stack pinned the proxy to a fixed IP on a fixed subnet
    /// so this setting could name it — which worked until the subnet collided
    /// with something already on the host, and then nothing started at all.
    /// Naming the network instead is both more robust and closer to what is
    /// actually meant: trust whatever is on the internal network, because
    /// nothing else can reach the API at all.
    /// </remarks>
    public static TrustedProxySet ParseTrustedProxies(IConfiguration configuration)
    {
        var values = configuration.GetSection(TrustedProxiesKey).Get<string[]>()
            ?? (configuration[TrustedProxiesKey] ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var addresses = new List<IPAddress>();
        var networks = new List<IPNetwork>();

        foreach (var value in values)
        {
            var trimmed = value.Trim();

            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.Contains('/'))
            {
                networks.Add(ParseNetwork(trimmed));
                continue;
            }

            if (!IPAddress.TryParse(trimmed, out var address))
            {
                throw new InvalidOperationException(
                    $"'{TrustedProxiesKey}' contains '{trimmed}', which is neither an IP address " +
                    "nor a CIDR range. List the addresses of the reverse proxies in front of the " +
                    "API, or the network they are on, or leave it empty when there are none.");
            }

            addresses.Add(address);
        }

        return new TrustedProxySet(addresses, networks);
    }

    private static IPNetwork ParseNetwork(string value)
    {
        var slash = value.IndexOf('/');
        var addressPart = value[..slash];
        var prefixPart = value[(slash + 1)..];

        // Rejected rather than clamped. A prefix length that does not fit the
        // address family is a typo, and silently widening or narrowing the
        // trusted range is exactly the mistake this setting exists to prevent.
        if (!IPAddress.TryParse(addressPart, out var address)
            || !int.TryParse(prefixPart, out var prefixLength)
            || prefixLength < 0
            || prefixLength > (address.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32))
        {
            throw new InvalidOperationException(
                $"'{TrustedProxiesKey}' contains '{value}', which is not a valid CIDR range. " +
                "Write it as an address and a prefix length, such as 172.28.0.0/16.");
        }

        return new IPNetwork(address, prefixLength);
    }
}

/// <summary>
/// The two shapes a trusted proxy can take: an exact address, or the network
/// it sits on.
/// </summary>
/// <remarks>
/// Kept as one type rather than two out-parameters so that "is anything
/// trusted at all" is a single question. The answer decides whether the
/// forwarding middleware is added to the pipeline, and getting that wrong in
/// the empty direction means trusting every caller's claim about its own
/// address.
/// </remarks>
public sealed record TrustedProxySet(
    IReadOnlyList<IPAddress> Addresses,
    IReadOnlyList<IPNetwork> Networks)
{
    public bool IsEmpty => Addresses.Count == 0 && Networks.Count == 0;

    public int Count => Addresses.Count + Networks.Count;
}
