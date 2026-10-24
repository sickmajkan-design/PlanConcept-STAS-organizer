using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Queries.GetLedgerReportHours;

/// <summary>
/// How many of the hours in a month's weekly columns were entered from approved weekly site reports, and how many
/// were typed by hand, so the sheet can say how much of the month has a report behind it.
/// </summary>
public record GetLedgerReportHoursQuery(Guid LedgerId) : IRequest<LedgerReportHoursDto>;

public record LedgerReportHoursDto
{
    /// <summary>Hours in the weekly columns that came from an approved report.</summary>
    public decimal ApprovedHours { get; init; }

    /// <summary>Hours in the weekly columns that somebody typed (or typed over), with no report behind them.</summary>
    public decimal TypedHours { get; init; }

    /// <summary>How many approved reports have entered hours in this month.</summary>
    public int ReportCount { get; init; }
}

public class GetLedgerReportHoursQueryHandler : IRequestHandler<GetLedgerReportHoursQuery, LedgerReportHoursDto>
{
    private readonly IApplicationDbContext _context;

    public GetLedgerReportHoursQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerReportHoursDto> Handle(GetLedgerReportHoursQuery request, CancellationToken cancellationToken)
    {
        if (!await _context.Ledgers.AnyAsync(l => l.Id == request.LedgerId, cancellationToken))
        {
            throw new NotFoundException(nameof(Ledger), request.LedgerId);
        }

        var cells = await _context.LedgerCells
            .AsNoTracking()
            .Where(c => c.Row.Section.LedgerId == request.LedgerId
                && c.Column.SystemKey != null
                && c.Column.SystemKey.StartsWith("week")
                && c.Value != null)
            .Select(c => new { c.Value, c.SourceReportId })
            .ToListAsync(cancellationToken);

        var approved = cells.Where(c => c.SourceReportId != null).ToList();

        return new LedgerReportHoursDto
        {
            ApprovedHours = approved.Sum(c => LedgerCellMath.ParseNumeric(c.Value)),
            TypedHours = cells.Where(c => c.SourceReportId == null).Sum(c => LedgerCellMath.ParseNumeric(c.Value)),
            ReportCount = approved.Select(c => c.SourceReportId).Distinct().Count(),
        };
    }
}
