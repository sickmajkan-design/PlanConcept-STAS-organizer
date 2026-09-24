using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Queries.GetCompanyCosts;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Finance;

public class FinanceProjectRowDto
{
    public Guid ProjectId { get; init; }

    public string ProjectName { get; init; } = null!;

    public decimal? ContractValue { get; init; }

    public decimal? Budget { get; init; }

    /// <summary>What came in against this project's contract in the period.</summary>
    public decimal Revenue { get; init; }

    /// <summary>What the project cost in the period — the project cost report's total.</summary>
    public decimal Expense { get; init; }

    public decimal Profit { get; init; }

    /// <summary>Profit as a share of revenue, in percent. Null when nothing came in.</summary>
    public decimal? MarginPercent { get; init; }
}

/// <summary>
/// The part of the company's money that belongs to no project in the list:
/// the fleet, the tools, empty housing, costs and pay tied to no site, income
/// from renting out, and anything of a project that no longer exists.
/// </summary>
public class FinanceUnallocatedDto
{
    public decimal Revenue { get; init; }

    public decimal Expense { get; init; }

    public decimal Profit { get; init; }
}

public class FinanceByProjectDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>False when the caller may not see pay rates, so labour and pay are zero in every row.</summary>
    public bool IncludesLabour { get; init; }

    /// <summary>The busiest projects first — by spending, then by revenue.</summary>
    public IReadOnlyList<FinanceProjectRowDto> Rows { get; init; } = [];

    /// <summary>How many projects had any money move in the period, before the list was cut to <c>Top</c>.</summary>
    public int TotalProjects { get; init; }

    /// <summary>
    /// The company's figures for the period, the same ones the overview shows —
    /// so every row, the projects left off the list, and <see cref="Unallocated"/>
    /// add up to them exactly.
    /// </summary>
    public FinanceUnallocatedDto Company { get; init; } = null!;

    /// <summary>What the company's figures hold beyond the projects of the whole period (not just those listed).</summary>
    public FinanceUnallocatedDto Unallocated { get; init; } = null!;

    /// <summary>
    /// Days on which a pay entry tied to no site sits beside hours the same
    /// person clocked at a site. The entry cannot say which site's hours it
    /// replaces, so both are counted; someone should look at each.
    /// </summary>
    public int UnassignedPayOverlaps { get; init; }
}

/// <summary>Income, spending and profit of each project over a period.</summary>
/// <remarks>
/// A project's spending is the project cost report's total, which already
/// holds the manual pay entered for it and leaves out the clocked hours that
/// pay stands in for (see <see cref="ProjectLabourPricing"/>) — so nothing here
/// is added in a second time. What no project owns is not spread across the
/// projects but reported once, as <see cref="FinanceByProjectDto.Unallocated"/>.
/// </remarks>
public record GetFinanceByProjectQuery : IRequest<FinanceByProjectDto>
{
    public const int MaxTop = 100;

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public int Top { get; init; } = 10;
}

public class GetFinanceByProjectQueryValidator : AbstractValidator<GetFinanceByProjectQuery>
{
    public GetFinanceByProjectQueryValidator()
    {
        RuleFor(x => x.From).NotEqual(default(DateOnly)).WithMessage("A start date is required.");

        RuleFor(x => x.To)
            .NotEqual(default(DateOnly)).WithMessage("An end date is required.")
            .GreaterThanOrEqualTo(x => x.From).WithMessage("The end of the period must not be before its start.")
            .Must((query, to) => to.DayNumber - query.From.DayNumber + 1 <= GetProjectCostsQuery.MaxDays)
            .WithMessage($"The period must not exceed {GetProjectCostsQuery.MaxDays} days.")
            .When(x => x.From != default);

        RuleFor(x => x.Top).InclusiveBetween(1, GetFinanceByProjectQuery.MaxTop);
    }
}

