using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Absences.Commands.RequestAbsence;
using Construction.Application.Features.Absences.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences.Commands.ReviewAbsence;

/// <summary>Grants or refuses a request for time off.</summary>
public record ReviewAbsenceCommand : IRequest<AbsenceDto>
{
    public Guid Id { get; init; }

    public bool Approve { get; init; }

    /// <summary>Required when refusing, so the person knows why.</summary>
    public string? Note { get; init; }
}

public class ReviewAbsenceCommandValidator : AbstractValidator<ReviewAbsenceCommand>
{
    public ReviewAbsenceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("A reason is required when refusing leave.")
            .MaximumLength(1000)
            .When(x => !x.Approve);

        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

public class ReviewAbsenceCommandHandler : IRequestHandler<ReviewAbsenceCommand, AbsenceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notifications;

    public ReviewAbsenceCommandHandler(
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
        ReviewAbsenceCommand request,
        CancellationToken cancellationToken)
    {
        if (!AbsenceRules.CanReview(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not grant or refuse leave.");
        }

        var absence = await _context.Absences
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Absence), request.Id);

        // Nobody grants their own leave, whatever their role. Without this the
        // approval means nothing, because a foreman's own holiday approves
        // itself — the same control the timesheets already carry.
        if (_currentUserService.EmployeeId is { } reviewerEmployeeId
            && reviewerEmployeeId == absence.EmployeeId)
        {
            throw new ForbiddenAccessException("You cannot grant your own leave.");
        }

        if (absence.Status == AbsenceStatus.Cancelled)
        {
            throw new ConflictException("This request was withdrawn.");
        }

        if (request.Approve)
        {
            await RequestAbsenceCommandHandler.EnsureNoApprovedOverlapAsync(
                _context,
                absence.EmployeeId,
                absence.StartDate,
                absence.EndDate,
                absence.Id,
                cancellationToken);
        }

        absence.Status = request.Approve ? AbsenceStatus.Approved : AbsenceStatus.Rejected;
        absence.ReviewedByUserId = _currentUserService.UserId;
        absence.ReviewedAt = _dateTimeProvider.UtcNow;
        // An approval note would sit on the row looking like an objection.
        absence.ReviewNote = request.Approve ? null : request.Note!.Trim();

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "This request was changed by someone else just now. Reload it and try again.");
        }

        // Unlike hours, both outcomes matter to the person: they are waiting
        // to know whether they can plan the days off.
        var requesterUserId = await _context.Users
            .Where(u => u.IsActive && u.EmployeeId == absence.EmployeeId)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (requesterUserId is { } recipientId)
        {
            var start = absence.StartDate.ToString("yyyy-MM-dd");
            var end = absence.EndDate.ToString("yyyy-MM-dd");
            var data = new Dictionary<string, string>
            {
                ["absenceId"] = absence.Id.ToString(),
                ["decision"] = request.Approve ? "Approved" : "Rejected",
                ["startDate"] = start,
                ["endDate"] = end,
                ["note"] = absence.ReviewNote ?? string.Empty
            };

            await _notifications.NotifyUserAsync(
                recipientId,
                NotificationType.AbsenceDecided,
                request.Approve ? "Time off approved" : "Time off refused",
                request.Approve
                    ? $"Your time off from {start} to {end} was approved."
                    : $"Your time off from {start} to {end} was refused: {absence.ReviewNote}",
                data,
                cancellationToken: cancellationToken);
        }

        return await _context.Absences
            .AsNoTracking()
            .Where(a => a.Id == absence.Id)
            .Select(AbsenceMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
