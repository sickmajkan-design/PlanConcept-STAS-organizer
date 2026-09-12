using System.Linq.Expressions;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.WeeklySiteReports.Models;

public record WeeklySiteReportDto
{
    public Guid Id { get; init; }

    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    public Guid SubmittedByEmployeeId { get; init; }

    public string SubmittedByEmployeeName { get; init; } = null!;

    public int IsoYear { get; init; }

    public int IsoWeek { get; init; }

    public WeeklyReportType Type { get; init; }

    public decimal? Quantity { get; init; }

    public string? Note { get; init; }

    public string FileName { get; init; } = null!;

    public WeeklyReportStatus Status { get; init; }

    public DateTime? ProcessedAt { get; init; }

    public string? ProcessedByEmail { get; init; }

    public DateTime CreatedAt { get; init; }
}

public static class WeeklySiteReportMapping
{
    public static Expression<Func<WeeklySiteReport, WeeklySiteReportDto>> Projection =>
        r => new WeeklySiteReportDto
        {
            Id = r.Id,
            ProjectId = r.ProjectId,
            ProjectName = r.Project.Name,
            SubmittedByEmployeeId = r.SubmittedByEmployeeId,
            SubmittedByEmployeeName = r.SubmittedByEmployee.FirstName + " " + r.SubmittedByEmployee.LastName,
            IsoYear = r.IsoYear,
            IsoWeek = r.IsoWeek,
            Type = r.Type,
            Quantity = r.Quantity,
            Note = r.Note,
            FileName = r.FileName,
            Status = r.Status,
            ProcessedAt = r.ProcessedAt,
            ProcessedByEmail = r.ProcessedByUser != null ? r.ProcessedByUser.Email : null,
            CreatedAt = r.CreatedAt,
        };
}
