using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Branches;
using Construction.Application.Features.Customers;
using Construction.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Invoices.Queries.GetInvoiceDocument;

/// <summary>One company the invoice is addressed to, with what a printed copy needs to name it.</summary>
public class InvoiceDocumentRecipientDto
{
    /// <summary>The client itself when it has no companies of its own.</summary>
    public string Name { get; init; } = null!;

    public string? Address { get; init; }

    /// <summary>The client's tax numbers; null unless the caller may see tax details.</summary>
    public string? TaxId { get; init; }

    public string? RegistrationNumber { get; init; }

    public string? VatNumber { get; init; }

    public decimal Amount { get; init; }
}

/// <summary>Everything a printed copy of a recorded invoice shows.</summary>
public class InvoiceDocumentDto
{
    public Guid InvoiceId { get; init; }

    public string Number { get; init; } = null!;

    public DateOnly IssueDate { get; init; }

    public DateOnly? DueDate { get; init; }

    public string? Description { get; init; }

    public decimal Amount { get; init; }

    /// <summary>"Issued", "Paid" or "Cancelled".</summary>
    public string Status { get; init; } = null!;

    public string? CancelReason { get; init; }

    public int PayrollYear { get; init; }

    public int PayrollMonth { get; init; }

    public string ProjectName { get; init; } = null!;

    public string? ProjectAddress { get; init; }

    public string? CustomerName { get; init; }

    public string? CustomerContactPerson { get; init; }

    public IssuerDto Issuer { get; init; } = null!;

    public IReadOnlyList<InvoiceDocumentRecipientDto> Recipients { get; init; } = [];
}

/// <summary>
/// The data of a printed copy of one recorded invoice, issuer included. The program records
/// invoices; it does not issue them, so this is an extract of the record, not the invoice itself.
/// </summary>
public record GetInvoiceDocumentQuery(Guid InvoiceId) : IRequest<InvoiceDocumentDto>;

public class GetInvoiceDocumentQueryHandler : IRequestHandler<GetInvoiceDocumentQuery, InvoiceDocumentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetInvoiceDocumentQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<InvoiceDocumentDto> Handle(GetInvoiceDocumentQuery request, CancellationToken cancellationToken)
    {
        await InvoiceRules.EnsureAllowedAsync(_context, _currentUserService, cancellationToken);

        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Project).ThenInclude(p => p.Customer)
            .Include(i => i.Shares).ThenInclude(s => s.CustomerCompany)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.InvoiceId);

        // The issuer's and the client's tax numbers are on the document, so they follow the same
        // rule as everywhere else: shown only to who may see them.
        var taxDetails = await CustomerRules.ResolveCanViewTaxDetailsAsync(
            _context, _currentUserService, cancellationToken);

        var issuer = await BranchIssuer.ResolveAsync(
            _context, invoice.Project.BranchId, taxDetails, cancellationToken);

        var customer = invoice.Project.Customer;

        var recipients = invoice.Shares
            .OrderBy(s => s.CustomerCompany?.Name ?? string.Empty)
            .Select(s => new InvoiceDocumentRecipientDto
            {
                Name = s.CustomerCompany?.Name ?? customer?.Name ?? string.Empty,
                Address = s.CustomerCompany?.Address,
                // A client's companies carry a name and an address; the tax numbers are the client's.
                TaxId = taxDetails ? customer?.TaxId : null,
                RegistrationNumber = taxDetails ? customer?.RegistrationNumber : null,
                VatNumber = taxDetails ? customer?.VatNumber : null,
                Amount = s.Amount,
            })
            .ToList();

        return new InvoiceDocumentDto
        {
            InvoiceId = invoice.Id,
            Number = invoice.Number,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            Description = invoice.Description,
            Amount = invoice.Amount,
            Status = invoice.Status.ToString(),
            CancelReason = invoice.CancelReason,
            PayrollYear = invoice.PayrollYear,
            PayrollMonth = invoice.PayrollMonth,
            ProjectName = invoice.Project.Name,
            ProjectAddress = invoice.Project.Address,
            CustomerName = customer?.Name,
            CustomerContactPerson = customer?.ContactPerson,
            Issuer = issuer,
            Recipients = recipients,
        };
    }
}
