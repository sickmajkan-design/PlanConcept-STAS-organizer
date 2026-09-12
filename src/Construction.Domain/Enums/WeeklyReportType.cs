namespace Construction.Domain.Enums;

/// <summary>
/// What kind of proof a weekly site report carries. Only changes what the
/// office expects to find on the attached document — the platform does not
/// parse or price any of them.
/// </summary>
public enum WeeklyReportType
{
    /// <summary>A timesheet the customer signed off on for the week.</summary>
    SignedHours = 1,

    /// <summary>
    /// A German/Austrian-style measurement certificate: quantities against
    /// item codes, each code priced separately. The office reads the codes
    /// and prices off the document itself and bills elsewhere — this
    /// platform only files the proof, it does not know the price list.
    /// </summary>
    Aufmass = 2,

    Other = 3,
}
