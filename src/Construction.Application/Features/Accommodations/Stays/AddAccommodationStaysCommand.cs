using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Stays;

/// <summary>One person of a batch: where it differs from the others, it differs here.</summary>
public record AccommodationStayRow
{
    public Guid EmployeeId { get; init; }

    /// <summary>Defaults to today.</summary>
    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    /// <summary>The project this person's housing is charged to; one accommodation can serve several projects.</summary>
    public Guid? ProjectId { get; init; }

    public string? Note { get; init; }
}

/// <summary>
/// Puts several people into one accommodation at once. With <see cref="Preview"/> nothing is written and the answer
/// says what would happen to each; without it everything that is in order is written and the rest is reported.
/// </summary>
public record AddAccommodationStaysCommand : IRequest<AccommodationStaysResultDto>
{
    /// <summary>Set from the route, never from the body.</summary>
    public Guid AccommodationId { get; init; }

    public bool Preview { get; init; }

    public IReadOnlyList<AccommodationStayRow> Stays { get; init; } = [];
}

public static class AccommodationStayRowStatus
{
    public const string Ok = "Ok";

    /// <summary>It would go over the number of beds on some day. A warning, not a refusal.</summary>
    public const string OverCapacity = "OverCapacity";

    public const string Conflict = "Conflict";

    public const string Invalid = "Invalid";
}

public static class AccommodationStayRowReason
{
    public const string EmployeeNotFound = "EmployeeNotFound";
    public const string ProjectNotFound = "ProjectNotFound";
    public const string EndBeforeStart = "EndBeforeStart";
    public const string ListedTwice = "ListedTwice";
    public const string AlreadyHoused = "AlreadyHoused";
}

public record AccommodationStayRowResultDto
{
    public Guid EmployeeId { get; init; }

    public string? EmployeeName { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    /// <summary>See <see cref="AccommodationStayRowStatus"/>.</summary>
    public string Status { get; init; } = AccommodationStayRowStatus.Ok;

    /// <summary>See <see cref="AccommodationStayRowReason"/>. Null when the row is in order.</summary>
    public string? Reason { get; init; }

    /// <summary>For <see cref="AccommodationStayRowReason.AlreadyHoused"/>: where they already are, and until when.</summary>
    public string? Place { get; init; }

    public DateOnly? PlaceUntil { get; init; }

    /// <summary>The stay that was written. Null in a preview and for a row that was not written.</summary>
    public Guid? StayId { get; init; }
}

public record AccommodationStaysResultDto
{
    public bool Preview { get; init; }

    /// <summary>Null when the number of beds is not entered.</summary>
    public int? Beds { get; init; }

    /// <summary>The most people living there at once on any day the batch touches, with the batch in.</summary>
    public int PeakOccupancy { get; init; }

    public DateOnly? PeakDate { get; init; }

    public int Saved { get; init; }

    public IReadOnlyList<AccommodationStayRowResultDto> Rows { get; init; } = [];
}

public class AddAccommodationStaysCommandValidator : AbstractValidator<AddAccommodationStaysCommand>
{
    public AddAccommodationStaysCommandValidator()
    {
        RuleFor(x => x.AccommodationId).NotEmpty();
        RuleFor(x => x.Stays).NotEmpty().Must(s => s.Count <= 100).WithMessage("At most 100 people at once.");
        RuleForEach(x => x.Stays).ChildRules(row =>
        {
            row.RuleFor(r => r.EmployeeId).NotEmpty();
            row.RuleFor(r => r.Note).MaximumLength(500);
        });
    }
}

public class AddAccommodationStaysCommandHandler
    : IRequestHandler<AddAccommodationStaysCommand, AccommodationStaysResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notifications;

