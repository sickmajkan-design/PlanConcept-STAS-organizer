using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries;

/// <summary>
/// The rules every path into <see cref="TimeEntry"/> has to obey, in one
/// place because there are five of them — clocking in, clocking out, manual
/// entry, correction and review — and a rule enforced in four of the five is
/// not a rule.
/// </summary>
/// <summary>
/// Who may see whose hours.
/// </summary>
/// <remarks>
/// Separate from the role policies on the controller, which answer "may this
/// role call this endpoint at all". This answers "and how much of the result
/// is theirs" — the part a route attribute cannot express, because the same
/// endpoint serves the phone and the office.
/// </remarks>
public static class TimeEntryAccess
{
    /// <summary>
    /// True for roles that may only ever see their own hours.
    /// </summary>
    /// <remarks>
    /// A missing role is treated as restricted. An unauthenticated request
    /// never reaches a handler, so the only way to get here without one is a
    /// token shaped in a way this build did not expect — and the safe reading
    /// of that is the least access, not the most.
    /// </remarks>
    public static bool IsRestrictedToOwnEntries(UserRole? role) =>
        role is null or UserRole.Worker;
}

public static class TimeEntryRules
{
    /// <summary>
    /// Longest shift the system will record. Anything beyond this is someone
    /// who forgot to clock out, not someone who worked it, and letting it
    /// through silently corrupts every hours total downstream.
    /// </summary>
    public static readonly TimeSpan MaxShiftDuration = TimeSpan.FromHours(16);

    /// <summary>
    /// How far back an entry may be created or moved. Long enough to fix last
    /// week's timesheet, short enough that a closed payroll period cannot be
    /// rewritten.
    /// </summary>
    public static readonly TimeSpan MaxBackdating = TimeSpan.FromDays(31);

    /// <summary>
    /// Reads an incoming time as UTC.
    /// </summary>
    /// <remarks>
    /// PostgreSQL timestamptz columns require UTC, and a payload without an
    /// explicit offset must not be read as the API server's local zone —
    /// otherwise the same request means different instants depending on where
    /// the container happens to run. Shared by the validators and the handlers
    /// so both agree on what a submitted time means.
    /// </remarks>
    public static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    /// <summary>
    /// Refuses a shift that overlaps one the employee already has.
    /// </summary>
    /// <param name="excludeId">
    /// The entry being edited, so it does not collide with itself.
    /// </param>
    /// <remarks>
    /// A running shift (no end) blocks anything at or after its start: until
    /// someone clocks out, it occupies all the time from then on.
    /// </remarks>
    public static async Task EnsureNoOverlapAsync(
        IApplicationDbContext context,
        Guid employeeId,
        DateTime startedAt,
        DateTime? endedAt,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        // An open-ended new entry reaches forward without limit, so treat it
        // as ending at the far edge rather than special-casing it twice.
        var newEnd = endedAt ?? DateTime.MaxValue.ToUniversalTime();

        var clashes = await context.TimeEntries
            .AsNoTracking()
            .Where(t => t.EmployeeId == employeeId)
            .Where(t => excludeId == null || t.Id != excludeId)
            // Half-open intervals: a shift ending at 14:00 and one starting at
            // 14:00 do not overlap, which is how back-to-back shifts are
            // actually recorded.
            .Where(t => t.StartedAt < newEnd && (t.EndedAt == null || t.EndedAt > startedAt))
            .AnyAsync(cancellationToken);

        if (clashes)
        {
            throw new ConflictException(
                "This overlaps a shift the employee already has recorded.");
        }
    }

    /// <summary>
    /// Refuses to change an entry that has been signed off.
    /// </summary>
    /// <remarks>
    /// Approved hours are what payroll pays against. Editing them after the
    /// fact would change what someone is owed with no trace, so the way back
    /// is to reject the entry first, which is recorded.
    /// </remarks>
    public static void EnsureEditable(TimeEntry entry)
    {
        if (entry.Status == TimeEntryStatus.Approved)
        {
            throw new ConflictException(
                "This entry is approved. Reject it first to make changes.");
        }
    }
}
