using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Refunds.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Refunds.Commands.ReviewRefund;

/// <summary>Approves a request to be paid back, or declines it, or lets the person withdraw their own.</summary>
public record ReviewRefundCommand : IRequest<RefundDto>
{
    public Guid Id { get; init; }

    /// <summary><see cref="RefundStatus.Approved"/>, <see cref="RefundStatus.Rejected"/> or <see cref="RefundStatus.Cancelled"/>.</summary>
    public RefundStatus Status { get; init; }

    /// <summary>Required when declining, so the person knows why.</summary>
    public string? Note { get; init; }

    /// <summary>The payroll month to pay it with. Omitted means the month it is approved in.</summary>
    public int? PayrollYear { get; init; }

    public int? PayrollMonth { get; init; }
}

public class ReviewRefundCommandValidator : AbstractValidator<ReviewRefundCommand>
{
    public ReviewRefundCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Status)
            .Must(s => s is RefundStatus.Approved or RefundStatus.Rejected or RefundStatus.Cancelled)
            .WithMessage("A request can be approved, declined or withdrawn.");

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("A reason is required when declining.")
            .When(x => x.Status == RefundStatus.Rejected);
        RuleFor(x => x.Note).MaximumLength(1000);

        RuleFor(x => x.PayrollMonth).InclusiveBetween(1, 12).When(x => x.PayrollMonth is not null);
        RuleFor(x => x.PayrollYear).InclusiveBetween(2000, 2100).When(x => x.PayrollYear is not null);
        RuleFor(x => x)
            .Must(x => (x.PayrollYear is null) == (x.PayrollMonth is null))
            .WithMessage("Give both the payroll year and month, or neither.")
            .OverridePropertyName(nameof(ReviewRefundCommand.PayrollMonth));
    }
}

public class ReviewRefundCommandHandler : IRequestHandler<ReviewRefundCommand, RefundDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notifications;

    public ReviewRefundCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _notifications = notifications;
    }

    public async Task<RefundDto> Handle(ReviewRefundCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException("Sign in to change a request.");

        var refund = await _context.Refunds
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Refund), request.Id);

        var own = refund.RequestedByUserId == userId;

        if (request.Status == RefundStatus.Cancelled)
        {
            if (!own)
            {
                throw new ForbiddenAccessException("Only the person who asked can withdraw the request.");
            }
        }
        else
        {
            if (!RefundRules.CanReview(_currentUserService.Role))
            {
                throw new ForbiddenAccessException("You may not decide on requests to be paid back.");
            }

            // Nobody approves their own money, whatever their role.
            if (own)
            {
                throw new ForbiddenAccessException("You cannot decide on your own request.");
            }
        }

        if (refund.Status != RefundStatus.Requested)
        {
            throw new ConflictException("This request has already been decided. Reload it and try again.");
        }

        var now = _dateTimeProvider.UtcNow;

        refund.Status = request.Status;

        if (request.Status != RefundStatus.Cancelled)
        {
            refund.ReviewedByUserId = userId;
            refund.ReviewedAt = now;
        }

        refund.ReviewNote = request.Status == RefundStatus.Rejected ? request.Note!.Trim() : null;

        if (request.Status == RefundStatus.Approved)
        {
            refund.PayrollYear = request.PayrollYear ?? now.Year;
            refund.PayrollMonth = request.PayrollMonth ?? now.Month;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("This request was changed by someone else just now. Reload it and try again.");
        }

        if (request.Status != RefundStatus.Cancelled)
        {
            await _notifications.NotifyUserAsync(
                refund.RequestedByUserId,
                NotificationType.RefundDecided,
                request.Status == RefundStatus.Approved ? "Refund approved" : "Refund declined",
                request.Status == RefundStatus.Approved
                    ? $"Your request for {refund.Amount:0.00} {refund.Currency} was approved."
                    : $"Your request for {refund.Amount:0.00} {refund.Currency} was declined: {refund.ReviewNote}",
                new Dictionary<string, string>
                {
                    ["refundId"] = refund.Id.ToString(),
                    ["decision"] = request.Status.ToString(),
                    ["amount"] = refund.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    ["currency"] = refund.Currency,
                    ["note"] = refund.ReviewNote ?? string.Empty
                },
                cancellationToken: cancellationToken);
        }

        return await _context.Refunds
            .AsNoTracking()
            .Where(r => r.Id == refund.Id)
            .Select(RefundMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
