using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Commands.RequestWeeklySiteReportCorrection;

/// <summary>The office sends a weekly report back: something is wrong, and the foreman has to send it again.</summary>
public record RequestWeeklySiteReportCorrectionCommand(Guid Id, string Note) : IRequest;

public class RequestWeeklySiteReportCorrectionCommandValidator
    : AbstractValidator<RequestWeeklySiteReportCorrectionCommand>
{
    public RequestWeeklySiteReportCorrectionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Say what is wrong, so the foreman knows what to fix.")
            .MaximumLength(1000);
    }
}

public class RequestWeeklySiteReportCorrectionCommandHandler
    : IRequestHandler<RequestWeeklySiteReportCorrectionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notifications;

    public RequestWeeklySiteReportCorrectionCommandHandler(
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

    public async Task Handle(RequestWeeklySiteReportCorrectionCommand request, CancellationToken cancellationToken)
    {
        var report = await _context.WeeklySiteReports
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklySiteReport), request.Id);

        var note = request.Note.Trim();

        report.Status = WeeklyReportStatus.CorrectionRequested;
        report.ProcessedAt = _dateTimeProvider.UtcNow;
        report.ProcessedByUserId = _currentUserService.UserId;
        report.ReviewNote = note;

        await _context.SaveChangesAsync(cancellationToken);

        await WeeklyReportNotices.ToSubmitterAsync(
            _context,
            _notifications,
            report,
            NotificationType.WeeklyReportCorrection,
            "Weekly report needs a correction",
            $"{report.Project.Name} — KW{report.IsoWeek:D2}/{report.IsoYear}: {note}",
            note,
            cancellationToken);
    }
}
