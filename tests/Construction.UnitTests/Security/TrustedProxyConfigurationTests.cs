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

        Assert.True(trusted.IsEmpty);
    }

    [Fact]
    public void An_empty_string_means_no_trusted_proxy()
    {
        var trusted = ForwardedHeadersExtensions.ParseTrustedProxies(
            Configure((ForwardedHeadersExtensions.TrustedProxiesKey, "")));

        Assert.True(trusted.IsEmpty);
    }

    [Fact]
    public void Reads_a_single_address()
    {
        var trusted = ForwardedHeadersExtensions.ParseTrustedProxies(
            Configure((ForwardedHeadersExtensions.TrustedProxiesKey, "10.0.0.8")));

        Assert.Equal("10.0.0.8", Assert.Single(trusted.Addresses).ToString());
        Assert.Empty(trusted.Networks);
    }

    [Fact]
    public void Reads_a_comma_separated_list_from_one_environment_variable()
    {
        var trusted = ForwardedHeadersExtensions.ParseTrustedProxies(
            Configure((ForwardedHeadersExtensions.TrustedProxiesKey, "10.0.0.8, 10.0.0.9 ,::1")));

        Assert.Equal(["10.0.0.8", "10.0.0.9", "::1"], trusted.Addresses.Select(a => a.ToString()));
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

    [Fact]
    public void Reads_a_network_range()
    {
        // The form a container deployment needs. A proxy in a container has no
        // stable address, so the only thing that can be named up front is the
        // network it will be on.
        var trusted = ForwardedHeadersExtensions.ParseTrustedProxies(
            Configure((ForwardedHeadersExtensions.TrustedProxiesKey, "172.28.0.0/16")));

        var network = Assert.Single(trusted.Networks);

        Assert.Equal("172.28.0.0", network.Prefix.ToString());
        Assert.Equal(16, network.PrefixLength);
        Assert.Empty(trusted.Addresses);
        Assert.False(trusted.IsEmpty);
    }

    [Fact]
    public void Reads_addresses_and_ranges_together()
    {
        var trusted = ForwardedHeadersExtensions.ParseTrustedProxies(
            Configure((ForwardedHeadersExtensions.TrustedProxiesKey, "10.0.0.8, 172.28.0.0/16")));

        Assert.Equal("10.0.0.8", Assert.Single(trusted.Addresses).ToString());
        Assert.Equal(16, Assert.Single(trusted.Networks).PrefixLength);
        Assert.Equal(2, trusted.Count);
    }

    [Theory]
    [InlineData("172.28.0.0/33")]     // wider than IPv4 allows
    [InlineData("172.28.0.0/-1")]
    [InlineData("172.28.0.0/sixteen")]
    [InlineData("not-an-address/16")]
    public void Rejects_a_malformed_range(string value)
    {
        // Rejected rather than clamped. A prefix length that does not fit is a
        // typo, and quietly widening the trusted range is the exact mistake
        // this setting exists to prevent: every caller could then choose the
        // address the rate limiter partitions on.
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ForwardedHeadersExtensions.ParseTrustedProxies(
                Configure((ForwardedHeadersExtensions.TrustedProxiesKey, value))));

        Assert.Contains(value, exception.Message);
    }

    [Fact]
    public void Accepts_the_widest_and_narrowest_prefixes()
    {
        // /32 is one host written as a range, /0 is everything. Both are legal
        // and both are somebody's deliberate choice; the parser has no opinion.
        var trusted = ForwardedHeadersExtensions.ParseTrustedProxies(
            Configure((ForwardedHeadersExtensions.TrustedProxiesKey, "10.0.0.8/32, 0.0.0.0/0")));

        Assert.Equal([32, 0], trusted.Networks.Select(n => n.PrefixLength));
    }
}
