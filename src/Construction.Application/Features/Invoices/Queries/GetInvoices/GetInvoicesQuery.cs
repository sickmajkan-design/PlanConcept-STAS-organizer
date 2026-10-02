using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Models;
using Construction.Application.Common.Security;
using Construction.Application.Features.Invoices.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Invoices.Queries.GetInvoices;

/// <summary>Lists issued invoices by site, client, company, payroll month and status.</summary>
public record GetInvoicesQuery : ISortablePagedQuery, IRequest<PagedList<InvoiceDto>>
{
    public static readonly string[] AllowedSortFields = ["issueDate", "amount", "number", "status", "createdAt"];

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public Guid? ProjectId { get; init; }

    /// <summary>Restricts results to records of projects in this business unit.</summary>
    public Guid? BranchId { get; init; }

    public Guid? CustomerId { get; init; }

    /// <summary>Only invoices with a part for this company.</summary>
    public Guid? CustomerCompanyId { get; init; }

    public int? PayrollYear { get; init; }

    public int? PayrollMonth { get; init; }

    public InvoiceStatus? Status { get; init; }

    /// <summary>Part of the invoice number.</summary>
    public string? Search { get; init; }

    public string? SortBy { get; init; }

    public bool SortDescending { get; init; } = true;
}

public class GetInvoicesQueryValidator : SortablePagedQueryValidator<GetInvoicesQuery>
{
    public GetInvoicesQueryValidator()
        : base(GetInvoicesQuery.AllowedSortFields)
    {
    }
}

public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, PagedList<InvoiceDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetInvoicesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedList<InvoiceDto>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        await InvoiceRules.EnsureAllowedAsync(_context, _currentUserService, cancellationToken);

        var query = _context.Invoices.AsNoTracking();

        if (request.BranchId is { } branchId)
        {
            query = query.Where(i => i.Project.BranchId == branchId);
        }

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(i => i.ProjectId == projectId);
        }

        if (request.CustomerId is { } customerId)
        {
            query = query.Where(i => i.Project.CustomerId == customerId);
        }

        if (request.CustomerCompanyId is { } companyId)
        {
            query = query.Where(i => i.Shares.Any(s => s.CustomerCompanyId == companyId));
        }

        if (request.PayrollYear is { } year)
        {
            query = query.Where(i => i.PayrollYear == year);
        }

        if (request.PayrollMonth is { } month)
        {
            query = query.Where(i => i.PayrollMonth == month);
        }

        if (request.Status is { } status)
        {
            query = query.Where(i => i.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = SearchPattern.Contains(request.Search);
            query = query.Where(i => EF.Functions.Like(i.Number.ToLower(), pattern, SearchPattern.Escape));
        }

        IOrderedQueryable<Invoice> ordered = (request.SortBy?.ToLowerInvariant(), request.SortDescending) switch
        {
            ("amount", false) => query.OrderBy(i => i.Amount),
            ("amount", true) => query.OrderByDescending(i => i.Amount),
            ("number", false) => query.OrderBy(i => i.Number),
            ("number", true) => query.OrderByDescending(i => i.Number),
            ("status", false) => query.OrderBy(i => i.Status),
            ("status", true) => query.OrderByDescending(i => i.Status),
            ("createdat", false) => query.OrderBy(i => i.CreatedAt),
            ("createdat", true) => query.OrderByDescending(i => i.CreatedAt),
            (_, false) => query.OrderBy(i => i.IssueDate),
            _ => query.OrderByDescending(i => i.IssueDate),
        };

        return await PagedList<InvoiceDto>.CreateAsync(
            ordered.ThenBy(i => i.Id).Select(InvoiceMapping.Projection),
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}
