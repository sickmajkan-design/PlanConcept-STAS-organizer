using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Accommodations.Costs;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
using Construction.Application.Features.Finance;
using Construction.Application.Features.Costs.Queries.GetToolCosts;
using Construction.Application.Features.Costs.Queries.GetVehicleCosts;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Queries.GetCompanyCosts;

/// <summary>What the whole company spent in a period, by kind.</summary>
public class CompanyCostsDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>False when the caller may not see pay rates; the pay figures are then zero.</summary>
    public bool IncludesLabour { get; init; }

    /// <summary>Approved hours priced at each person's rate.</summary>
    public decimal Labour { get; init; }

    /// <summary>Pay entered by hand rather than clocked.</summary>
    public decimal ManualPay { get; init; }

    /// <summary>Material issued out, priced — to a project or not.</summary>
    public decimal Material { get; init; }

    /// <summary>Other costs (bookkeeping, insurance, fees…), to a project or not.</summary>
    public decimal GeneralExpenses { get; init; }

    /// <summary>Rent of every accommodation for the period, occupied or empty.</summary>
    public decimal Accommodation { get; init; }

    /// <summary>Fuel, service, repairs and rented or leased cars.</summary>
    public decimal Vehicles { get; init; }

    /// <summary>Repairs, maintenance and rented tools.</summary>
    public decimal Tools { get; init; }

    public decimal Total { get; init; }

    /// <summary>Hours worked that have no rate, so are in <see cref="Labour"/> at zero.</summary>
    public int UnpricedMinutes { get; init; }
}

/// <summary>
/// Everything the company spent, whether or not it belongs to a project.
/// </summary>
/// <remarks>
/// <para>
/// The project report answers "what did each site cost" and so counts only what is
/// tied to a site. A fleet, a set of tools, a flat nobody works from and the
/// bookkeeper's invoice belong to no site, and a company's cost is not complete
/// without them. This is the total the dashboard shows.
/// </para>
/// <para>
/// Nothing is counted twice: what a project's report attributes to a site is part of
/// the same figure here, and a housing rent is taken once for the whole
/// accommodation, not once per site plus once for the vacant days.
/// </para>
/// </remarks>
public record GetCompanyCostsQuery : IRequest<CompanyCostsDto>
{
    public const int MaxDays = 732;

    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>
    /// Set only by code inside the application that turns the amounts into
    /// something that is not money (<c>GetFinanceStatisticsQuery</c>). Internal,
    /// so a request from outside can never bind it.
    /// </summary>
    internal bool SkipFinanceCheck { get; init; }
}

public class GetCompanyCostsQueryValidator : AbstractValidator<GetCompanyCostsQuery>
{
    public GetCompanyCostsQueryValidator()
    {
        RuleFor(x => x.From).NotEqual(default(DateOnly)).WithMessage("A start date is required.");

        RuleFor(x => x.To)
            .NotEqual(default(DateOnly)).WithMessage("An end date is required.")
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("The end of the period must not be before its start.")
            .Must((query, to) => to.DayNumber - query.From.DayNumber + 1 <= GetCompanyCostsQuery.MaxDays)
            .WithMessage($"The period must not exceed {GetCompanyCostsQuery.MaxDays} days.")
            .When(x => x.From != default);
    }
}

public class GetCompanyCostsQueryHandler : IRequestHandler<GetCompanyCostsQuery, CompanyCostsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public GetCompanyCostsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<CompanyCostsDto> Handle(GetCompanyCostsQuery request, CancellationToken cancellationToken)
    {
        var role = _currentUserService.Role;

        if (!CostRules.CanSeeSpending(role))
        {
            throw new ForbiddenAccessException("You may not see cost reports.");
        }

        // The company-wide total is the one figure the dashboard widgets and
        // the cost pages share, so it sits behind the finance right.
        if (!request.SkipFinanceCheck)
        {
            await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);
        }

        var includesLabour = CostRules.CanSeeLabourCost(role);
        var from = request.From;
        var to = request.To;

        // Pay: clocked hours as the project report prices them, plus pay entered by
        // hand for anyone — attached to a project or not.
        var labour = 0m;
        var unpriced = 0;

        if (includesLabour)
        {
            var entries = await ProjectLabourPricing.LoadAsync(_context, from, to, null, cancellationToken);

            labour = entries.Sum(e => e.Cost);
            unpriced = entries.Sum(e => e.UnpricedMinutes);
        }

        var manualPay = includesLabour
            ? await _context.FinanceEntries
                .AsNoTracking()
                .Where(f => f.OccurredOn >= from && f.OccurredOn <= to)
                .SumAsync(f => (decimal?)f.Amount, cancellationToken) ?? 0m
            : 0m;

        var material = await _context.MaterialMovements
            .AsNoTracking()
            .Where(m => m.Kind == MaterialMovementKind.Out && m.UnitPrice != null)
            .Where(m => m.OccurredOn >= from && m.OccurredOn <= to)
            .SumAsync(m => (decimal?)(m.UnitPrice!.Value * m.Quantity), cancellationToken) ?? 0m;

        var general = await _context.GeneralExpenses
            .AsNoTracking()
            .Where(e => e.OccurredOn >= from && e.OccurredOn <= to)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        var accommodation = await LoadAccommodationAsync(from, to, cancellationToken);

        // The fleet and the tools already have their own reports, so the same figures
        // the Costs pages show are the ones counted here.
        var vehicles = (await _sender.Send(new GetVehicleCostsQuery { From = from, To = to, SkipFinanceCheck = true }, cancellationToken)).Total;
        var tools = (await _sender.Send(new GetToolCostsQuery { From = from, To = to, SkipFinanceCheck = true }, cancellationToken)).Total;

        var labourRounded = decimal.Round(labour, 2);
        var manualRounded = decimal.Round(manualPay, 2);

        return new CompanyCostsDto
        {
            From = from,
            To = to,
            IncludesLabour = includesLabour,
            Labour = labourRounded,
            ManualPay = manualRounded,
            Material = decimal.Round(material, 2),
            GeneralExpenses = decimal.Round(general, 2),
            Accommodation = decimal.Round(accommodation, 2),
            Vehicles = decimal.Round(vehicles, 2),
            Tools = decimal.Round(tools, 2),
            UnpricedMinutes = unpriced,
            Total = labourRounded + manualRounded + decimal.Round(material, 2) + decimal.Round(general, 2)
                + decimal.Round(accommodation, 2) + decimal.Round(vehicles, 2) + decimal.Round(tools, 2),
        };
    }

    /// <summary>The rent of every accommodation for the period, worked out as the accommodation pages do.</summary>
    private async Task<decimal> LoadAccommodationAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var rates = await _context.AccommodationRates
            .AsNoTracking()
            .Where(r => r.StartDate <= to && (r.EndDate == null || r.EndDate >= from))
            .ToListAsync(cancellationToken);

        if (rates.Count == 0)
        {
            return 0m;
        }

        var accommodationIds = rates.Select(r => r.AccommodationId).Distinct().ToList();

        var stays = await _context.AccommodationStays
            .AsNoTracking()
            .Where(s => accommodationIds.Contains(s.AccommodationId)
                && s.StartDate <= to
                && (s.EndDate == null || s.EndDate >= from))
            .ToListAsync(cancellationToken);

        var none = new Dictionary<Guid, string>();

        return accommodationIds.Sum(id => AccommodationCostCalculator.Calculate(
            rates.Where(r => r.AccommodationId == id).ToList(),
            stays.Where(s => s.AccommodationId == id).ToList(),
            from,
            to,
            none,
            none).Total);
    }
}
