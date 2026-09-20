using Construction.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Queries.GetMyHousing;

/// <summary>
/// What a worker needs to know about where they sleep. Deliberately leaves out
/// rent, deposit and contract: those are the firm's spending, not the worker's.
/// </summary>
public class MyHousingDto
{
    public string Name { get; init; } = null!;

    public string Address { get; init; } = null!;

    public string? City { get; init; }

    public string? Floor { get; init; }

    public int? Rooms { get; init; }

    public string? LandlordName { get; init; }

    public string? LandlordPhone { get; init; }

    /// <summary>Practical notes for living there: keys, entry, house rules.</summary>
    public string? Note { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    /// <summary>True when the stay begins in the future.</summary>
    public bool Upcoming { get; init; }

    public IReadOnlyList<string> Roommates { get; init; } = [];
}

/// <summary>The caller's current stay, or their next one; null when they have none.</summary>
public record GetMyHousingQuery : IRequest<MyHousingDto?>;

public class GetMyHousingQueryHandler : IRequestHandler<GetMyHousingQuery, MyHousingDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetMyHousingQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<MyHousingDto?> Handle(GetMyHousingQuery request, CancellationToken cancellationToken)
    {
        var employeeId = _currentUser.EmployeeId;

        if (employeeId is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);

        var stay = await _context.AccommodationStays
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId
                && (s.EndDate == null || s.EndDate >= today))
            .OrderBy(s => s.StartDate)
            .Select(s => new
            {
                s.AccommodationId,
                s.StartDate,
                s.EndDate,
                s.Accommodation.Name,
                s.Accommodation.Address,
                s.Accommodation.City,
                s.Accommodation.Floor,
                s.Accommodation.Rooms,
                s.Accommodation.LandlordName,
                s.Accommodation.LandlordPhone,
                s.Accommodation.Note
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (stay is null)
        {
            return null;
        }

        var roommates = await _context.AccommodationStays
            .AsNoTracking()
            .Where(s => s.AccommodationId == stay.AccommodationId
                && s.EmployeeId != employeeId
                && s.StartDate <= today
                && (s.EndDate == null || s.EndDate >= today))
            .Select(s => s.Employee.FirstName + " " + s.Employee.LastName)
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);

        return new MyHousingDto
        {
            Name = string.IsNullOrWhiteSpace(stay.Name) ? stay.Address : stay.Name,
            Address = stay.Address,
            City = stay.City,
            Floor = stay.Floor,
            Rooms = stay.Rooms,
            LandlordName = stay.LandlordName,
            LandlordPhone = stay.LandlordPhone,
            Note = stay.Note,
            StartDate = stay.StartDate,
            EndDate = stay.EndDate,
            Upcoming = stay.StartDate > today,
            Roommates = roommates
        };
    }
}
