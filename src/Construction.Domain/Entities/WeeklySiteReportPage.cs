using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One picture or file of a weekly site report. A week may be written on several sheets, so a report has
/// as many pages as the foreman attached, in the order they were attached.
/// </summary>
public class WeeklySiteReportPage : BaseEntity
{
    public Guid ReportId { get; set; }

    public WeeklySiteReport Report { get; set; } = null!;

    public int SortOrder { get; set; }

    /// <summary>Name as uploaded, shown to people. Never used as a path.</summary>
    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long SizeBytes { get; set; }

    /// <summary>The <see cref="Application.Common.Interfaces.IFileStorage"/> key the page's bytes live under.</summary>
    public string StorageKey { get; set; } = null!;
}
