using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Invoices.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Invoices.Commands.InvoicePayments;

/// <summary>
/// Records money a client paid against an invoice, in whole or in part. The invoice becomes paid when what was
/// paid reaches its amount. A payment larger than what is still open is refused.
/// </summary>
public record AddInvoicePaymentCommand : IRequest<InvoiceDto>
{
    /// <summary>Taken from the route.</summary>
    public Guid InvoiceId { get; init; }

    public decimal Amount { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? PaidOn { get; init; }

    public BookPaymentMethod? Method { get; init; }

    public string? Note { get; init; }
}

public class AddInvoicePaymentCommandValidator : AbstractValidator<AddInvoicePaymentCommand>
{
    public AddInvoicePaymentCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("A payment must be more than zero.");

        RuleFor(x => x.PaidOn)
            .LessThanOrEqualTo(today).WithMessage("A payment cannot be dated in the future.")
            .When(x => x.PaidOn is not null);

        RuleFor(x => x.Method).IsInEnum().When(x => x.Method is not null);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public class AddInvoicePaymentCommandHandler : IRequestHandler<AddInvoicePaymentCommand, InvoiceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AddInvoicePaymentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<InvoiceDto> Handle(AddInvoicePaymentCommand request, CancellationToken cancellationToken)
    {
        await InvoiceRules.EnsureAllowedAsync(_context, _currentUserService, cancellationToken);

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.InvoiceId);

        if (invoice.Status != InvoiceStatus.Issued)
        {
            throw new ConflictException($"Only an issued invoice can be paid; this one is {invoice.Status}.");
        }

        if (invoice.Amount <= 0)
        {
            throw new ConflictException("A credit note is not paid; it is set off against an invoice.");
        }

        var paid = await _context.InvoicePayments
            .Where(p => p.InvoiceId == invoice.Id)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var remaining = invoice.Amount - paid;

        if (request.Amount > remaining)
        {
            throw new ConflictException(
                $"That is more than what is still open on this invoice ({remaining:0.00}).");
        }

        var paidOn = request.PaidOn ?? DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        if (paidOn < invoice.IssueDate)
        {
            throw new ConflictException("A payment cannot be dated before the invoice was issued.");
        }

        _context.InvoicePayments.Add(new InvoicePayment
        {
            InvoiceId = invoice.Id,
            Amount = request.Amount,
            PaidOn = paidOn,
            Method = request.Method,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            RecordedByUserId = _currentUserService.UserId,
        });

        if (paid + request.Amount >= invoice.Amount)
        {
            invoice.Status = InvoiceStatus.Paid;
        }
        else
        {
            // The invoice row carries the concurrency token; touching it makes two payments recorded at
            // the same moment collide instead of both passing the "still open" check and overpaying.
            invoice.UpdatedAt = _dateTimeProvider.UtcNow;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This invoice was changed by someone else just now. Reload it and try again.");
        }

        return await InvoiceReader.ReadAsync(_context, invoice.Id, cancellationToken);
    }
}

/// <summary>Takes back a payment that was recorded by mistake.</summary>
public record RemoveInvoicePaymentCommand(Guid InvoiceId, Guid PaymentId) : IRequest<InvoiceDto>;

public class RemoveInvoicePaymentCommandHandler : IRequestHandler<RemoveInvoicePaymentCommand, InvoiceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RemoveInvoicePaymentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<InvoiceDto> Handle(RemoveInvoicePaymentCommand request, CancellationToken cancellationToken)
    {
        await InvoiceRules.EnsureAllowedAsync(_context, _currentUserService, cancellationToken);

        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.InvoiceId);

        var payment = await _context.InvoicePayments
            .FirstOrDefaultAsync(
                p => p.Id == request.PaymentId && p.InvoiceId == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(InvoicePayment), request.PaymentId);

        _context.InvoicePayments.Remove(payment);

        var remainingPaid = await _context.InvoicePayments
            .Where(p => p.InvoiceId == invoice.Id && p.Id != payment.Id)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        if (invoice.Status == InvoiceStatus.Paid && remainingPaid < invoice.Amount)
        {
            invoice.Status = InvoiceStatus.Issued;
        }
        else
        {
            invoice.UpdatedAt = _dateTimeProvider.UtcNow;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This invoice was changed by someone else just now. Reload it and try again.");
        }

        return await InvoiceReader.ReadAsync(_context, invoice.Id, cancellationToken);
    }
}

internal static class InvoiceReader
{
    public static Task<InvoiceDto> ReadAsync(
        IApplicationDbContext context, Guid id, CancellationToken cancellationToken) =>
        context.Invoices
            .AsNoTracking()
            .Where(i => i.Id == id)
            .Select(InvoiceMapping.Projection)
            .FirstAsync(cancellationToken);
}
