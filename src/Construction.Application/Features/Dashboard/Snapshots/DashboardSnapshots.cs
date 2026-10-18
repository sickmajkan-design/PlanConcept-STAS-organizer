using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Dashboard.Snapshots;

/// <summary>
/// What this user's board showed today and, for "since yesterday", what it last showed yesterday.
/// </summary>
/// <remarks>
/// Only exactly yesterday counts: after a gap there is no honest "since yesterday", so there is no baseline
/// until the next day the figure is seen. The client records what it displayed; nothing here is computed from
/// the data, and nobody else's board is touched.
/// </remarks>
public record GetDashboardSnapshotsQuery : IRequest<DashboardSnapshotsDto>;

public record DashboardSnapshotsDto
{
    public IReadOnlyDictionary<string, decimal> Today { get; init; } = new Dictionary<string, decimal>();

    public IReadOnlyDictionary<string, decimal> Yesterday { get; init; } = new Dictionary<string, decimal>();
}

public class GetDashboardSnapshotsQueryHandler : IRequestHandler<GetDashboardSnapshotsQuery, DashboardSnapshotsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetDashboardSnapshotsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<DashboardSnapshotsDto> Handle(GetDashboardSnapshotsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("No signed-in user.");
        var today = _clock.Today;
        var yesterday = today.AddDays(-1);

        var rows = await _context.DashboardSnapshots.AsNoTracking()
            .Where(s => s.UserId == userId && (s.Day == today || s.Day == yesterday))
            .Select(s => new { s.Day, s.Key, s.Value })
            .ToListAsync(cancellationToken);

        return new DashboardSnapshotsDto
        {
            Today = rows.Where(r => r.Day == today).ToDictionary(r => r.Key, r => r.Value),
            Yesterday = rows.Where(r => r.Day == yesterday).ToDictionary(r => r.Key, r => r.Value),
        };
    }
}

/// <summary>Records the figures this user's board is showing now as today's; the last value of the day is kept.</summary>
public record RecordDashboardSnapshotsCommand : IRequest<Unit>
{
    public IReadOnlyDictionary<string, decimal> Values { get; init; } = new Dictionary<string, decimal>();
}

public class RecordDashboardSnapshotsCommandValidator : AbstractValidator<RecordDashboardSnapshotsCommand>
{
    public const int MaxValues = 100;
    public const int MaxKeyLength = 120;
    private const decimal MaxMagnitude = 1_000_000_000_000m;

    public RecordDashboardSnapshotsCommandValidator()
    {
        RuleFor(x => x.Values)
            .Must(v => v.Count <= MaxValues).WithMessage($"At most {MaxValues} figures at a time.")
            .Must(v => v.Keys.All(k => k.Length is > 0 and <= MaxKeyLength)).WithMessage("A figure name is empty or too long.")
            .Must(v => v.Values.All(n => Math.Abs(n) <= MaxMagnitude)).WithMessage("A figure is out of range.");
    }
}

public class RecordDashboardSnapshotsCommandHandler : IRequestHandler<RecordDashboardSnapshotsCommand, Unit>
{
    /// <summary>Days kept: today, yesterday and a spare, so a day boundary crossed mid-session never loses the baseline.</summary>
    private const int KeepDays = 3;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public RecordDashboardSnapshotsCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Unit> Handle(RecordDashboardSnapshotsCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenAccessException("No signed-in user.");
        var today = _clock.Today;
        var oldest = today.AddDays(-(KeepDays - 1));

        var existing = await _context.DashboardSnapshots
            .Where(s => s.UserId == userId && (s.Day == today || s.Day < oldest))
            .ToListAsync(cancellationToken);

        foreach (var stale in existing.Where(s => s.Day < oldest))
        {
            _context.DashboardSnapshots.Remove(stale);
        }

        var todays = existing.Where(s => s.Day == today).ToDictionary(s => s.Key);

        foreach (var (key, value) in request.Values)
        {
            if (todays.TryGetValue(key, out var row))
            {
                row.Value = value;
            }
            else
            {
                _context.DashboardSnapshots.Add(new DashboardSnapshot { UserId = userId, Day = today, Key = key, Value = value });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