    public AddAccommodationStaysCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        INotificationService notifications)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _notifications = notifications;
    }

    public async Task<AccommodationStaysResultDto> Handle(
        AddAccommodationStaysCommand request,
        CancellationToken cancellationToken)
    {
        var accommodation = await _context.Accommodations
            .AsNoTracking()
            .Where(a => a.Id == request.AccommodationId)
            .Select(a => new { a.Id, a.Beds })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Accommodation), request.AccommodationId);

        var today = _dateTimeProvider.Today;
        var employeeIds = request.Stays.Select(r => r.EmployeeId).Distinct().ToList();
        var projectIds = request.Stays.Where(r => r.ProjectId != null).Select(r => r.ProjectId!.Value).Distinct().ToList();

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, Name = e.FirstName + " " + e.LastName })
            .ToDictionaryAsync(e => e.Id, e => e.Name, cancellationToken);

        var projects = await _context.Projects
            .AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

        var rows = new List<Row>();

        foreach (var input in request.Stays)
        {
            var start = input.StartDate ?? today;
            var row = new Row(input, start, employees.GetValueOrDefault(input.EmployeeId),
                input.ProjectId is { } pid ? projects.GetValueOrDefault(pid) : null);

            if (row.EmployeeName is null)
            {
                row.Refuse(AccommodationStayRowStatus.Invalid, AccommodationStayRowReason.EmployeeNotFound);
            }
            else if (input.ProjectId is not null && row.ProjectName is null)
            {
                row.Refuse(AccommodationStayRowStatus.Invalid, AccommodationStayRowReason.ProjectNotFound);
            }
            else if (input.EndDate is { } end && end < start)
            {
                row.Refuse(AccommodationStayRowStatus.Invalid, AccommodationStayRowReason.EndBeforeStart);
            }
            else if (rows.Any(r => r.Fits && r.Input.EmployeeId == input.EmployeeId && r.Overlaps(start, input.EndDate)))
            {
                row.Refuse(AccommodationStayRowStatus.Conflict, AccommodationStayRowReason.ListedTwice);
            }
            else if (await StayRules.FindClashAsync(_context, input.EmployeeId, null, start, input.EndDate, cancellationToken)
                is { } clash)
            {
                row.Refuse(AccommodationStayRowStatus.Conflict, AccommodationStayRowReason.AlreadyHoused, clash.Where, clash.Until);
            }

            rows.Add(row);
        }

        var (peak, peakDate) = await WarnAboutCapacityAsync(accommodation.Id, accommodation.Beds, rows, cancellationToken);

        var saved = 0;

        if (!request.Preview)
        {
            var created = new List<(Row Row, AccommodationStay Stay)>();

            foreach (var row in rows.Where(r => r.Fits))
            {
                var stay = new AccommodationStay
                {
                    AccommodationId = accommodation.Id,
                    EmployeeId = row.Input.EmployeeId,
                    StartDate = row.Start,
                    EndDate = row.Input.EndDate,
                    ProjectId = row.Input.ProjectId,
                    Note = string.IsNullOrWhiteSpace(row.Input.Note) ? null : row.Input.Note.Trim()
                };

                _context.AccommodationStays.Add(stay);
                created.Add((row, stay));
            }

            await _context.SaveChangesAsync(cancellationToken);

            foreach (var (row, stay) in created)
            {
                row.StayId = stay.Id;
                saved++;
                await StayNotifications.NotifyAsync(_context, _notifications, stay, cancellationToken);
            }
        }

        return new AccommodationStaysResultDto
        {
            Preview = request.Preview,
            Beds = accommodation.Beds,
            PeakOccupancy = peak,
            PeakDate = peakDate,
            Saved = saved,
            Rows = rows.Select(r => r.ToDto()).ToList()
        };
    }

    /// <summary>
    /// Counts the people living there on every day the batch can change the count (the day each stay starts), and
    /// marks the rows that are in the house on a day when it is over the number of beds.
    /// </summary>
    private async Task<(int Peak, DateOnly? PeakDate)> WarnAboutCapacityAsync(
        Guid accommodationId, int? beds, List<Row> rows, CancellationToken cancellationToken)
    {
        var incoming = rows.Where(r => r.Fits).ToList();

        if (incoming.Count == 0)
        {
            return (0, null);
        }

        var from = incoming.Min(r => r.Start);
        var to = incoming.Max(r => r.Input.EndDate ?? DateOnly.MaxValue);

        var existing = await _context.AccommodationStays
            .AsNoTracking()
            .Where(s => s.AccommodationId == accommodationId
                && s.StartDate <= to
                && (s.EndDate == null || s.EndDate >= from))
            .Select(s => new { s.EmployeeId, s.StartDate, s.EndDate })
            .ToListAsync(cancellationToken);

        var days = incoming.Select(r => r.Start)
            .Concat(existing.Select(s => s.StartDate).Where(d => d > from && d <= to))
            .Append(from)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        var peak = 0;
        DateOnly? peakDate = null;

        foreach (var day in days)
        {
            var living = existing.Where(s => s.StartDate <= day && (s.EndDate == null || s.EndDate >= day))
                .Select(s => s.EmployeeId)
                .Concat(incoming.Where(r => r.Covers(day)).Select(r => r.Input.EmployeeId))
                .Distinct()
                .Count();

            if (living > peak)
            {
                peak = living;
                peakDate = day;
            }

            if (beds is { } capacity && living > capacity)
            {
                foreach (var row in incoming.Where(r => r.Covers(day)))
                {
                    row.WarnOverCapacity();
                }
            }
        }

        return (peak, peakDate);
    }

    private sealed class Row
    {
        public Row(AccommodationStayRow input, DateOnly start, string? employeeName, string? projectName)
        {
            Input = input;
            Start = start;
            EmployeeName = employeeName;
            ProjectName = projectName;
        }

        public AccommodationStayRow Input { get; }

        public DateOnly Start { get; }

        public string? EmployeeName { get; }

        public string? ProjectName { get; }

        public string Status { get; private set; } = AccommodationStayRowStatus.Ok;

        public string? Reason { get; private set; }

        public string? Place { get; private set; }

        public DateOnly? PlaceUntil { get; private set; }

        public Guid? StayId { get; set; }

        /// <summary>It can be written: in order, possibly with the warning about beds.</summary>
        public bool Fits => Status is AccommodationStayRowStatus.Ok or AccommodationStayRowStatus.OverCapacity;

        public bool Covers(DateOnly day) => Start <= day && (Input.EndDate is null || Input.EndDate >= day);

        public bool Overlaps(DateOnly start, DateOnly? end) =>
            Start <= (end ?? DateOnly.MaxValue) && (Input.EndDate is null || Input.EndDate >= start);

        public void Refuse(string status, string reason, string? place = null, DateOnly? until = null)
        {
            Status = status;
            Reason = reason;
            Place = place;
            PlaceUntil = until;
        }

        public void WarnOverCapacity() => Status = AccommodationStayRowStatus.OverCapacity;

        public AccommodationStayRowResultDto ToDto() => new()
        {
            EmployeeId = Input.EmployeeId,
            EmployeeName = EmployeeName,
            StartDate = Start,
            EndDate = Input.EndDate,
            ProjectId = Input.ProjectId,
            ProjectName = ProjectName,
            Status = Status,
            Reason = Reason,
            Place = Place,
            PlaceUntil = PlaceUntil,
            StayId = StayId
        };
    }
}
