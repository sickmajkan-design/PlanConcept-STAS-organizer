using AutoMapper;
using AutoMapper.QueryableExtensions;
using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.TimeEntries.Models;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.TimeEntries.Commands.ClockOut;

/// <summary>
/// Ends the signed-in employee's running shift and submits it for review.
/// </summary>
/// <remarks>
/// There is no entry id in the payload: an employee has at most one running
/// shift, and the server already knows which. That also means a stale phone
/// cannot close a shift someone else already corrected.
/// </remarks>
public record ClockOutCommand : IRequest<TimeEntryDto>
{
    /// <summary>Unpaid break to subtract from the shift.</summary>
    public int BreakMinutes { get; init; }

    public string? Note { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}

public class ClockOutCommandValidator : AbstractValidator<ClockOutCommand>
{
    public ClockOutCommandValidator()
    {
        RuleFor(x => x.BreakMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Break must not be negative.")
            .LessThanOrEqualTo((int)TimeEntryRules.MaxShiftDuration.TotalMinutes)
            .WithMessage("Break cannot be longer than a shift.");

        RuleFor(x => x.Note).MaximumLength(1000);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.Latitude is not null);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.Longitude is not null);

        RuleFor(x => x)
            .Must(x => x.Latitude is null == x.Longitude is null)
            .WithMessage("Latitude and longitude must be supplied together.")
            // Named so the 400 response points at a field the client can
            // highlight, instead of an error with no property at all.
            .OverridePropertyName(nameof(ClockOutCommand.Longitude));
    }
}

public class ClockOutCommandHandler : IRequestHandler<ClockOutCommand, TimeEntryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMapper _mapper;

    public ClockOutCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _mapper = mapper;
    }

    public async Task<TimeEntryDto> Handle(
        ClockOutCommand request,
        CancellationToken cancellationToken)
    {
        var employeeId = _currentUserService.EmployeeId
            ?? throw new ForbiddenAccessException(
                "Only accounts linked to an employee can record work time.");

        var entry = await _context.TimeEntries
            .FirstOrDefaultAsync(
                t => t.EmployeeId == employeeId && t.EndedAt == null,
                cancellationToken)
            ?? throw new ConflictException("You are not clocked in.");

        var endedAt = _dateTimeProvider.UtcNow;
        var elapsed = endedAt - entry.StartedAt;

        if (elapsed > TimeEntryRules.MaxShiftDuration)
        {
            // Someone left it running overnight. Refusing is the honest
            // outcome: the app cannot know when they actually stopped, and a
            // guess would be indistinguishable from a real shift afterwards.
            throw new ConflictException(
                $"This shift has been running for over " +
                $"{TimeEntryRules.MaxShiftDuration.TotalHours:0} hours. " +
                "A supervisor has to close it with the correct end time.");
        }

        // Truncated to whole minutes so this compares the same number the
        // entry will report. Comparing against the fractional elapsed time
        // instead lets a break equal to the shift through whenever the stored
        // start has been rounded to PostgreSQL's microsecond precision — and
        // the entry then records zero paid minutes, which is precisely what
        // this guard exists to prevent.
        var elapsedMinutes = (int)elapsed.TotalMinutes;

        if (request.BreakMinutes >= elapsedMinutes)
        {
            throw new ConflictException(
                "The break is as long as the shift, which would leave no time worked.");
        }

        entry.EndedAt = endedAt;
        entry.BreakMinutes = request.BreakMinutes;
        entry.Status = TimeEntryStatus.Submitted;
        entry.EndLatitude = request.Latitude;
        entry.EndLongitude = request.Longitude;

        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            entry.Note = request.Note.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.TimeEntries
            .AsNoTracking()
            .Where(t => t.Id == entry.Id)
            .ProjectTo<TimeEntryDto>(_mapper.ConfigurationProvider)
            .FirstAsync(cancellationToken);
    }
}
