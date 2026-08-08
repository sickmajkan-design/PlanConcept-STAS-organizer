using Construction.API.BackgroundServices;
using Microsoft.Extensions.Configuration;

namespace Construction.UnitTests;

/// <summary>
/// The retention settings, and the one conversion in them.
/// </summary>
/// <remarks>
/// Small, but the failure mode is not: "zero days" has to mean "keep
/// everything" and not <c>TimeSpan.Zero</c>, which the sweep would read as
/// "delete a ping the moment it arrives". The two readings differ by every row
/// in the table, and the wrong one shows up as a live map with nobody on it
/// rather than as an error.
/// </remarks>
public class RetentionSettingsTests
{
    [Fact]
    public void Defaults_purge_rather_than_keeping_everything()
    {
        // A deployment that never touches the configuration still purges. The
        // alternative — keep it all until somebody notices — is how a table of
        // employee movements ends up holding four years of them.
        var settings = new RetentionSettings();

        Assert.Equal(TimeSpan.FromDays(180), settings.LocationRetention);
        Assert.Equal(30, settings.RefreshTokenGraceDays);
        Assert.Equal(7, settings.PasswordResetTokenGraceDays);
    }

    [Fact]
    public void The_audit_trail_is_the_one_thing_kept_forever_by_default()
    {
        // Deliberately the opposite of every other default here. The trail
        // exists for the dispute that surfaces years after the change, and
        // employment claims run to years in most jurisdictions — so one that
        // had quietly aged out is worse than none, because everybody believed
        // it was there. Deleting evidence on a schedule should be a decision
        // somebody made.
        var settings = new RetentionSettings();

        Assert.Equal(0, settings.AuditEntryDays);
        Assert.Null(settings.AuditRetention);
    }

    [Theory]
    [InlineData(365)]
    [InlineData(2555)]
    public void An_audit_retention_can_still_be_set_where_one_is_required(int days)
    {
        var settings = new RetentionSettings { AuditEntryDays = days };

        Assert.Equal(TimeSpan.FromDays(days), settings.AuditRetention);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(3650)]
    public void A_positive_setting_becomes_that_many_days(int days)
    {
        var settings = new RetentionSettings { LocationRecordDays = days };

        Assert.Equal(TimeSpan.FromDays(days), settings.LocationRetention);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Zero_or_less_means_keep_everything_and_not_delete_everything(int days)
    {
        var settings = new RetentionSettings { LocationRecordDays = days };

        Assert.Null(settings.LocationRetention);
    }

    [Fact]
    public void The_shipped_configuration_binds_to_a_working_policy()
    {
        // Reads the file the API actually ships, so a typo in a key name —
        // which binds silently to the default and is invisible in review —
        // fails here instead.
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(
                Path.Combine(FindApiDirectory(), "appsettings.json"),
                optional: false)
            .Build();

        var settings = configuration
            .GetSection(RetentionSettings.SectionName)
            .Get<RetentionSettings>();

        Assert.NotNull(settings);
        Assert.Equal(TimeSpan.FromDays(180), settings.LocationRetention);
        Assert.Equal(30, settings.RefreshTokenGraceDays);
        Assert.Equal(7, settings.PasswordResetTokenGraceDays);
        Assert.Equal(14, settings.SentOutboxMessageDays);
        Assert.Equal(5_000, settings.BatchSize);
        Assert.Equal(6, settings.IntervalHours);

        // Absent from the shipped file on purpose, so it binds to "keep
        // everything". A number here would be a silent policy decision.
        Assert.Null(settings.AuditRetention);
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
