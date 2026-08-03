using Construction.Domain.Enums;

namespace Construction.Application.Features.Absences;

/// <summary>Who may ask for time off, and who may grant it.</summary>
public static class AbsenceRules
{
    /// <summary>
    /// How far ahead leave may be booked. Longer than any holiday year, short
    /// enough that a mistyped year is caught.
    /// </summary>
    public const int MaxLeadDays = 550;

    /// <summary>
    /// How far back an absence may be recorded. Sick leave is often entered
    /// after the fact — somebody was ill on Monday and said so on Wednesday.
    /// </summary>
    public const int MaxBackdatingDays = 90;

    /// <summary>
    /// Longest single absence. Beyond this it is not a holiday, it is a change
    /// of employment status, and recording it as leave hides that.
    /// </summary>
    public const int MaxDays = 180;

    /// <summary>Granting or refusing leave is a supervisor's call.</summary>
    public static bool CanReview(UserRole? role) =>
        role is UserRole.SuperAdmin or UserRole.Admin
            or UserRole.ProjectManager or UserRole.Foreman;

    /// <summary>
    /// True when this caller may record an absence for somebody else.
    /// </summary>
    /// <remarks>
    /// A worker may ask for their own leave and nothing more. Anyone above
    /// them may record it for anyone, because sick leave is usually phoned in
    /// and typed by the office.
    /// </remarks>
    public static bool CanRequestForOthers(UserRole? role) => CanReview(role);

    /// <summary>True for roles that only ever see their own absences.</summary>
    public static bool IsRestrictedToOwnAbsences(UserRole? role) =>
        role is null or UserRole.Worker;
}
