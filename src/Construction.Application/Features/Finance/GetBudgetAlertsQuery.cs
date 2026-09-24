using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Finance;

public class BudgetAlertDto
{
    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    /// <summary>"Budget" or "Contract" — what the spending was measured against.</summary>
    public string Basis { get; init; } = null!;

    public decimal Limit { get; init; }

    /// <summary>What the project has cost from its start to today.</summary>
    public decimal Spent { get; init; }

    /// <summary>Spent as a share of the limit, in percent. Goes past 100 when the limit is passed.</summary>
    public decimal UsedPercent { get; init; }

    /// <summary>The share at which this project starts to warn.</summary>
    public int WarnPercent { get; init; }

    /// <summary>"Warning" from the warning share, "Over" from 100%.</summary>
    public string Level { get; init; } = null!;
}

public class BudgetAlertsDto
{
    /// <summary>False when the caller may not see pay rates, so labour and pay are missing from every figure.</summary>
    public bool IncludesLabour { get; init; }

    /// <summary>Projects at or past their warning share, the most spent first.</summary>
    public IReadOnlyList<BudgetAlertDto> Alerts { get; init; } = [];

    /// <summary>How many projects have anything to be measured against — so an empty list can say "all within" or "nothing set".</summary>
    public int MeasuredProjects { get; init; }
}

/// <summary>Which running projects have spent their budget — or the share of their contract — the office asked to be warned at.</summary>
/// <remarks>
/// Spending is measured from the start of the project to today, not over a
/// period: a budget is for the whole job. Each project chooses what it is
/// measured against (see <see cref="BudgetAlertRules"/>). Projects that are
/// finished or cancelled are left out; their overrun is history, not a warning.
/// The cost report is asked once per window for every project at once, not once
/// per project, and its windows do not overlap, so nothing is counted twice.
/// </remarks>
public record GetBudgetAlertsQuery : IRequest<BudgetAlertsDto>;

public class GetBudgetAlertsQueryHandler : IRequestHandler<GetBudgetAlertsQuery, BudgetAlertsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISender _sender;

    public GetBudgetAlertsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _sender = sender;
    }

    public async Task<BudgetAlertsDto> Handle(GetBudgetAlertsQuery request, CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);

        var running = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Status != ProjectStatus.Completed && p.Status != ProjectStatus.Cancelled)
            .Select(p => new
            {
                p.Id, p.Name, p.Budget, p.ContractValue, p.BudgetAlertBasis, p.BudgetWarnPercent, p.StartDate, p.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var measured = running
            .Select(p => new { Project = p, Measure = BudgetAlertRules.Resolve(p.BudgetAlertBasis, p.Budget, p.ContractValue) })
            .Where(x => x.Measure is not null)
            .ToList();

        if (measured.Count == 0)
        {
            return new BudgetAlertsDto { IncludesLabour = true, MeasuredProjects = 0 };
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        // The earliest a cost of any of these can fall: the start of the earliest project, less how far back a cost may be dated.
        var from = measured
            .Select(x => x.Project.StartDate is { } s && s < DateOnly.FromDateTime(x.Project.CreatedAt)
                ? s
                : DateOnly.FromDateTime(x.Project.CreatedAt))
            .Min()
            .AddDays(-CostRules.MaxBackdatingDays);

        var spent = new Dictionary<Guid, decimal>();
        var includesLabour = true;

        for (var windowStart = from; windowStart <= today;)
        {
            var windowEnd = windowStart.AddDays(GetProjectCostsQuery.MaxDays - 1);
            if (windowEnd > today)
            {
                windowEnd = today;
            }

            var report = await _sender.Send(
                new GetProjectCostsQuery { From = windowStart, To = windowEnd },
                cancellationToken);

            includesLabour &= report.IncludesLabour;

            foreach (var row in report.Rows)
            {
                spent[row.ProjectId] = spent.GetValueOrDefault(row.ProjectId) + row.Total;
            }

            windowStart = windowEnd.AddDays(1);
        }

        var alerts = new List<BudgetAlertDto>();

        foreach (var x in measured)
        {
            var (basis, limit) = x.Measure!.Value;
            var total = spent.GetValueOrDefault(x.Project.Id);
            var used = Math.Round(total / limit * 100m, 1);
            var warnAt = x.Project.BudgetWarnPercent ?? BudgetAlertRules.DefaultWarnPercent;

            if (used < warnAt)
            {
                continue;
            }

            alerts.Add(new BudgetAlertDto
            {
                ProjectId = x.Project.Id,
                ProjectName = x.Project.Name,
                Basis = basis.ToString(),
                Limit = limit,
                Spent = total,
                UsedPercent = used,
                WarnPercent = warnAt,
                Level = used >= BudgetAlertRules.OverPercent ? "Over" : "Warning",
            });
        }

        return new BudgetAlertsDto
        {
            IncludesLabour = includesLabour,
            MeasuredProjects = measured.Count,
            Alerts = alerts.OrderByDescending(a => a.UsedPercent).ThenBy(a => a.ProjectName).ToList(),
        };
    }
}
