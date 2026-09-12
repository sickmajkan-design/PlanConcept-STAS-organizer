using Construction.Domain.Enums;

namespace Construction.Application.Features.ScheduledReports.Models;

public record ScheduledReportSubscriptionDto
{
    public Guid Id { get; init; }

    public string RecipientEmail { get; init; } = null!;

    public ScheduledReportType ReportType { get; init; }

    public ScheduledReportCadence Cadence { get; init; }

    public DayOfWeek? DayOfWeek { get; init; }

    public int? DayOfMonth { get; init; }

    public string Language { get; init; } = null!;

    public DateTime NextRunAtUtc { get; init; }

    public string CreatedByEmail { get; init; } = null!;
}
