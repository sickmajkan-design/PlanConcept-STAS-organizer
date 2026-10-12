using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Invoices.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Invoices.Commands.ChangeInvoiceStatus;

/// <summary>Marks an issued invoice as paid.</summary>
public record MarkInvoicePaidCommand(Guid Id) : IRequest<InvoiceDto>;

/// <summary>
/// Withdraws an invoice that was issued by mistake. A paid invoice is not withdrawn; it is answered
/// with a credit note (an invoice with a negative amount).
/// </summary>
public record CancelInvoiceCommand : IRequest<InvoiceDto>
{
    public Guid Id { get; init; }

    public string Reason { get; init; } = null!;
}

public class CancelInvoiceCommandValidator : AbstractValidator<CancelInvoiceCommand>
{
    public CancelInvoiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required to cancel an invoice.")
            .MaximumLength(500);
    }
}

public class ChangeInvoiceStatusHandlers :
    IRequestHandler<MarkInvoicePaidCommand, InvoiceDto>,
    IRequestHandler<CancelInvoiceCommand, InvoiceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeInvoiceStatusHandlers(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<InvoiceDto> Handle(MarkInvoicePaidCommand request, CancellationToken cancellationToken)
    {
        var invoice = await LoadAsync(request.Id, cancellationToken);

        if (invoice.Status != InvoiceStatus.Issued)
        {
            throw new ConflictException($"Only an issued invoice can be marked as paid; this one is {invoice.Status}.");
        }

        // What is still open is recorded as a payment of today, so the paid amount stays the sum of the
        // payments whichever way an invoice came to be paid.
        var paid = await _context.InvoicePayments
            .Where(p => p.InvoiceId == invoice.Id)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var remaining = invoice.Amount - paid;

        if (invoice.Amount > 0 && remaining > 0)
        {
            _context.InvoicePayments.Add(new InvoicePayment
            {
                InvoiceId = invoice.Id,
                Amount = remaining,
                PaidOn = DateOnly.FromDateTime(_dateTimeProvider.UtcNow),
                RecordedByUserId = _currentUserService.UserId,
            });
        }

        invoice.Status = InvoiceStatus.Paid;

        return await SaveAsync(invoice, cancellationToken);
    }

    public async Task<InvoiceDto> Handle(CancelInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await LoadAsync(request.Id, cancellationToken);

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new ConflictException("This invoice is already cancelled.");
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            throw new ConflictException("A paid invoice cannot be cancelled. Record a credit note instead.");
        }

        if (await _context.InvoicePayments.AnyAsync(p => p.InvoiceId == invoice.Id, cancellationToken))
        {
            throw new ConflictException(
                "Part of this invoice has been paid. Remove the payments first, or record a credit note.");
        }

        invoice.Status = InvoiceStatus.Cancelled;
        invoice.CancelReason = request.Reason.Trim();

        return await SaveAsync(invoice, cancellationToken);
    }

    private async Task<Invoice> LoadAsync(Guid id, CancellationToken cancellationToken)
    {
        await InvoiceRules.EnsureAllowedAsync(_context, _currentUserService, cancellationToken);

        return await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), id);
    }

    private async Task<InvoiceDto> SaveAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This invoice was changed by someone else just now. Reload it and try again.");
        }

        return await _context.Invoices
            .AsNoTracking()
            .Where(i => i.Id == invoice.Id)
            .Select(InvoiceMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
