using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Absences.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences.Commands.ProposeAbsenceEdit;

/// <summary>
/// Proposes new dates (and optionally a new reason) for an already-approved
/// absence. Either side may propose — the employee themselves, or anyone who
/// can review leave — and the proposer's own action counts as their side's
/// agreement: what is missing is the other side's confirmation, via
/// <c>ConfirmAbsenceEditCommand</c>.
/// </summary>
public record ProposeAbsenceEditCommand : IRequest<AbsenceDto>
{
    /// <summary>Set by the API layer from the route.</summary>
    public Guid Id { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public string? Reason { get; init; }
}

public class ProposeAbsenceEditCommandValidator : AbstractValidator<ProposeAbsenceEditCommand>
{
    public ProposeAbsenceEditCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.StartDate)
            .NotEqual(default(DateOnly)).WithMessage("A start date is required.")
            .GreaterThanOrEqualTo(today.AddDays(-AbsenceRules.MaxBackdatingDays))
            .WithMessage(
                $"An absence cannot be recorded more than {AbsenceRules.MaxBackdatingDays} days back.")
            .LessThanOrEqualTo(today.AddDays(AbsenceRules.MaxLeadDays))
            .WithMessage("That start date is further ahead than leave can be booked.");

        RuleFor(x => x.EndDate)
            .NotEqual(default(DateOnly)).WithMessage("An end date is required.")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("The absence cannot end before it starts.");

        RuleFor(x => x)
            .Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber + 1 <= AbsenceRules.MaxDays)
            .WithMessage(
                $"An absence longer than {AbsenceRules.MaxDays} days is a change of employment, not leave.")
            .OverridePropertyName(nameof(ProposeAbsenceEditCommand.EndDate))
            .When(x => x.EndDate >= x.StartDate);

        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public class ProposeAbsenceEditCommandHandler
    : IRequestHandler<ProposeAbsenceEditCommand, AbsenceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notifications;

    public ProposeAbsenceEditCommandHandler(
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

    public async Task<AbsenceDto> Handle(
        ProposeAbsenceEditCommand request,
        CancellationToken cancellationToken)
    {
        var absence = await _context.Absences
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Absence), request.Id);

        var isOwn = _currentUserService.EmployeeId is { } employeeId
            && employeeId == absence.EmployeeId;
        var canReview = AbsenceRules.CanReview(_currentUserService.Role);

        if (!isOwn && !canReview)
        {
            throw new ForbiddenAccessException("You may not propose a change to this absence.");
        }

        if (absence.Status != AbsenceStatus.Approved)
        {
            throw new ConflictException("Only an already-approved absence can have a change proposed.");
        }

        if (absence.Type != AbsenceType.AnnualLeave)
        {
            throw new ConflictException("Only annual leave supports this two-sided change flow.");
        }

        if (absence.HasPendingEdit)
        {
            throw new ConflictException(
                "A change is already waiting on confirmation for this absence.");
        }

        var now = _dateTimeProvider.UtcNow;

        absence.ProposedStartDate = request.StartDate;
        absence.ProposedEndDate = request.EndDate;
        absence.ProposedReason = request.Reason?.Trim();
        absence.ProposedByUserId = _currentUserService.UserId;
        // The employee's own account is what makes this "proposed by the
        // employee" — a foreman acting on someone else's absence is always
        // the management side, even though both are technically staff.
        absence.ProposedByEmployee = isOwn;
        absence.ProposedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        await NotifyOtherSideAsync(absence, cancellationToken);

        return await _context.Absences
            .AsNoTracking()
            .Where(a => a.Id == absence.Id)
            .Select(AbsenceMapping.Projection)
            .FirstAsync(cancellationToken);
    }

    private async Task NotifyOtherSideAsync(Absence absence, CancellationToken cancellationToken)
    {
        var title = "Change proposed for approved leave";
        var body = $"{absence.ProposedStartDate:dd.MM.yyyy}–{absence.ProposedEndDate:dd.MM.yyyy}"
            + " — please confirm or decline.";
        var data = new Dictionary<string, string> { ["absenceId"] = absence.Id.ToString() };

        if (absence.ProposedByEmployee)
        {
            // The employee proposed it — management needs to confirm.
            var recipients = await _context.Users
                .Where(u => u.IsActive
                    && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin
                        || u.Role == UserRole.ProjectManager || u.Role == UserRole.Foreman))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            await _notifications.NotifyUsersAsync(
                recipients, NotificationType.AbsenceEditProposed, title, body, data,
                cancellationToken: cancellationToken);
        }
        else
        {
            // Management proposed it — the employee needs to confirm.
            var userId = await _context.Users
                .Where(u => u.EmployeeId == absence.EmployeeId && u.IsActive)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (userId is null)
            {
                return;
            }

            await _notifications.NotifyUserAsync(
                userId.Value, NotificationType.AbsenceEditProposed, title, body, data,
                cancellationToken: cancellationToken);
        }
    }
}
