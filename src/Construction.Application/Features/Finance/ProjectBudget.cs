using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Finance;

/// <summary>What a site is expected to cost and what its customer pays.</summary>
public class ProjectBudgetDto
{
    public Guid ProjectId { get; init; }

    /// <summary>What the customer pays under the contract, when one is set.</summary>
    public decimal? ContractValue { get; init; }

    /// <summary>What the office plans to spend on the site, when it has set a figure.</summary>
    public decimal? Budget { get; init; }
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

        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == request.ProjectId)
            .Select(p => new ProjectBudgetDto { ProjectId = p.Id, ContractValue = p.ContractValue, Budget = p.Budget })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);
    }
}

/// <summary>Sets, or with null clears, a site's planned spending.</summary>
public record SetProjectBudgetCommand : IRequest<ProjectBudgetDto>
{
    public Guid ProjectId { get; init; }

    public decimal? Budget { get; init; }
}

public class SetProjectBudgetCommandValidator : AbstractValidator<SetProjectBudgetCommand>
{
    public SetProjectBudgetCommandValidator()
    {
        RuleFor(x => x.Budget)
            .GreaterThanOrEqualTo(0).WithMessage("A budget cannot be negative.")
            .When(x => x.Budget is not null);
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
        await _context.SaveChangesAsync(cancellationToken);

        return new ProjectBudgetDto { ProjectId = project.Id, ContractValue = project.ContractValue, Budget = project.Budget };
    }
}
