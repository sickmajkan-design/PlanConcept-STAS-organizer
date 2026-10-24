using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.WeeklySiteReports.Commands.CreateWeeklySiteReport;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Commands.MarkWeeklySiteReportProcessed;

/// <summary>What approving a report did to the monthly sheet.</summary>
public record ApproveWeeklySiteReportResult(int CellsWritten, int CellsUnchanged, int CellsKept);

/// <summary>
/// The office approves a weekly report: its hours are right, and they are entered into the month's sheet.
/// </summary>
/// <remarks>
/// <paramref name="Lines"/>, when given, are the workers and hours as the office has just checked them against the
/// picture; they replace what the foreman wrote. <paramref name="KeepExisting"/> names the workers whose cell in the
/// sheet already says something else and should stay as it is.
/// </remarks>
public record MarkWeeklySiteReportProcessedCommand(
    Guid Id,
    IReadOnlyList<ReportLineInput>? Lines = null,
    IReadOnlyList<Guid>? KeepExisting = null) : IRequest<ApproveWeeklySiteReportResult>;

public class MarkWeeklySiteReportProcessedCommandValidator : AbstractValidator<MarkWeeklySiteReportProcessedCommand>
{
    public MarkWeeklySiteReportProcessedCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Lines!)
            .Must(ReportLines.HaveNoDuplicates).WithMessage("The same worker is listed twice.")
            .When(x => x.Lines is not null);

        RuleForEach(x => x.Lines!).ChildRules(line =>
        {
            line.RuleFor(l => l.EmployeeId).NotEmpty();
            line.RuleFor(l => l.Hours).InclusiveBetween(0m, CreateWeeklySiteReportCommandValidator.MaxWeekHours);
            line.RuleFor(l => l.HoursSecondMonth).GreaterThanOrEqualTo(0m);
            line.RuleFor(l => l).Must(l => l.HoursSecondMonth <= l.Hours)
                .WithMessage("More hours in the later month than in the whole week.");
        }).When(x => x.Lines is not null);
    }
}

public class MarkWeeklySiteReportProcessedCommandHandler
    : IRequestHandler<MarkWeeklySiteReportProcessedCommand, ApproveWeeklySiteReportResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notifications;

    public MarkWeeklySiteReportProcessedCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _notifications = notifications;
    }

    public async Task<ApproveWeeklySiteReportResult> Handle(
        MarkWeeklySiteReportProcessedCommand request, CancellationToken cancellationToken)
    {
        var report = await _context.WeeklySiteReports
            .Include(r => r.Project)
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklySiteReport), request.Id);

        if (report.Status == WeeklyReportStatus.Processed && request.Lines is null)
        {
            return new ApproveWeeklySiteReportResult(0, 0, 0);
        }

        if (request.Lines is not null)
        {
            await ReportLines.ReplaceAsync(_context, report, request.Lines, cancellationToken);
        }

        var result = new WeeklyReportLedger.WriteResult(0, 0, 0);

        if (report.Type == WeeklyReportType.SignedHours && report.Lines.Count > 0)
        {
            result = await WeeklyReportLedger.WriteAsync(
                _context, report, (request.KeepExisting ?? []).ToHashSet(), cancellationToken);
        }

        report.Status = WeeklyReportStatus.Processed;
        report.ProcessedAt = _dateTimeProvider.UtcNow;
        report.ProcessedByUserId = _currentUserService.UserId;
        report.ReviewNote = null;

        // The report and what it entered into the sheets are saved together: either both happen or neither.
        await _context.SaveChangesAsync(cancellationToken);

        await WeeklyReportNotices.ToSubmitterAsync(
            _context,
            _notifications,
            report,
            NotificationType.WeeklyReportApproved,
            "Weekly report approved",
            $"{report.Project.Name} — KW{report.IsoWeek:D2}/{report.IsoYear}",
            note: null,
            cancellationToken);

        return new ApproveWeeklySiteReportResult(result.Written, result.Unchanged, result.Kept);
    }
}
