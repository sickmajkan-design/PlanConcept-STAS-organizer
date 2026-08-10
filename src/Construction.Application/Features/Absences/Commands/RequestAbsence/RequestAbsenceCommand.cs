using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Absences.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences.Commands.RequestAbsence;

/// <summary>Books time off, or records it after the fact.</summary>
public record RequestAbsenceCommand : IRequest<AbsenceDto>
{
    /// <summary>Omitted means the caller's own employee record.</summary>
    public Guid? EmployeeId { get; init; }

    public AbsenceType Type { get; init; } = AbsenceType.AnnualLeave;

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public string? Reason { get; init; }

    /// <summary>
    /// Records it as already granted rather than as a request. Only a
    /// supervisor may, and it is refused for anyone else — the office typing
    /// in a phoned-in sick day should not have to approve it afterwards.
    /// </summary>
    public bool Approve { get; init; }
}

public class RequestAbsenceCommandValidator : AbstractValidator<RequestAbsenceCommand>
{
    public RequestAbsenceCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        RuleFor(x => x.Type).IsInEnum();

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
            .OverridePropertyName(nameof(RequestAbsenceCommand.EndDate))
            .When(x => x.EndDate >= x.StartDate);

        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public class RequestAbsenceCommandHandler : IRequestHandler<RequestAbsenceCommand, AbsenceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RequestAbsenceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AbsenceDto> Handle(
        RequestAbsenceCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = request.EmployeeId ?? _currentUserService.EmployeeId
            ?? throw new ForbiddenAccessException(
                "This account is not linked to an employee, so it can only book leave for someone else.");

        var forSomebodyElse = employeeId != _currentUserService.EmployeeId;

        if (forSomebodyElse
            && !AbsenceRules.CanRequestForOthers(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may only book your own leave.");
        }

        if (request.Approve && !AbsenceRules.CanReview(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not grant leave.");
        }

        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == employeeId, cancellationToken);

        if (!employeeExists)
        {
            throw new NotFoundException(nameof(Employee), employeeId);
        }

        var status = request.Approve ? AbsenceStatus.Approved : AbsenceStatus.Requested;

        if (status == AbsenceStatus.Approved)
        {
            await EnsureNoApprovedOverlapAsync(
                employeeId, request.StartDate, request.EndDate, null, cancellationToken);
        }

        var now = _dateTimeProvider.UtcNow;

        var absence = new Absence
        {
            EmployeeId = employeeId,
            Type = request.Type,
            Status = status,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason?.Trim(),
            RequestedByUserId = _currentUserService.UserId,
            ReviewedByUserId = status == AbsenceStatus.Approved
                ? _currentUserService.UserId
                : null,
            ReviewedAt = status == AbsenceStatus.Approved ? now : null
        };

        _context.Absences.Add(absence);

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Absences
            .AsNoTracking()
            .Where(a => a.Id == absence.Id)
            .Select(AbsenceMapping.Projection)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// Refuses granted leave that collides with granted leave.
    /// </summary>
    /// <remarks>
    /// The database refuses this too, with a partial exclusion constraint.
    /// This exists so the answer is a sentence; that exists so two approvals
    /// racing each other cannot both land.
    /// </remarks>
    internal static async Task EnsureNoApprovedOverlapAsync(
        IApplicationDbContext context,
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var clashes = await context.Absences
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId
                && a.Status == AbsenceStatus.Approved
                && (excludeId == null || a.Id != excludeId)
                && a.StartDate <= endDate
                && a.EndDate >= startDate)
            .AnyAsync(cancellationToken);

        if (clashes)
        {
            throw new ConflictException(
                "This employee already has approved time off over those dates.");
        }
    }

    private Task EnsureNoApprovedOverlapAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeId,
        CancellationToken cancellationToken) =>
        EnsureNoApprovedOverlapAsync(
            _context, employeeId, startDate, endDate, excludeId, cancellationToken);
}
