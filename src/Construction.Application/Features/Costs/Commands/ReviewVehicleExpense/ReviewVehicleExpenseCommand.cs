using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Commands.ReviewVehicleExpense;

/// <summary>
/// Signs a recorded vehicle cost off, or sends it back with a reason.
/// </summary>
/// <remarks>
/// One command for both outcomes, the same shape as
/// <c>ReviewTimeEntryCommand</c> — they share every rule that matters: who may
/// decide, what a rejection has to explain, and that reversing an earlier
/// decision needs the caller to say they meant it.
/// </remarks>
public record ReviewVehicleExpenseCommand : IRequest<VehicleExpenseDto>
{
    public Guid Id { get; init; }

    public bool Approve { get; init; }

    /// <summary>Required when sending a cost back, so whoever recorded it knows what to fix.</summary>
    public string? Note { get; init; }

    /// <summary>
    /// Required to reverse an earlier decision — approving something already
    /// rejected, or the other way around.
    /// </summary>
    public bool Confirm { get; init; }
}

public class ReviewVehicleExpenseCommandValidator : AbstractValidator<ReviewVehicleExpenseCommand>
{
    public ReviewVehicleExpenseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("A reason is required when sending a cost back.")
            .When(x => !x.Approve);

        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public class ReviewVehicleExpenseCommandHandler
    : IRequestHandler<ReviewVehicleExpenseCommand, VehicleExpenseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notifications;

    public ReviewVehicleExpenseCommandHandler(
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

    public async Task<VehicleExpenseDto> Handle(
        ReviewVehicleExpenseCommand request,
        CancellationToken cancellationToken)
    {
        if (!CostRules.CanReviewSpending(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not review vehicle costs.");
        }

        var reviewerId = _currentUserService.UserId
            ?? throw new ForbiddenAccessException("No signed-in user to record the review against.");

        var expense = await _context.VehicleExpenses
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(VehicleExpense), request.Id);

        // Nobody signs off their own entry — without this a project manager
        // recording their own fuel could approve it themselves, and the
        // review would mean nothing.
        //
        // Except the owner. A SuperAdmin sits at the top of the approval
        // chain: there is nobody above them to send it to, and in a firm where
        // one person records most costs the rule would leave those costs
        // unapprovable by anyone. Every other role, Admin included, still
        // needs a second pair of eyes.
        if (expense.RecordedByUserId is { } recordedByUserId
            && recordedByUserId == reviewerId
            && _currentUserService.Role != UserRole.SuperAdmin)
        {
            throw new ForbiddenAccessException("You cannot review a cost you recorded yourself.");
        }

        if (request.Approve && expense.Status == VehicleExpenseStatus.Approved)
        {
            throw new ConflictException("This cost is already approved.");
        }

        if (!request.Approve && expense.Status == VehicleExpenseStatus.Rejected && !request.Confirm)
        {
            throw new ConflictException(
                "This cost was already sent back. Rejecting it again with a new " +
                "reason overrides the earlier one — confirm to proceed.");
        }

        if (request.Approve && expense.Status == VehicleExpenseStatus.Rejected && !request.Confirm)
        {
            throw new ConflictException(
                $"This cost was sent back: \"{expense.ReviewNote}\". " +
                "Approving it now overrides that decision — confirm to proceed.");
        }

        expense.Status = request.Approve ? VehicleExpenseStatus.Approved : VehicleExpenseStatus.Rejected;
        expense.ReviewedByUserId = reviewerId;
        expense.ReviewedAt = _dateTimeProvider.UtcNow;
        // An approval note would sit on the row looking like an objection.
        expense.ReviewNote = request.Approve ? null : request.Note!.Trim();

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else reviewed, edited, or deleted this cost between this
            // handler reading it and saving.
            throw new ConflictException(
                "This cost was changed by someone else just now. Reload it and try again.");
        }

        var dto = await _context.VehicleExpenses
            .AsNoTracking()
            .Where(e => e.Id == expense.Id)
            .Select(VehicleExpenseMapping.Projection)
            .FirstAsync(cancellationToken);

        // Only a rejection: it is the one outcome that asks something of the
        // person who recorded the cost. An approval needs nothing from them,
        // and a notification for every signed-off fill-up would teach people to
        // stop reading this one.
        if (!request.Approve && expense.RecordedByUserId is { } recorderId)
        {
            await _notifications.NotifyUserAsync(
                recorderId,
                NotificationType.VehicleExpenseRejected,
                "Cost sent back",
                $"{dto.VehicleName} ({dto.OccurredOn:yyyy-MM-dd}) was sent back: {dto.ReviewNote}",
                new Dictionary<string, string>
                {
                    ["expenseId"] = dto.Id.ToString(),
                    ["vehicleName"] = dto.VehicleName,
                    ["occurredOn"] = dto.OccurredOn.ToString("yyyy-MM-dd"),
                    ["note"] = dto.ReviewNote ?? string.Empty
                },
                cancellationToken: cancellationToken);
        }

        return dto;
    }
}
