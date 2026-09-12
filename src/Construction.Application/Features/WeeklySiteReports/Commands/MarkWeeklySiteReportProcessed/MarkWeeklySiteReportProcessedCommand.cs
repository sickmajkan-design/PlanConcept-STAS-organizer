using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.WeeklySiteReports.Commands.MarkWeeklySiteReportProcessed;

/// <summary>The office's "handled" flag — billed, filed, whatever it means to them.</summary>
public record MarkWeeklySiteReportProcessedCommand(Guid Id) : IRequest;

public class MarkWeeklySiteReportProcessedCommandHandler
    : IRequestHandler<MarkWeeklySiteReportProcessedCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public MarkWeeklySiteReportProcessedCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(MarkWeeklySiteReportProcessedCommand request, CancellationToken cancellationToken)
    {
        var report = await _context.WeeklySiteReports
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WeeklySiteReport), request.Id);

        report.Status = WeeklyReportStatus.Processed;
        report.ProcessedAt = _dateTimeProvider.UtcNow;
        report.ProcessedByUserId = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
