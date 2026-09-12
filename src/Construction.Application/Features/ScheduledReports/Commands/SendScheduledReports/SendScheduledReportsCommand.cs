using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Exports.Queries;
using Construction.Application.Features.Outbox;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.ScheduledReports.Commands.SendScheduledReports;

/// <summary>
/// Runs every subscription that is due: rebuilds its export for the right
/// period and queues it as an email attachment, then reschedules it for its
/// next occurrence — success or failure, so one broken subscription is
/// retried on its own cadence instead of every sweep.
/// </summary>
public record SendScheduledReportsCommand : IRequest<int>;

public class SendScheduledReportsCommandHandler : IRequestHandler<SendScheduledReportsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _mediator;
    private readonly IOutbox _outbox;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<SendScheduledReportsCommandHandler> _logger;

    public SendScheduledReportsCommandHandler(
        IApplicationDbContext context,
        ISender mediator,
        IOutbox outbox,
        IDateTimeProvider dateTimeProvider,
        ILogger<SendScheduledReportsCommandHandler> logger)
    {
        _context = context;
        _mediator = mediator;
        _outbox = outbox;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<int> Handle(SendScheduledReportsCommand request, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var today = DateOnly.FromDateTime(utcNow);

        var due = await _context.ScheduledReportSubscriptions
            .Where(s => s.NextRunAtUtc <= utcNow)
            .ToListAsync(cancellationToken);

        var sent = 0;

        foreach (var subscription in due)
        {
            var (from, to) = ScheduledReportScheduling.ComputePeriod(subscription.Cadence, today);

            try
            {
                // Run as the person who set the subscription up: the export
                // must show exactly what they are allowed to see, including a
                // permission lost between creating it and it next running —
                // never an unrestricted "system" view of the data.
                var creator = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Id == subscription.CreatedByUserId)
                    .Select(u => new { u.Email, u.Role, u.EmployeeId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (creator is null)
                {
                    _logger.LogWarning(
                        "Scheduled report {Id} has no creator account left; skipping.",
                        subscription.Id);
                    continue;
                }

                ExportFile file;

                using (CurrentUserOverride.Push(new CurrentUserOverride.Identity(
                    subscription.CreatedByUserId, creator.Email, creator.Role, creator.EmployeeId)))
                {
                    file = await BuildExportAsync(
                        subscription.ReportType, from, to, subscription.Language, cancellationToken);
                }

                var english = subscription.Language == "en";

                _outbox.Enqueue(new EmailPayload(
                    subscription.RecipientEmail,
                    Subject(subscription.ReportType, from, to, english),
                    Body(subscription.ReportType, from, to, english),
                    new EmailAttachment(file.FileName, file.ContentType, file.Content)));

                sent++;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to build scheduled report {Id} ({ReportType}).",
                    subscription.Id,
                    subscription.ReportType);
            }
            finally
            {
                subscription.NextRunAtUtc = ScheduledReportScheduling.ComputeNextRun(
                    subscription.Cadence, subscription.DayOfWeek, subscription.DayOfMonth, utcNow);
            }
        }

        if (due.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return sent;
    }

    private Task<ExportFile> BuildExportAsync(
        ScheduledReportType type, DateOnly from, DateOnly to, string language, CancellationToken cancellationToken)
    {
        IRequest<ExportFile> query = type switch
        {
            ScheduledReportType.TimeEntries =>
                new ExportTimeEntriesQuery { From = from, To = to, Language = language },
            ScheduledReportType.ProjectCosts =>
                new ExportProjectCostsQuery { From = from, To = to, Language = language },
            ScheduledReportType.VehicleCosts =>
                new ExportVehicleCostsQuery { From = from, To = to, Language = language },
            ScheduledReportType.MaterialMovements =>
                new ExportMaterialMovementsQuery { From = from, To = to, Language = language },
            ScheduledReportType.Absences =>
                new ExportAbsencesQuery { From = from, To = to, Language = language },
            ScheduledReportType.FinanceEntries =>
                new ExportFinanceEntriesQuery { From = from, To = to, Language = language },
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown scheduled report type."),
        };

        return _mediator.Send(query, cancellationToken);
    }

    private static string ReportName(ScheduledReportType type, bool english) => type switch
    {
        ScheduledReportType.TimeEntries => english ? "Work hours" : "Radni sati",
        ScheduledReportType.ProjectCosts => english ? "Project costs" : "Troškovi projekata",
        ScheduledReportType.VehicleCosts => english ? "Vehicle costs" : "Troškovi vozila",
        ScheduledReportType.MaterialMovements => english ? "Stock movements" : "Kretanje zaliha",
        ScheduledReportType.Absences => english ? "Time off" : "Odsustva",
        ScheduledReportType.FinanceEntries => english ? "Finance entries" : "Finansijski unosi",
        _ => type.ToString(),
    };

    private static string Subject(ScheduledReportType type, DateOnly from, DateOnly to, bool english) =>
        $"{ReportName(type, english)}: {from:yyyy-MM-dd} – {to:yyyy-MM-dd}";

    private static string Body(ScheduledReportType type, DateOnly from, DateOnly to, bool english) => english
        ? $"<p>Attached: {ReportName(type, english)} for {from:yyyy-MM-dd} to {to:yyyy-MM-dd}.</p>"
        : $"<p>U prilogu: {ReportName(type, english)} za period {from:yyyy-MM-dd} – {to:yyyy-MM-dd}.</p>";
}
