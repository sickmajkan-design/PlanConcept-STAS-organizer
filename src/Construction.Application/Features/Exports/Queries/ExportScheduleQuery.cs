using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Spreadsheets;
using Construction.Application.Features.Planning;
using FluentValidation;
using MediatR;

namespace Construction.Application.Features.Exports.Queries;

/// <summary>
/// The schedule on paper: who is where on each day of a period, and what each site has on each day.
/// </summary>
/// <remarks>
/// Built from the planning query itself rather than from the tables, so a foreman's file holds exactly
/// what their screen holds: their own business unit, sick leave shown only as leave. Meant for printing and
/// pinning up on a site, so the period is kept to what fits across a page.
/// </remarks>
public sealed record ExportScheduleQuery : ExportQueryBase, IRequest<ExportFile>
{
    public Guid? BranchId { get; init; }
}

public class ExportScheduleQueryValidator : ExportQueryValidator<ExportScheduleQuery>
{
    /// <summary>Widest period the paper form covers: two months, a day to a column.</summary>
    public const int MaxPaperDays = 62;

    public ExportScheduleQueryValidator()
    {
        RuleFor(x => x.To)
            .Must((query, to) => to.DayNumber - query.From.DayNumber + 1 <= MaxPaperDays)
            .WithMessage($"The schedule on paper covers at most {MaxPaperDays} days.")
            .When(x => x.From != default && x.To >= x.From);
    }
}

public class ExportScheduleQueryHandler : IRequestHandler<ExportScheduleQuery, ExportFile>
{
    private readonly ISender _sender;
    private readonly ISpreadsheetWriter _writer;

    public ExportScheduleQueryHandler(ISender sender, ISpreadsheetWriter writer)
    {
        _sender = sender;
        _writer = writer;
    }

    public async Task<ExportFile> Handle(ExportScheduleQuery request, CancellationToken cancellationToken)
    {
        var english = ExportLabels.IsEnglish(request.Language);
        var plan = await _sender.Send(new GetPlanningQuery { From = request.From, To = request.To, BranchId = request.BranchId }, cancellationToken);

        var days = Enumerable.Range(0, request.To.DayNumber - request.From.DayNumber + 1)
            .Select(i => request.From.AddDays(i))
            .ToList();

        var sites = plan.Projects.ToDictionary(p => p.Id);

        string Weekday(DateOnly d) => d.DayOfWeek switch
        {
            DayOfWeek.Monday => english ? "Mon" : "Pon",
            DayOfWeek.Tuesday => english ? "Tue" : "Uto",
            DayOfWeek.Wednesday => english ? "Wed" : "Sri",
            DayOfWeek.Thursday => english ? "Thu" : "Čet",
            DayOfWeek.Friday => english ? "Fri" : "Pet",
            DayOfWeek.Saturday => english ? "Sat" : "Sub",
            _ => english ? "Sun" : "Ned",
        };

        string Away(string type) => type switch
        {
            "AnnualLeave" => english ? "Leave" : "GO",
            "SickLeave" => english ? "Sick" : "BO",
            _ => english ? "Away" : "Odsutan",
        };

        string SiteNames(IEnumerable<Guid> ids) =>
            string.Join(" / ", ids.Select(id => sites.TryGetValue(id, out var s) ? s.Name : "?").Distinct());

        // Sheet one: a row per person, a column per day, the site in each cell.
        var byPerson = new SpreadsheetSheet(
            ExportLabels.Get("sheet.schedule", english),
            [
                new(ExportLabels.Get("employee", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("position", english), SpreadsheetValueKind.Text),
                .. days.Select(d => new SpreadsheetColumn($"{Weekday(d)} {d:dd.MM.}", SpreadsheetValueKind.Text)),
            ],
            plan.Employees
                .Select(e => (IReadOnlyList<object?>)
                [
                    e.FullName,
                    e.Position,
                    .. days.Select(d =>
                    {
                        var away = e.Absences.FirstOrDefault(a => a.StartDate <= d && a.EndDate >= d);
                        if (away is not null)
                        {
                            return (object?)Away(away.Type);
                        }

                        var posted = e.Postings.Where(p => p.StartDate <= d && (p.EndDate == null || p.EndDate >= d)).Select(p => p.ProjectId).ToList();
                        return posted.Count == 0 ? null : SiteNames(posted);
                    }),
                ])
                .ToList());

        // Sheet two: a row per site per day it has people or needs them, with who is there.
        var siteRows = new List<IReadOnlyList<object?>>();

        foreach (var site in plan.Projects)
        {
            var needed = site.Needs.Sum(n => n.Count);

            foreach (var d in days)
            {
                var onSite = plan.Employees
                    .Where(e => e.Postings.Any(p => p.ProjectId == site.Id && p.StartDate <= d && (p.EndDate == null || p.EndDate >= d))
                        && !e.Absences.Any(a => a.StartDate <= d && a.EndDate >= d))
                    .Select(e => e.FullName)
                    .ToList();

                var active = d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
                    && (site.StartDate == null || d >= site.StartDate)
                    && (site.EndDate == null || d <= site.EndDate);

                if (onSite.Count == 0 && !(active && needed > 0))
                {
                    continue;
                }

                siteRows.Add(
                [
                    site.Name,
                    d,
                    active ? needed : null,
                    onSite.Count,
                    string.Join(", ", onSite),
                ]);
            }
        }

        var bySite = new SpreadsheetSheet(
            ExportLabels.Get("sheet.scheduleBySite", english),
            [
                new(ExportLabels.Get("project", english), SpreadsheetValueKind.Text),
                new(ExportLabels.Get("date", english), SpreadsheetValueKind.Date),
                new(ExportLabels.Get("needed", english), SpreadsheetValueKind.Integer),
                new(ExportLabels.Get("posted", english), SpreadsheetValueKind.Integer),
                new(ExportLabels.Get("crew", english), SpreadsheetValueKind.Text),
            ],
            siteRows);

        return _writer.Render([byPerson, bySite], "schedule", request);
    }
}
