using Construction.API.Extensions;
using Microsoft.Extensions.Configuration;

namespace Construction.UnitTests.Security;

/// <summary>
/// The trusted-proxy list decides whether a caller can choose the address the
/// API believes it is talking to. That address is the sign-in rate limiter's
/// partition key, so getting this wrong turns the brute-force limit into a
/// formality.
/// </summary>
public class TrustedProxyConfigurationTests
{
    private static IConfiguration Configure(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void No_configuration_means_no_trusted_proxy()
    {
        // This is the case that matters. The forwarding middleware treats two
        // empty lists as "trust everyone" rather than "trust no one", so an
        // empty result here has to keep the middleware out of the pipeline.
        var trusted = ForwardedHeadersExtensions.ParseTrustedProxies(Configure());

        Assert.Empty(trusted);
    }

    [Fact]
    public void An_empty_string_means_no_trusted_proxy()
    {
        var trusted = ForwardedHeadersExtensions.ParseTrustedProxies(
            Configure((ForwardedHeadersExtensions.TrustedProxiesKey, "")));

        Assert.Empty(trusted);
    }

    [Fact]
    public void Reads_a_single_address()
    {
        var trusted = ForwardedHeadersExtensions.ParseTrustedProxies(
            Configure((ForwardedHeadersExtensions.TrustedProxiesKey, "10.0.0.8")));

        Assert.Equal("10.0.0.8", Assert.Single(trusted).ToString());
    }

    [Fact]
    public void Reads_a_comma_separated_list_from_one_environment_variable()
    {
        var trusted = ForwardedHeadersExtensions.ParseTrustedProxies(
            Configure((ForwardedHeadersExtensions.TrustedProxiesKey, "10.0.0.8, 10.0.0.9 ,::1")));

        Assert.Equal(["10.0.0.8", "10.0.0.9", "::1"], trusted.Select(a => a.ToString()));
    }

    [Fact]
    public void Rejects_a_value_that_is_not_an_address()
    {
        // Failing loudly beats silently trusting nothing, which would look
        // identical to a correct deployment until the audit trail was wrong.
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ForwardedHeadersExtensions.ParseTrustedProxies(
                Configure((ForwardedHeadersExtensions.TrustedProxiesKey, "proxy.internal"))));

        Assert.Contains("proxy.internal", exception.Message);
    }
}
