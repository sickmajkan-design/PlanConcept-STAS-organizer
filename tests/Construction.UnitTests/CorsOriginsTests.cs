using Construction.API.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Construction.UnitTests;

/// <summary>
/// The shape of a configured CORS origin, and whether the validator's opinion
/// of it agrees with what ASP.NET Core actually does.
/// </summary>
/// <remarks>
/// The reason this is worth testing rather than reading: a wrong origin is not
/// an error anywhere. The policy builds, the server starts, and every request
/// from the admin panel is refused by the browser with nothing in the server
/// log. The only way to find out is to be told at startup, and the only way to
/// trust being told is to check the rule against the matcher it is predicting.
/// </remarks>
public class CorsOriginsTests
{
    [Theory]
    [InlineData("https://admin.example.com")]
    [InlineData("http://localhost:5173")]
    [InlineData("https://admin.example.com:8443")]
    [InlineData("https://xn--nmea-0kaa.example.com")]
    public void A_well_formed_origin_is_accepted(string origin)
    {
        Assert.Null(CorsOrigins.Describe(origin));
    }

    [Theory]
    // The one that catches people: it is what the address bar shows.
    [InlineData("https://admin.example.com/")]
    [InlineData("https://admin.example.com/admin")]
    [InlineData("https://admin.example.com?x=1")]
    [InlineData("https://admin.example.com#top")]
    // Written out, but a browser omits the default port.
    [InlineData("https://admin.example.com:443")]
    [InlineData("http://admin.example.com:80")]
    [InlineData("admin.example.com")]
    [InlineData("ftp://admin.example.com")]
    [InlineData("https://user:pass@admin.example.com")]
    [InlineData("*")]
    [InlineData("")]
    [InlineData("   ")]
    public void An_origin_a_browser_would_never_send_is_rejected(string origin)
    {
        Assert.NotNull(CorsOrigins.Describe(origin));
    }

    [Theory]
    [InlineData("https://admin.example.com/", "https://admin.example.com")]
    [InlineData("https://admin.example.com:443", "https://admin.example.com")]
    [InlineData("http://localhost:5173/app", "http://localhost:5173")]
    public void The_message_names_the_string_that_would_have_worked(string wrong, string right)
    {
        // A validator that says "invalid" and stops has moved the problem from
        // runtime to startup without making it any easier to fix.
        var message = CorsOrigins.Describe(wrong);

        Assert.NotNull(message);
        Assert.Contains(right, message);
        Assert.Null(CorsOrigins.Describe(right));
    }

    [Fact]
    public void A_wildcard_is_told_why_it_cannot_be_one()
    {
        // "not an absolute URL" would also reject it, and would send whoever
        // wrote it looking for a syntax error. The reason is AllowCredentials:
        // ASP.NET Core refuses that combination, and at request time rather
        // than at startup.
        var message = CorsOrigins.Describe("*");

        Assert.NotNull(message);
        Assert.Contains("credentials", message);
    }

    [Fact]
    public void An_origin_carrying_credentials_says_so()
    {
        var message = CorsOrigins.Describe("https://user:pass@admin.example.com");

        Assert.NotNull(message);
        Assert.Contains("credentials", message);
    }

    [Fact]
    public void A_host_in_capitals_is_accepted_because_it_genuinely_works()
    {
        // Uri lowercases the host, so this one really does match, and
        // rejecting it would be a startup failure with nothing behind it.
        Assert.Null(CorsOrigins.Describe("HTTPS://Admin.Example.COM"));
        Assert.True(RealCorsAllows("HTTPS://Admin.Example.COM", "https://admin.example.com"));
    }

    [Theory]
    [InlineData("https://a.example.com")]
    [InlineData("http://localhost:5173")]
    public void What_the_validator_accepts_the_matcher_allows(string origin)
    {
        Assert.Null(CorsOrigins.Describe(origin));
        Assert.True(RealCorsAllows(origin, origin));
    }

    [Theory]
    [InlineData("https://a.example.com/", "https://a.example.com")]
    [InlineData("https://a.example.com/admin", "https://a.example.com")]
    [InlineData("https://a.example.com:443", "https://a.example.com")]
    public void What_the_validator_rejects_the_matcher_silently_refuses(string configured, string sent)
    {
        // The half that justifies the whole file. If ASP.NET Core ever starts
        // normalising these away, this test fails and the validator becomes an
        // obstacle rather than a warning — which is worth being told about.
        Assert.NotNull(CorsOrigins.Describe(configured));
        Assert.False(RealCorsAllows(configured, sent));
    }

    [Fact]
    public void The_shipped_configuration_passes_its_own_validation()
    {
        foreach (var file in new[] { "appsettings.json", "appsettings.Development.json" })
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(FindApiDirectory(), file), optional: false)
                .Build();

            var origins = CorsOrigins.ReadAndValidate(configuration);

            Assert.All(origins, origin => Assert.Null(CorsOrigins.Describe(origin)));
        }
    }

    [Fact]
    public void A_bad_origin_stops_startup_and_says_which_one()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://good.example.com",
                ["Cors:AllowedOrigins:1"] = "https://bad.example.com/",
            })
            .Build();

        var error = Assert.Throws<InvalidOperationException>(
            () => CorsOrigins.ReadAndValidate(configuration));

        Assert.Contains("https://bad.example.com/", error.Message);
        Assert.Contains("Cors:AllowedOrigins[1]", error.Message);
    }

    [Fact]
    public void No_configured_origins_is_not_an_error_here()
    {
        // It is a warning at startup, deliberately: an API with no browser
        // client in front of it is a legitimate deployment, and refusing to
        // start would be wrong. Empty is different from malformed.
        var configuration = new ConfigurationBuilder().Build();

        Assert.Empty(CorsOrigins.ReadAndValidate(configuration));
    }

    /// <summary>
    /// Asks the real <see cref="CorsService"/>, not a reimplementation of it.
    /// </summary>
    private static bool RealCorsAllows(string configured, string browserOrigin)
    {
        var policy = new CorsPolicyBuilder().WithOrigins(configured).Build();
        var service = new CorsService(Options.Create(new CorsOptions()), NullLoggerFactory.Instance);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Headers.Origin = browserOrigin;

        return service.EvaluatePolicy(context, policy).IsOriginAllowed;
    }

    private static string FindApiDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "Construction.API");

            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find src/Construction.API from " + AppContext.BaseDirectory);
    }
}