public class GetFinanceByProjectQueryHandler : IRequestHandler<GetFinanceByProjectQuery, FinanceByProjectDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public GetFinanceByProjectQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<FinanceByProjectDto> Handle(GetFinanceByProjectQuery request, CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);

        var from = request.From;
        var to = request.To;

        var projectCosts = await _sender.Send(new GetProjectCostsQuery { From = from, To = to }, cancellationToken);
        var companyCosts = await _sender.Send(new GetCompanyCostsQuery { From = from, To = to }, cancellationToken);

        var expenseByProject = projectCosts.Rows.ToDictionary(r => r.ProjectId, r => r.Total);

        var revenueByProject = await _context.ProjectRevenues
            .AsNoTracking()
            .Where(r => r.OccurredOn >= from && r.OccurredOn <= to)
            .GroupBy(r => r.ProjectId)
            .Select(g => new { ProjectId = g.Key, Amount = g.Sum(r => r.Amount) })
            .ToDictionaryAsync(r => r.ProjectId, r => r.Amount, cancellationToken);

        var otherRevenue = await _context.CompanyRevenues
            .AsNoTracking()
            .Where(r => r.OccurredOn >= from && r.OccurredOn <= to)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

        var ids = expenseByProject.Keys.Union(revenueByProject.Keys).ToList();

        var projects = await _context.Projects
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.ContractValue, p.Budget })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var rows = new List<FinanceProjectRowDto>();

        foreach (var id in ids)
        {
            // A project deleted since the money moved has no row to show; its
            // figures fall into the unallocated part, which is computed against
            // the company's totals, so they are not lost.
            if (!projects.TryGetValue(id, out var project))
            {
                continue;
            }

            var expense = expenseByProject.GetValueOrDefault(id);
            var revenue = revenueByProject.GetValueOrDefault(id);

            if (expense == 0 && revenue == 0)
            {
                continue;
            }

            rows.Add(new FinanceProjectRowDto
            {
                ProjectId = id,
                ProjectName = project.Name,
                ContractValue = project.ContractValue,
                Budget = project.Budget,
                Revenue = revenue,
                Expense = expense,
                Profit = revenue - expense,
                MarginPercent = revenue > 0 ? Math.Round((revenue - expense) / revenue * 100m, 1) : null,
            });
        }

        var companyRevenue = revenueByProject.Values.Sum() + otherRevenue;
        var companyExpense = companyCosts.Total;

        var unallocatedRevenue = companyRevenue - rows.Sum(r => r.Revenue);
        var unallocatedExpense = companyExpense - rows.Sum(r => r.Expense);

        return new FinanceByProjectDto
        {
            From = from,
            To = to,
            IncludesLabour = projectCosts.IncludesLabour,
            TotalProjects = rows.Count,
            Rows = rows
                .OrderByDescending(r => r.Expense)
                .ThenByDescending(r => r.Revenue)
                .ThenBy(r => r.ProjectName)
                .Take(request.Top)
                .ToList(),
            Company = new FinanceUnallocatedDto
            {
                Revenue = companyRevenue,
                Expense = companyExpense,
                Profit = companyRevenue - companyExpense,
            },
            Unallocated = new FinanceUnallocatedDto
            {
                Revenue = unallocatedRevenue,
                Expense = unallocatedExpense,
                Profit = unallocatedRevenue - unallocatedExpense,
            },
            UnassignedPayOverlaps = projectCosts.IncludesLabour
                ? await CountUnassignedPayOverlapsAsync(from, to, cancellationToken)
                : 0,
        };
    }

    private async Task<int> CountUnassignedPayOverlapsAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var entries = await _context.FinanceEntries
            .AsNoTracking()
            .Where(f => f.ProjectId == null && f.OccurredOn >= from && f.OccurredOn <= to)
            .Select(f => new { f.EmployeeId, f.OccurredOn })
            .Distinct()
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return 0;
        }

        var employeeIds = entries.Select(e => e.EmployeeId).Distinct().ToList();

        var clocked = (await _context.TimeEntries
                .AsNoTracking()
                .Where(t => t.Status == Construction.Domain.Enums.TimeEntryStatus.Approved
                    && t.ProjectId != null
                    && t.EndedAt != null
                    && employeeIds.Contains(t.EmployeeId)
                    && DateOnly.FromDateTime(t.StartedAt) >= from
                    && DateOnly.FromDateTime(t.StartedAt) <= to)
                .Select(t => new { t.EmployeeId, Day = DateOnly.FromDateTime(t.StartedAt) })
                .Distinct()
                .ToListAsync(cancellationToken))
            .Select(t => (t.EmployeeId, t.Day))
            .ToHashSet();

        return entries.Count(e => clocked.Contains((e.EmployeeId, e.OccurredOn)));
    }
}
