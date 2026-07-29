using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Construction.Application.Features.Locations.Commands.ReportLocations;

/// <summary>One GPS fix captured by the device.</summary>
public record LocationPing
{
    public double Latitude { get; init; }

    public double Longitude { get; init; }

    /// <summary>Horizontal accuracy radius in meters, as reported by the device.</summary>
    public double? Accuracy { get; init; }

    /// <summary>When the device captured the fix (UTC).</summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Ingests GPS pings from the mobile app. The employee identity always comes
/// from the JWT — a device can never report a position for someone else.
/// Batched so the app can buffer while offline and flush when back online.
/// </summary>
public record ReportLocationsCommand : IRequest<int>
{
    public IReadOnlyList<LocationPing> Pings { get; init; } = Array.Empty<LocationPing>();
}

public class ReportLocationsCommandValidator : AbstractValidator<ReportLocationsCommand>
{
    private const int MaxBatchSize = 120;
    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromMinutes(5);

    public ReportLocationsCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.Pings)
            .NotEmpty().WithMessage("At least one ping is required.")
            .Must(p => p.Count <= MaxBatchSize)
            .WithMessage($"A batch may contain at most {MaxBatchSize} pings.");

        RuleForEach(x => x.Pings).ChildRules(ping =>
        {
            ping.RuleFor(p => p.Latitude)
                .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");

            ping.RuleFor(p => p.Longitude)
                .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");

            ping.RuleFor(p => p.Accuracy)
                .GreaterThanOrEqualTo(0).WithMessage("Accuracy must not be negative.")
                .When(p => p.Accuracy is not null);

            ping.RuleFor(p => p.Timestamp)
                .NotEmpty().WithMessage("Timestamp is required.")
                .Must(t => AsUtc(t) <= dateTimeProvider.UtcNow.Add(AllowedClockSkew))
                .WithMessage("Timestamp must not be in the future.");
        });
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}

public class ReportLocationsCommandHandler : IRequestHandler<ReportLocationsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReportLocationsCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> Handle(ReportLocationsCommand request, CancellationToken cancellationToken)
    {
        var employeeId = _currentUserService.EmployeeId
            ?? throw new ForbiddenAccessException(
                "Only accounts linked to an employee can report locations.");

        var receivedAt = _dateTimeProvider.UtcNow;

        foreach (var ping in request.Pings)
        {
            _context.LocationRecords.Add(new LocationRecord
            {
                EmployeeId = employeeId,
                Latitude = ping.Latitude,
                Longitude = ping.Longitude,
                Accuracy = ping.Accuracy,
                Timestamp = NormalizeToUtc(ping.Timestamp),
                ReceivedAt = receivedAt
            });
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// PostgreSQL timestamptz columns require UTC values; timestamps arriving
    /// without an explicit offset are treated as UTC.
    /// </summary>
    private static DateTime NormalizeToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
