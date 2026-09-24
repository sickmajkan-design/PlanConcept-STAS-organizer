using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Finance;

/// <summary>What a site is expected to cost, what its customer pays, and when to be warned.</summary>
public class ProjectBudgetDto
{
    public Guid ProjectId { get; init; }

    /// <summary>What the customer pays under the contract, when one is set.</summary>
    public decimal? ContractValue { get; init; }

    /// <summary>What the office plans to spend on the site, when it has set a figure.</summary>
    public decimal? Budget { get; init; }

    /// <summary>What spending is measured against: "Budget", "Contract", or null to use the budget when there is one and the contract otherwise.</summary>
    public string? AlertBasis { get; init; }

    /// <summary>The share of it, in percent, at which to start warning. Null means <see cref="BudgetAlertRules.DefaultWarnPercent"/>.</summary>
    public int? WarnPercent { get; init; }
}

/// <summary>Which figure a project's spending is measured against, and where the warning starts.</summary>
public static class BudgetAlertRules
{
    public const int DefaultWarnPercent = 80;

    /// <summary>At this share of the limit the project is over it.</summary>
    public const int OverPercent = 100;

    /// <summary>The basis in force and the figure it names — null when there is nothing to measure against.</summary>
    public static (BudgetAlertBasis Basis, decimal Limit)? Resolve(
        BudgetAlertBasis? chosen,
        decimal? budget,
        decimal? contractValue)
    {
        var basis = chosen ?? (budget is > 0 ? BudgetAlertBasis.Budget : BudgetAlertBasis.Contract);
        var limit = basis == BudgetAlertBasis.Budget ? budget : contractValue;

        return limit is > 0 ? (basis, limit.Value) : null;
    }

    public static ProjectBudgetDto ToDto(Project p) => new()
    {
        ProjectId = p.Id,
        ContractValue = p.ContractValue,
        Budget = p.Budget,
        AlertBasis = p.BudgetAlertBasis?.ToString(),
        WarnPercent = p.BudgetWarnPercent,
    };
}

public record GetProjectBudgetQuery(Guid ProjectId) : IRequest<ProjectBudgetDto>;

public class GetProjectBudgetQueryHandler : IRequestHandler<GetProjectBudgetQuery, ProjectBudgetDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetProjectBudgetQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ProjectBudgetDto> Handle(GetProjectBudgetQuery request, CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);

        var project = await _context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        return BudgetAlertRules.ToDto(project);
    }
}

/// <summary>Sets a site's planned spending and how the office wants to be warned about it. Every field is replaced; null clears it.</summary>
public record SetProjectBudgetCommand : IRequest<ProjectBudgetDto>
{
    public Guid ProjectId { get; init; }

    public decimal? Budget { get; init; }

    public BudgetAlertBasis? AlertBasis { get; init; }

    public int? WarnPercent { get; init; }
}

public class SetProjectBudgetCommandValidator : AbstractValidator<SetProjectBudgetCommand>
{
    public SetProjectBudgetCommandValidator()
    {
        RuleFor(x => x.Budget)
            .GreaterThanOrEqualTo(0).WithMessage("A budget cannot be negative.")
            .When(x => x.Budget is not null);

        RuleFor(x => x.AlertBasis).IsInEnum().When(x => x.AlertBasis is not null);

        // Measuring against a budget that is not there would never warn about anything.
        RuleFor(x => x.Budget)
            .Must(budget => budget is > 0)
            .WithMessage("Set a budget to measure spending against it.")
            .When(x => x.AlertBasis == BudgetAlertBasis.Budget);

        RuleFor(x => x.WarnPercent)
            .InclusiveBetween(1, BudgetAlertRules.OverPercent - 1)
            .WithMessage($"The warning must start between 1% and {BudgetAlertRules.OverPercent - 1}%.")
            .When(x => x.WarnPercent is not null);
    }
}

public class SetProjectBudgetCommandHandler : IRequestHandler<SetProjectBudgetCommand, ProjectBudgetDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SetProjectBudgetCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ProjectBudgetDto> Handle(SetProjectBudgetCommand request, CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        project.Budget = request.Budget;
        project.BudgetAlertBasis = request.AlertBasis;
        project.BudgetWarnPercent = request.WarnPercent;
        await _context.SaveChangesAsync(cancellationToken);

        return BudgetAlertRules.ToDto(project);
    }
}
