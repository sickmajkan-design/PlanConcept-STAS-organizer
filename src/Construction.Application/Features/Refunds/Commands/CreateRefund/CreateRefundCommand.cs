using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Refunds.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Refunds.Commands.CreateRefund;

/// <summary>Asks to be paid back for something bought for the firm. The receipt is attached afterwards.</summary>
public record CreateRefundCommand : IRequest<RefundDto>
{
    public decimal Amount { get; init; }

    public string Currency { get; init; } = "EUR";

    public DateOnly ExpenseDate { get; init; }

    /// <summary>Why the firm should pay it back.</summary>
    public string Description { get; init; } = null!;

    public Guid? ProjectId { get; init; }
}

public class CreateRefundCommandValidator : AbstractValidator<CreateRefundCommand>
{
    public CreateRefundCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(RefundRules.MaxAmount);
        RuleFor(x => x.Currency).NotEmpty().Length(3).Matches("^[A-Za-z]{3}$");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Say why the firm should pay it back.").MaximumLength(1000);

        RuleFor(x => x.ExpenseDate)
            .NotEqual(default(DateOnly)).WithMessage("The day it was spent is required.")
            .LessThanOrEqualTo(today.AddDays(1)).WithMessage("The expense cannot be in the future.")
            .GreaterThanOrEqualTo(today.AddDays(-RefundRules.MaxBackdatingDays))
            .WithMessage($"An expense older than {RefundRules.MaxBackdatingDays} days cannot be asked for here.");
    }
}

public class CreateRefundCommandHandler : IRequestHandler<CreateRefundCommand, RefundDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notifications;

    public CreateRefundCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _notifications = notifications;
    }

    public async Task<RefundDto> Handle(CreateRefundCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException("Sign in to ask to be paid back.");

        // Paid through the payroll, so it has to be somebody on the payroll.
        var employeeId = _currentUserService.EmployeeId
            ?? throw new ForbiddenAccessException(
                "This account is not linked to an employee, so it cannot ask to be paid back.");

        if (request.ProjectId is { } projectId
            && !await _context.Projects.AnyAsync(p => p.Id == projectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Project), projectId);
        }

        var refund = new Refund
        {
            EmployeeId = employeeId,
            RequestedByUserId = userId,
            ProjectId = request.ProjectId,
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            ExpenseDate = request.ExpenseDate,
            Description = request.Description.Trim(),
        };

        _context.Refunds.Add(refund);
        await _context.SaveChangesAsync(cancellationToken);

        var dto = await _context.Refunds
            .AsNoTracking()
            .Where(r => r.Id == refund.Id)
            .Select(RefundMapping.Projection)
            .FirstAsync(cancellationToken);

        var recipients = await _context.Users
            .Where(u => u.IsActive
                && u.Id != userId
                && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin || u.Role == UserRole.ProjectManager))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        await _notifications.NotifyUsersAsync(
            recipients,
            NotificationType.RefundRequested,
            "Request to be paid back",
            $"{dto.EmployeeName} asked to be paid back {dto.Amount:0.00} {dto.Currency}.",
            new Dictionary<string, string>
            {
                ["refundId"] = dto.Id.ToString(),
                ["employeeName"] = dto.EmployeeName,
                ["amount"] = dto.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                ["currency"] = dto.Currency
            },
            cancellationToken: cancellationToken);

        return dto;
    }
}
