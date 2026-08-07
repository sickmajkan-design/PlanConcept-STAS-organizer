using Construction.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Construction.UnitTests;

/// <summary>
/// What <c>AddInfrastructure</c> refuses to start with.
/// </summary>
/// <remarks>
/// <para>
/// These settings used to be bound and never looked at, which meant a wrong
/// SMTP port or a truncated service-account key was discovered by the first
/// person who needed the feature — and for email that is a password reset the
/// API accepts, records as sent, and never delivers. Nobody reports that as a
/// bug against configuration.
/// </para>
/// <para>
/// The tests drive the real host: <c>ValidateOnStart</c> only fires from
/// <c>StartAsync</c>, so validating it any other way would be testing a
/// reimplementation of the registration rather than the registration.
/// </para>
/// </remarks>
public class InfrastructureConfigurationTests
{
    /// <summary>A connection string Npgsql accepts. Nothing connects during these tests.</summary>
    private const string Connection = "Host=localhost;Database=none;Username=none;Password=none";

    private static readonly Dictionary<string, string?> Minimum = new()
    {
        ["ConnectionStrings:DefaultConnection"] = Connection,
        ["JwtSettings:SecretKey"] = new string('k', 48),
        ["JwtSettings:Issuer"] = "construction",
        ["JwtSettings:Audience"] = "construction",
    };

    private static IHost Build(params (string Key, string? Value)[] overrides)
    {
        var settings = new Dictionary<string, string?>(Minimum);

        foreach (var (key, value) in overrides)
        {
            settings[key] = value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new HostBuilder()
            .ConfigureServices(services => services.AddInfrastructure(configuration))
            .Build();
    }

    private static async Task<string> StartFailureAsync(params (string, string?)[] overrides)
    {
        using var host = Build(overrides);

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        return string.Join(" ", error.Failures);
    }

    [Fact]
    public async Task A_deployment_that_configures_nothing_optional_still_starts()
    {
        // Email and push are optional, and a developer machine has neither.
        // Validation that fires on an unset feature would make the checks
        // something to work around rather than something to keep.
        using var host = Build();

        await host.StartAsync();
        await host.StopAsync();
    }

    [Fact]
    public void A_missing_connection_string_names_the_environment_variable()
    {
        // Without this the failure surfaces from inside Npgsql as a complaint
        // about a missing host, which sends the reader to the database rather
        // than to the variable nobody set.
        var configuration = new ConfigurationBuilder().Build();

        var error = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddInfrastructure(configuration));

        Assert.Contains("ConnectionStrings__DefaultConnection", error.Message);
    }

    [Fact]
    public void A_blank_connection_string_fails_the_same_way_as_a_missing_one()
    {
        // An empty environment variable is set as far as the binder is
        // concerned, so a null check alone lets this through.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "   ",
            })
            .Build();

        var error = Assert.Throws<InvalidOperationException>(
            () => new ServiceCollection().AddInfrastructure(configuration));

        Assert.Contains("ConnectionStrings__DefaultConnection", error.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("70000")]
    public async Task An_impossible_smtp_port_is_refused(string port)
    {
        var message = await StartFailureAsync(
            ("EmailSettings:Host", "smtp.example.com"),
            ("EmailSettings:Port", port));

        Assert.Contains("EmailSettings:Port", message);
    }

    [Theory]
    [InlineData("no-reply")]
    [InlineData("no-reply@")]
    [InlineData("")]
    public async Task A_from_address_no_mail_server_would_accept_is_refused(string address)
    {
        // A rejected envelope sender means every message bounces, and the
        // outbox reports the send as attempted.
        var message = await StartFailureAsync(
            ("EmailSettings:Host", "smtp.example.com"),
            ("EmailSettings:FromAddress", address));

        Assert.Contains("EmailSettings:FromAddress", message);
    }

    [Fact]
    public async Task A_bad_from_address_is_ignored_while_email_is_switched_off()
    {
        // The checks are gated on the feature being configured at all, so the
        // default appsettings value does not stop a machine with no SMTP.
        using var host = Build(("EmailSettings:FromAddress", "not-an-address"));

        await host.StartAsync();
        await host.StopAsync();
    }

    [Fact]
    public async Task Firebase_credentials_given_two_ways_at_once_are_refused()
    {
        // Which one wins is an implementation detail nobody should have to
        // know, and the loser is usually the one that was just changed.
        var message = await StartFailureAsync(
            ("Firebase:CredentialsPath", Path.GetTempFileName()),
            ("Firebase:CredentialsJson", """{"type":"service_account"}"""));

        Assert.Contains("alternatives", message);
    }

    [Fact]
    public async Task A_firebase_credentials_path_that_points_at_nothing_is_refused()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"absent-{Guid.NewGuid():N}.json");

        var message = await StartFailureAsync(("Firebase:CredentialsPath", missing));

        Assert.Contains("Firebase:CredentialsPath", message);
    }

    [Theory]
    [InlineData("{\"type\":\"service_account\"")]
    [InlineData("not json at all")]
    // Valid JSON, wrong shape: a key file is an object, and a bare string is
    // what a shell produces when the quoting goes wrong.
    [InlineData("\"{}\"")]
    public async Task A_mangled_firebase_key_is_refused(string json)
    {
        var message = await StartFailureAsync(("Firebase:CredentialsJson", json));

        Assert.Contains("Firebase:CredentialsJson", message);
    }

    [Fact]
    public async Task A_credentials_path_that_exists_is_accepted()
    {
        var file = Path.GetTempFileName();

        try
        {
            using var host = Build(("Firebase:CredentialsPath", file));

            await host.StartAsync();
            await host.StopAsync();
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task Well_formed_credentials_json_is_accepted()
    {
        using var host = Build(
            ("Firebase:CredentialsJson", """{"type":"service_account","project_id":"x"}"""));

        await host.StartAsync();
        await host.StopAsync();
    }

    [Fact]
    public async Task A_short_jwt_secret_is_still_refused()
    {
        // Already covered elsewhere, asserted here because the Email and
        // Firebase registrations were rewritten around it and a dropped
        // ValidateOnStart is invisible.
        var message = await StartFailureAsync(("JwtSettings:SecretKey", "short"));

        Assert.Contains("JwtSettings:SecretKey", message);
    }
}
