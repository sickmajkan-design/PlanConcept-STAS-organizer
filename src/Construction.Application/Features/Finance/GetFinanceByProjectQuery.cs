using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
using Construction.Domain.Enums;
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

    /// <summary>What the project cost in the period, subcontractors' flat pay included.</summary>
    public decimal Expense { get; init; }

    /// <summary>The part of <see cref="Expense"/> that is a subcontractor's flat day or lump sum.</summary>
    public decimal SubcontractorPay { get; init; }

    public decimal Profit { get; init; }

    /// <summary>Profit as a share of revenue, in percent. Null when nothing came in.</summary>
    public decimal? MarginPercent { get; init; }
}

public class FinanceByProjectDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>False when the caller may not see pay rates, so labour is zero in every row.</summary>
    public bool IncludesLabour { get; init; }

    /// <summary>The busiest projects first — by spending, then by revenue.</summary>
    public IReadOnlyList<FinanceProjectRowDto> Rows { get; init; } = [];

    /// <summary>How many projects had any money move in the period, before the list was cut to <c>Top</c>.</summary>
    public int TotalProjects { get; init; }
}

/// <summary>Income, spending and profit of each project over a period.</summary>
/// <remarks>
/// Spending is the project cost report's total plus what subcontractors were
/// paid a flat day or lump sum for the site. The report leaves manual pay out
/// of its total because it can repeat clocked hours; a subcontractor's flat
/// entry cannot, since it replaces their hours for that site and day (see
/// <see cref="ProjectLabourPricing"/>), so it is safe to add here.
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

        var costs = await _sender.Send(
            new GetProjectCostsQuery { From = request.From, To = request.To },
            cancellationToken);

        var expenseByProject = costs.Rows.ToDictionary(r => r.ProjectId, r => r.Total);

        var revenueByProject = await _context.ProjectRevenues
            .AsNoTracking()
            .Where(r => r.OccurredOn >= request.From && r.OccurredOn <= request.To)
            .GroupBy(r => r.ProjectId)
            .Select(g => new { ProjectId = g.Key, Amount = g.Sum(r => r.Amount) })
            .ToDictionaryAsync(r => r.ProjectId, r => r.Amount, cancellationToken);

        var flatPayByProject = await _context.FinanceEntries
            .AsNoTracking()
            .Where(f => f.ProjectId != null
                && (f.Kind == FinanceEntryKind.WorkerPaymentDaily || f.Kind == FinanceEntryKind.WorkerPaymentFixed)
                && f.Employee.Type == EmployeeType.Subcontractor
                && f.OccurredOn >= request.From
                && f.OccurredOn <= request.To)
            .GroupBy(f => f.ProjectId!.Value)
            .Select(g => new { ProjectId = g.Key, Amount = g.Sum(f => f.Amount) })
            .ToDictionaryAsync(r => r.ProjectId, r => r.Amount, cancellationToken);

        var ids = expenseByProject.Keys
            .Union(revenueByProject.Keys)
            .Union(flatPayByProject.Keys)
            .ToList();

        var projects = await _context.Projects
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.ContractValue, p.Budget })
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var rows = new List<FinanceProjectRowDto>();

        foreach (var id in ids)
        {
            // A project deleted since the money moved has no row to show.
            if (!projects.TryGetValue(id, out var project))
            {
                continue;
            }

            var flat = flatPayByProject.GetValueOrDefault(id);
            var expense = expenseByProject.GetValueOrDefault(id) + flat;
            var revenue = revenueByProject.GetValueOrDefault(id);
            var profit = revenue - expense;

            // A project the money merely brushed past shows nothing worth a row.
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
                SubcontractorPay = flat,
                Profit = profit,
                MarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100m, 1) : null,
            });
        }

        return new FinanceByProjectDto
        {
            From = request.From,
            To = request.To,
            IncludesLabour = costs.IncludesLabour,
            TotalProjects = rows.Count,
            Rows = rows
                .OrderByDescending(r => r.Expense)
                .ThenByDescending(r => r.Revenue)
                .ThenBy(r => r.ProjectName)
                .Take(request.Top)
                .ToList(),
        };
    }
}
