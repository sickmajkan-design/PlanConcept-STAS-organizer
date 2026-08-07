namespace Construction.API.BackgroundServices;

/// <summary>
/// How long the system keeps the data it has finished with.
/// </summary>
/// <remarks>
/// In days rather than <c>TimeSpan</c> because these are set by whoever runs
/// the deployment, in a configuration file or an environment variable, and
/// <c>"180"</c> is harder to get wrong than <c>"180.00:00:00"</c>.
///
/// The defaults are deliberate, not placeholders. A deployment that never
/// touches this file still purges — the alternative, keeping everything until
/// somebody notices, is how a table of employee movements ends up holding four
/// years of it.
/// </remarks>
public class RetentionSettings
{
    public const string SectionName = "Retention";

    /// <summary>Days a spent refresh token is kept past its own expiry.</summary>
    /// <remarks>
    /// Long enough that the audit trail outlives the incident that would send
    /// somebody looking for it.
    /// </remarks>
    public int RefreshTokenGraceDays { get; set; } = 30;

    /// <summary>Days a used or expired password-reset token is kept.</summary>
    public int PasswordResetTokenGraceDays { get; set; } = 7;

    /// <summary>
    /// Days a GPS ping is kept. Zero or less keeps them forever.
    /// </summary>
    /// <remarks>
    /// "Forever" is offered because a deployment may be under an obligation to
    /// retain. It is not the default: a minute-by-minute record of where an
    /// employee was is personal data, and keeping it indefinitely should be a
    /// decision somebody made rather than one nobody did.
    /// </remarks>
    public int LocationRecordDays { get; set; } = 180;

    /// <summary>
    /// <see cref="LocationRecordDays"/> as a period, or null for "keep
    /// everything".
    /// </summary>
    /// <remarks>
    /// The conversion lives here rather than in the sweep so that "zero means
    /// forever" is stated once. Written the other way round — a check at the
    /// call site — a zero would become <c>TimeSpan.Zero</c>, which the command
    /// reads as "delete a ping the moment it arrives": the opposite of what
    /// the setting says, and a mistake that only shows up as a live map with
    /// nobody on it.
    /// </remarks>
    public TimeSpan? LocationRetention =>
        LocationRecordDays > 0 ? TimeSpan.FromDays(LocationRecordDays) : null;

    /// <summary>Days a delivered outbox message is kept.</summary>
    /// <remarks>
    /// Enough to answer "was that email sent, and when?" after somebody asks.
    /// Messages that were given up on are never purged — each one is a
    /// delivery that failed for good, and that is the thing worth keeping.
    /// </remarks>
    public int SentOutboxMessageDays { get; set; } = 14;

    /// <summary>Rows deleted per statement.</summary>
    public int BatchSize { get; set; } = 5_000;

    /// <summary>Statements per table per sweep.</summary>
    public int MaxBatchesPerTable { get; set; } = 20;

    /// <summary>Hours between sweeps.</summary>
    public int IntervalHours { get; set; } = 6;
}
