using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.TimeEntries.Models;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries.Commands.ClockIn;

/// <summary>
/// Starts a shift for the signed-in employee.
/// </summary>
/// <remarks>
/// The employee always comes from the JWT, never from the payload, for the
/// same reason location reporting does: a phone must not be able to start a
/// shift in someone else's name.
/// </remarks>
public record ClockInCommand : IRequest<TimeEntryDto>
{
    public Guid? ProjectId { get; init; }

    public WorkType WorkType { get; init; } = WorkType.Regular;

    public string? Note { get; init; }

    /// <summary>Where the phone was, when it had a fix. Optional by design —
    /// a worker in a basement must still be able to start work.</summary>
    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}

public class ClockInCommandValidator : AbstractValidator<ClockInCommand>
{
    public ClockInCommandValidator()
    {
        RuleFor(x => x.WorkType).IsInEnum();

        RuleFor(x => x.Note).MaximumLength(1000);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude is not null);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude is not null);

        // Half a position is not a position, and storing one would put a
        // marker on the null island.
        RuleFor(x => x)
            .Must(x => x.Latitude is null == x.Longitude is null)
            .WithMessage("Latitude and longitude must be supplied together.")
            // Named so the 400 response points at a field the client can
            // highlight, instead of an error with no property at all.
            .OverridePropertyName(nameof(ClockInCommand.Longitude));
    }
}

public class ClockInCommandHandler : IRequestHandler<ClockInCommand, TimeEntryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ClockInCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<TimeEntryDto> Handle(
        ClockInCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUserService.EmployeeId
            ?? throw new ForbiddenAccessException(
                "Only accounts linked to an employee can record work time.");

        var openShift = await _context.TimeEntries
            .AsNoTracking()
            .AnyAsync(t => t.EmployeeId == employeeId && t.EndedAt == null, cancellationToken);

        if (openShift)
        {
            // The database refuses this too; catching it here turns a
            // constraint violation into a sentence the app can show.
            throw new ConflictException("You are already clocked in.");
        }

        if (request.ProjectId is { } projectId)
        {
            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == projectId, cancellationToken);

            if (!projectExists)
            {
                throw new NotFoundException(nameof(Project), projectId);
            }
        }

        var startedAt = _dateTimeProvider.UtcNow;

        await TimeEntryRules.EnsureNoOverlapAsync(
            _context, employeeId, startedAt, null, null, cancellationToken);

        var entry = new TimeEntry
        {
            EmployeeId = employeeId,
            ProjectId = request.ProjectId,
            StartedAt = startedAt,
            WorkType = request.WorkType,
            Status = TimeEntryStatus.InProgress,
            Note = request.Note?.Trim(),
            StartLatitude = request.Latitude,
            StartLongitude = request.Longitude
        };

        _context.TimeEntries.Add(entry);

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.TimeEntries
            .AsNoTracking()
            .Where(t => t.Id == entry.Id)
            .Select(TimeEntryMapping.Projection)
            .FirstAsync(cancellationToken);
    }
}
