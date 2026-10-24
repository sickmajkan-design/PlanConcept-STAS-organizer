using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Commands.MarkWeeklySiteReportProcessed;

/// <summary>The office approves a weekly report: its hours are right and it is filed.</summary>
public record MarkWeeklySiteReportProcessedCommand(Guid Id) : IRequest;

public class MarkWeeklySiteReportProcessedCommandHandler
    : IRequestHandler<MarkWeeklySiteReportProcessedCommand>
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

    public async Task Handle(MarkWeeklySiteReportProcessedCommand request, CancellationToken cancellationToken)
    {
        var report = await _context.WeeklySiteReports
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklySiteReport), request.Id);

        if (report.Status == WeeklyReportStatus.Processed)
        {
            return;
        }

        report.Status = WeeklyReportStatus.Processed;
        report.ProcessedAt = _dateTimeProvider.UtcNow;
        report.ProcessedByUserId = _currentUserService.UserId;
        report.ReviewNote = null;

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
    }
}
