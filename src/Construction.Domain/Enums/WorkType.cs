namespace Construction.Domain.Enums;

/// <summary>
/// What kind of hours a shift records. Kept separate from the duration because
/// the same hour is paid differently depending on when it was worked, and the
/// cost reporting that arrives later has to be able to tell them apart.
/// </summary>
public enum WorkType
{
    Regular = 1,
    Overtime = 2,
    Weekend = 3,
    PublicHoliday = 4,

    /// <summary>Travel to or from a site, which many collective agreements pay differently.</summary>
    Travel = 5
}
