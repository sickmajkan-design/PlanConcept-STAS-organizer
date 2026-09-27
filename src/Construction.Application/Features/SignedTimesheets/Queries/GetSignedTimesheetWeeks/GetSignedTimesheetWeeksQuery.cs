using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.SignedTimesheets.Queries.GetSignedTimesheetWeeks;

/// <summary>One calendar week a month touches, and whether that project's signed timesheet is filed.</summary>
public class SignedTimesheetWeekDto
{
    public int IsoWeek { get; init; }

    /// <summary>The ISO week's own year — see <see cref="LedgerTemplates.MonthWeeks"/>.</summary>
    public int IsoYear { get; init; }

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>Null until the week's row has been touched — see <c>GetOrCreateSignedTimesheetCommand</c>.</summary>
    public Guid? SignedTimesheetId { get; init; }

    public bool HasAttachment { get; init; }
}

/// <summary>
/// The calendar weeks one project/month touches, each flagged with whether a signed
/// timesheet scan has been filed for it yet — what the ledger's "attach" button per
/// week is built from.
/// </summary>
public record GetSignedTimesheetWeeksQuery : IRequest<IReadOnlyList<SignedTimesheetWeekDto>>
{
    public Guid ProjectId { get; init; }

    public int Year { get; init; }

    public int Month { get; init; }
}

public class GetSignedTimesheetWeeksQueryValidator : AbstractValidator<GetSignedTimesheetWeeksQuery>
{
    public GetSignedTimesheetWeeksQueryValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

public class GetSignedTimesheetWeeksQueryHandler
    : IRequestHandler<GetSignedTimesheetWeeksQuery, IReadOnlyList<SignedTimesheetWeekDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSignedTimesheetWeeksQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SignedTimesheetWeekDto>> Handle(
        GetSignedTimesheetWeeksQuery request,
        CancellationToken cancellationToken)
    {
        var weeks = LedgerTemplates.MonthWeeks(request.Year, request.Month);
        var isoYears = weeks.Select(w => w.IsoYear).Distinct().ToList();

        var rows = await _context.SignedTimesheets
            .AsNoTracking()
            .Where(s => s.ProjectId == request.ProjectId && isoYears.Contains(s.Year))
            .Select(s => new
            {
                s.Id,
                s.Year,
                s.IsoWeek,
                HasAttachment = _context.Attachments.Any(a => a.SignedTimesheetId == s.Id),
            })
            .ToListAsync(cancellationToken);

        return weeks
            .Select(w =>
            {
                var row = rows.FirstOrDefault(r => r.Year == w.IsoYear && r.IsoWeek == w.IsoWeek);

                return new SignedTimesheetWeekDto
                {
                    IsoWeek = w.IsoWeek,
                    IsoYear = w.IsoYear,
                    From = w.From,
                    To = w.To,
                    SignedTimesheetId = row?.Id,
                    HasAttachment = row?.HasAttachment ?? false,
                };
            })
            .ToList();
    }
}
