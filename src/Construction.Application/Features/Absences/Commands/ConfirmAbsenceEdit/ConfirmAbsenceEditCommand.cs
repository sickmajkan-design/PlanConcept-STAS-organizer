using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Absences.Commands.RequestAbsence;
using Construction.Application.Features.Absences.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences.Commands.ConfirmAbsenceEdit;

/// <summary>
/// Confirms or declines a change proposed by the other side (see
/// <c>ProposeAbsenceEditCommand</c>). Confirming writes the proposed dates
/// onto the absence itself; declining just clears the proposal and leaves
/// the original, already-approved absence untouched.
/// </summary>
public record ConfirmAbsenceEditCommand : IRequest<AbsenceDto>
{
    /// <summary>Set by the API layer from the route.</summary>
    public Guid Id { get; init; }

    public bool Approve { get; init; }
}

public class ConfirmAbsenceEditCommandValidator : AbstractValidator<ConfirmAbsenceEditCommand>
{
    public ConfirmAbsenceEditCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ConfirmAbsenceEditCommandHandler
    : IRequestHandler<ConfirmAbsenceEditCommand, AbsenceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notifications;

    public ConfirmAbsenceEditCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        INotificationService notifications)
    {
        _context = context;
        _currentUserService = currentUserService;
        _notifications = notifications;
    }

    public async Task<AbsenceDto> Handle(
        ConfirmAbsenceEditCommand request,
        CancellationToken cancellationToken)
    {
        var absence = await _context.Absences
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Absence), request.Id);

        if (!absence.HasPendingEdit)
        {
            throw new ConflictException("There is no pending change on this absence.");
        }

        var isOwn = _currentUserService.EmployeeId is { } employeeId
            && employeeId == absence.EmployeeId;
        var canReview = AbsenceRules.CanReview(_currentUserService.Role);

        // Only the side that did NOT propose it may confirm — the proposer's
        // own action already counted as their agreement.
        var canConfirm = absence.ProposedByEmployee ? canReview : isOwn;

        if (!canConfirm)
        {
            throw new ForbiddenAccessException(
                absence.ProposedByEmployee
                    ? "Only management may confirm this change."
                    : "Only the employee may confirm this change.");
        }

        if (request.Approve)
        {
            await RequestAbsenceCommandHandler.EnsureNoApprovedOverlapAsync(
                _context,
                absence.EmployeeId,
                absence.ProposedStartDate!.Value,
                absence.ProposedEndDate!.Value,
                absence.Id,
                cancellationToken);

            absence.StartDate = absence.ProposedStartDate.Value;
            absence.EndDate = absence.ProposedEndDate.Value;
            if (absence.ProposedReason is not null)
            {
                absence.Reason = absence.ProposedReason;
            }
        }

        var proposerUserId = absence.ProposedByUserId;

        absence.ProposedStartDate = null;
        absence.ProposedEndDate = null;
        absence.ProposedReason = null;
        absence.ProposedByUserId = null;
        absence.ProposedByEmployee = false;
        absence.ProposedAt = null;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "This request was changed by someone else just now. Reload it and try again.");
        }

        if (proposerUserId is { } notifyUserId)
        {
            await _notifications.NotifyUserAsync(
                notifyUserId,
                NotificationType.AbsenceEditProposed,
                request.Approve ? "Leave change confirmed" : "Leave change declined",
                request.Approve
                    ? $"{absence.StartDate:dd.MM.yyyy}–{absence.EndDate:dd.MM.yyyy}"
                    : "The other side declined your proposed change.",
                BuildData(absence, request.Approve),
                cancellationToken: cancellationToken);
        }

        return await _context.Absences
            .AsNoTracking()
            .Where(a => a.Id == absence.Id)
            .Select(AbsenceMapping.Projection)
            .FirstAsync(cancellationToken);
    }

    private static Dictionary<string, string> BuildData(Absence absence, bool approved)
    {
        var data = new Dictionary<string, string>
        {
            ["absenceId"] = absence.Id.ToString(),
            ["approved"] = approved ? "true" : "false"
        };

        if (approved)
        {
            data["startDate"] = absence.StartDate.ToString("yyyy-MM-dd");
            data["endDate"] = absence.EndDate.ToString("yyyy-MM-dd");
        }

        return data;
    }
}
