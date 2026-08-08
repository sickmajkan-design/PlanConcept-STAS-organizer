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

    /// <summary>
    /// Days a time entry's clock-in and clock-out coordinates are kept. Zero
    /// or less — the default — keeps them for as long as the shift.
    /// </summary>
    /// <remarks>
    /// A deliberate exception to <see cref="LocationRecordDays"/>, and the
    /// reason it needs its own setting rather than inheriting that one. The
    /// two coordinates on a shift are recorded on the entry precisely so they
    /// outlive the GPS sweep: an approved timesheet is payroll evidence, and
    /// "where was this shift started" is part of it.
    ///
    /// That is defensible and it is still location data about a person, kept
    /// indefinitely. A deployment under a stricter regime — or one that
    /// decides the coordinates stop being evidence once the wage is paid — sets
    /// a number here. The default keeps the behaviour the design chose, rather
    /// than changing it quietly on somebody's behalf.
    /// </remarks>
    public int TimeEntryCoordinateDays { get; set; }

    /// <summary>
    /// <see cref="TimeEntryCoordinateDays"/> as a period, or null for "keep
    /// them with the shift".
    /// </summary>
    public TimeSpan? TimeEntryCoordinateRetention =>
        TimeEntryCoordinateDays > 0 ? TimeSpan.FromDays(TimeEntryCoordinateDays) : null;

    /// <summary>
    /// Days an audit entry is kept. Zero or less — the default — keeps them
    /// forever.
    /// </summary>
    /// <remarks>
    /// The one retention default that keeps rather than purges, and the
    /// exception is the point of the table. An audit trail exists for the
    /// dispute, the investigation or the claim that surfaces long after the
    /// change did, and those arrive on their own schedule: employment claims
    /// run to years in most jurisdictions. A trail that had quietly aged out
    /// the month somebody asks about is worse than no trail, because everybody
    /// believed it was there.
    ///
    /// Growth is bounded by what it records instead: only the entities under
    /// <c>IAuditable</c>, and only when a value actually changed. The
    /// machine-generated traffic — GPS, notifications, outbox — is not audited
    /// at all.
    ///
    /// Set a number here if a retention obligation requires one. Deleting
    /// evidence should be a decision somebody made.
    /// </remarks>
    public int AuditEntryDays { get; set; }

    /// <summary>
    /// <see cref="AuditEntryDays"/> as a period, or null for "keep
    /// everything". Same reasoning as <see cref="LocationRetention"/>.
    /// </summary>
    public TimeSpan? AuditRetention =>
        AuditEntryDays > 0 ? TimeSpan.FromDays(AuditEntryDays) : null;

    /// <summary>Rows deleted per statement.</summary>
    public int BatchSize { get; set; } = 5_000;

    /// <summary>Statements per table per sweep.</summary>
    public int MaxBatchesPerTable { get; set; } = 20;

    /// <summary>Hours between sweeps.</summary>
    public int IntervalHours { get; set; } = 6;
}
