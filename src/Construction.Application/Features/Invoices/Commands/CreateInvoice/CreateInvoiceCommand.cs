using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Invoices.Models;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Invoices.Commands.CreateInvoice;

/// <summary>One company's part of an invoice, as written by the person recording it.</summary>
public record InvoiceShareInput
{
    /// <summary>Null means the client itself, which is only allowed when it has no companies.</summary>
    public Guid? CustomerCompanyId { get; init; }

    public decimal Amount { get; init; }
}

/// <summary>
/// Records an invoice the firm issued to a client for a site, on one or more of the client's
/// companies.
/// </summary>
/// <remarks>
/// The parts must add up to the whole. To avoid typing them, name the companies in
/// <see cref="CompanyIds"/> and leave <see cref="Shares"/> empty: the amount is then split evenly
/// to the cent.
/// </remarks>
public record CreateInvoiceCommand : IRequest<InvoiceDto>
{
    public Guid ProjectId { get; init; }

    public string Number { get; init; } = null!;

    public DateOnly IssueDate { get; init; }

    public DateOnly? DueDate { get; init; }

    public string? Description { get; init; }

    /// <summary>The whole invoice. Negative for a credit note; never zero.</summary>
    public decimal Amount { get; init; }

    /// <summary>The payroll month it is counted in. Defaults to the month it was issued.</summary>
    public int? PayrollYear { get; init; }

    public int? PayrollMonth { get; init; }

    /// <summary>Explicit parts. When empty, see <see cref="CompanyIds"/>.</summary>
    public IReadOnlyList<InvoiceShareInput> Shares { get; init; } = [];

    /// <summary>Companies to split the amount evenly among, when <see cref="Shares"/> is empty.</summary>
    public IReadOnlyList<Guid> CompanyIds { get; init; } = [];
}

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    /// <summary>A guard against a slipped decimal point, not a policy.</summary>
    public const decimal MaxAmount = 10_000_000m;

    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();

        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("The invoice number is required.")
            .MaximumLength(64);

        RuleFor(x => x.Description).MaximumLength(1000);

        RuleFor(x => x.Amount)
            .NotEqual(0).WithMessage("An invoice of zero is not an invoice.")
            .InclusiveBetween(-MaxAmount, MaxAmount);

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.IssueDate)
            .WithMessage("The due date cannot be before the issue date.")
            .When(x => x.DueDate is not null);

        RuleFor(x => x.PayrollMonth)
            .InclusiveBetween(1, 12)
            .When(x => x.PayrollMonth is not null);

        RuleFor(x => x.PayrollYear)
            .InclusiveBetween(2000, 2100)
            .When(x => x.PayrollYear is not null);

        RuleFor(x => x)
            .Must(x => (x.PayrollYear is null) == (x.PayrollMonth is null))
            .WithMessage("Give the payroll year and month together, or neither.")
            .OverridePropertyName(nameof(CreateInvoiceCommand.PayrollMonth));

        RuleForEach(x => x.Shares).ChildRules(share =>
            share.RuleFor(s => s.Amount).NotEqual(0).WithMessage("A part of zero is not a part."));

        RuleFor(x => x)
            .Must(x => x.Shares.Count == 0 || x.CompanyIds.Count == 0)
            .WithMessage("Give the parts, or the companies to split evenly among, not both.")
            .OverridePropertyName(nameof(CreateInvoiceCommand.Shares));
    }
}

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, InvoiceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateInvoiceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<InvoiceDto> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        await InvoiceRules.EnsureAllowedAsync(_context, _currentUserService, cancellationToken);

        var project = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == request.ProjectId)
            .Select(p => new { p.Id, p.CustomerId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        var number = request.Number.Trim();

        if (await _context.Invoices.AnyAsync(i => i.Number == number, cancellationToken))
        {
            throw new ConflictException($"An invoice numbered '{number}' already exists.");
        }

        // The companies this client has, which decide what a valid split is.
        var companies = project.CustomerId is { } customerId
            ? await _context.CustomerCompanies
                .AsNoTracking()
                .Where(c => c.CustomerId == customerId)
                .Select(c => new { c.Id, c.IsActive })
                .ToListAsync(cancellationToken)
            : [];

        var parts = ResolveShares(request);

        if (companies.Count == 0)
        {
            if (parts.Any(p => p.CustomerCompanyId is not null))
            {
                throw new ConflictException("This client has no companies to invoice.");
            }
        }
        else
        {
            if (parts.Any(p => p.CustomerCompanyId is null))
            {
                throw new ConflictException("This client has companies: say which of them the invoice is for.");
            }

            foreach (var part in parts)
            {
                var company = companies.FirstOrDefault(c => c.Id == part.CustomerCompanyId)
                    ?? throw new ConflictException("A company named on the invoice does not belong to this client.");

                if (!company.IsActive)
                {
                    throw new ConflictException("A company named on the invoice is no longer active.");
                }
            }
        }

        if (parts.Select(p => p.CustomerCompanyId).Distinct().Count() != parts.Count)
        {
            throw new ConflictException("A company can only appear once on an invoice.");
        }

        var total = parts.Sum(p => p.Amount);

        if (total != request.Amount)
        {
            throw new ConflictException(
                $"The parts add up to {total:0.00}, not the invoice amount {request.Amount:0.00} (difference {request.Amount - total:0.00}).");
        }

        var invoice = new Invoice
        {
            ProjectId = project.Id,
            Number = number,
            IssueDate = request.IssueDate,
            DueDate = request.DueDate,
            Description = request.Description?.Trim(),
            Amount = request.Amount,
            PayrollYear = request.PayrollYear ?? request.IssueDate.Year,
            PayrollMonth = request.PayrollMonth ?? request.IssueDate.Month,
            CreatedByUserId = _currentUserService.UserId,
        };

        foreach (var part in parts)
        {
            invoice.Shares.Add(new InvoiceShare { CustomerCompanyId = part.CustomerCompanyId, Amount = part.Amount });
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Id == invoice.Id)
            .Select(InvoiceMapping.Projection)
            .FirstAsync(cancellationToken);
    }

    /// <summary>The parts as written, or an even split of the amount among the named companies, or the whole for the client itself.</summary>
    private static List<InvoiceShareInput> ResolveShares(CreateInvoiceCommand request)
    {
        if (request.Shares.Count > 0)
        {
            return request.Shares.ToList();
        }

        if (request.CompanyIds.Count > 0)
        {
            var amounts = InvoiceRules.SplitEvenly(request.Amount, request.CompanyIds.Count);

            return request.CompanyIds
                .Select((id, index) => new InvoiceShareInput { CustomerCompanyId = id, Amount = amounts[index] })
                .ToList();
        }

        return [new InvoiceShareInput { CustomerCompanyId = null, Amount = request.Amount }];
    }
}
