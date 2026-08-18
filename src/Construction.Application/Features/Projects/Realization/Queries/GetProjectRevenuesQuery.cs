using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Features.Projects.Realization.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Projects.Realization.Queries;

/// <summary>The individual payments the realization plan is built from.</summary>
public record GetProjectRevenuesQuery : IRequest<PagedList<ProjectRevenueDto>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? ProjectId { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}

public class GetProjectRevenuesQueryValidator : AbstractValidator<GetProjectRevenuesQuery>
{
    public GetProjectRevenuesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From is not null && x.To is not null);
    }
}

public class GetProjectRevenuesQueryHandler
    : IRequestHandler<GetProjectRevenuesQuery, PagedList<ProjectRevenueDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProjectRevenuesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedList<ProjectRevenueDto>> Handle(
        GetProjectRevenuesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.ProjectRevenues.AsNoTracking();

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(r => r.ProjectId == projectId);
        }

        if (request.From is { } from)
        {
            query = query.Where(r => r.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(r => r.OccurredOn <= to);
        }

        return await PagedList<ProjectRevenueDto>.CreateAsync(
            query
                .OrderByDescending(r => r.OccurredOn)
                .ThenByDescending(r => r.CreatedAt)
                .Select(ProjectRevenueMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
