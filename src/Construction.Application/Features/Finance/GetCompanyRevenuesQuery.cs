using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Finance;

/// <summary>Lists money received that belongs to no project, newest first.</summary>
public record GetCompanyRevenuesQuery : IPagedQuery, IRequest<PagedList<CompanyRevenueDto>>
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 25;

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public CompanyRevenueSource? Source { get; init; }
}

public class GetCompanyRevenuesQueryValidator : PagedQueryValidator<GetCompanyRevenuesQuery>
{
    public GetCompanyRevenuesQueryValidator() : base(maxPageSize: 200)
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From is not null && x.To is not null);
    }
}

public class GetCompanyRevenuesQueryHandler : IRequestHandler<GetCompanyRevenuesQuery, PagedList<CompanyRevenueDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCompanyRevenuesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<CompanyRevenueDto>> Handle(
        GetCompanyRevenuesQuery request,
        CancellationToken cancellationToken)
    {
        await FinanceRules.EnsureFullAsync(_context, _currentUserService, cancellationToken);

        var query = _context.CompanyRevenues.AsNoTracking();

        if (request.From is { } from)
        {
            query = query.Where(r => r.OccurredOn >= from);
        }

        if (request.To is { } to)
        {
            query = query.Where(r => r.OccurredOn <= to);
        }

        if (request.Source is { } source)
        {
            query = query.Where(r => r.Source == source);
        }

        return await PagedList<CompanyRevenueDto>.CreateAsync(
            query.OrderByDescending(r => r.OccurredOn)
                .ThenByDescending(r => r.CreatedAt)
                .Select(CompanyRevenueMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
