namespace Construction.Domain.Enums;

public enum WeeklyReportStatus
{
    Submitted = 1,
    /// <summary>The office looked at the report and approved it. Named Processed since before reports were reviewed.</summary>
    Processed = 2,

    /// <summary>The office sent the report back: something is wrong and the foreman has to send it again.</summary>
    CorrectionRequested = 3,
}
