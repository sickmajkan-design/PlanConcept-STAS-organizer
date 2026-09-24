using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Queries.GetCompanyCosts;
using Construction.Application.Features.Costs.Queries.GetProjectCosts;
using FluentValidation;
using MediatR;

namespace Construction.Application.Features.Finance;

public class FinanceBreakdownItemDto
{
    public FinanceBreakdownItemDto(string kind, decimal amount)
    {
        Kind = kind;
        Amount = amount;
    }

    /// <summary>"Labour", "ManualPay", "Material", "GeneralExpenses", "Accommodation", "Vehicles" or "Tools".</summary>
    public string Kind { get; }

    public decimal Amount { get; }
}

public class FinanceBreakdownDto
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    /// <summary>Set when the breakdown is one project's; null for the whole company.</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>False when the caller may not see pay rates, so labour and pay are zero.</summary>
    public bool IncludesLabour { get; init; }

    /// <summary>What the spending was on, in a fixed order. Every kind is here, zero or not.</summary>
    public IReadOnlyList<FinanceBreakdownItemDto> Items { get; init; } = [];

    /// <summary>The sum of <see cref="Items"/> — and so of the figure the same period's overview shows.</summary>
    public decimal Total { get; init; }
}

/// <summary>What the company's — or one project's — spending in a period was on.</summary>
/// <remarks>
/// The whole company's breakdown is the company cost report's own kinds. A
/// project's is the project cost report's, which has no fleet or tools: those
/// belong to no site (see <c>GetProjectCostsQuery</c>). Each kind is rounded
/// as the reports round it and the total is their sum, so the pieces of a
/// chart can never add up to a different figure than the one shown beside it.
/// </remarks>
public record GetFinanceBreakdownQuery : IRequest<FinanceBreakdownDto>
{
    public DateOnly From { get; init; }

    public DateOnly To { get; init; }

    public Guid? ProjectId { get; init; }
}

public class GetFinanceBreakdownQueryValidator : AbstractValidator<GetFinanceBreakdownQuery>
{
    public GetFinanceBreakdownQueryValidator()
    {
        RuleFor(x => x.From).NotEqual(default(DateOnly)).WithMessage("A start date is required.");

        RuleFor(x => x.To)
            .NotEqual(default(DateOnly)).WithMessage("An end date is required.")
            .GreaterThanOrEqualTo(x => x.From).WithMessage("The end of the period must not be before its start.")
            .Must((query, to) => to.DayNumber - query.From.DayNumber + 1 <= GetCompanyCostsQuery.MaxDays)
            .WithMessage($"The period must not exceed {GetCompanyCostsQuery.MaxDays} days.")
            .When(x => x.From != default);
    }
}

public class GetFinanceBreakdownQueryHandler : IRequestHandler<GetFinanceBreakdownQuery, FinanceBreakdownDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public GetFinanceBreakdownQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<FinanceBreakdownDto> Handle(GetFinanceBreakdownQuery request, CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);

        if (request.ProjectId is { } projectId)
        {
            var report = await _sender.Send(
                new GetProjectCostsQuery { From = request.From, To = request.To, ProjectId = projectId },
                cancellationToken);

            // A project nothing was spent on has no row: all zeroes, not an error.
            var row = report.Rows.FirstOrDefault(r => r.ProjectId == projectId);

            return Build(request, report.IncludesLabour,
            [
                new("Labour", row?.LabourCost ?? 0m),
                new("ManualPay", row?.ManualPayAmount ?? 0m),
                new("Material", row?.MaterialCost ?? 0m),
                new("GeneralExpenses", row?.GeneralExpenseCost ?? 0m),
                new("Accommodation", row?.AccommodationCost ?? 0m),
                new("Vehicles", 0m),
                new("Tools", 0m),
            ]);
        }

        var costs = await _sender.Send(
            new GetCompanyCostsQuery { From = request.From, To = request.To },
            cancellationToken);

        return Build(request, costs.IncludesLabour,
        [
            new("Labour", costs.Labour),
            new("ManualPay", costs.ManualPay),
            new("Material", costs.Material),
            new("GeneralExpenses", costs.GeneralExpenses),
            new("Accommodation", costs.Accommodation),
            new("Vehicles", costs.Vehicles),
            new("Tools", costs.Tools),
        ]);
    }

    private static FinanceBreakdownDto Build(
        GetFinanceBreakdownQuery request,
        bool includesLabour,
        List<FinanceBreakdownItemDto> items) => new()
    {
        From = request.From,
        To = request.To,
        ProjectId = request.ProjectId,
        IncludesLabour = includesLabour,
        Items = items,
        Total = items.Sum(i => i.Amount),
    };
}
