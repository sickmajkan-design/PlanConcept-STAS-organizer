using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Accommodations.Stays;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences;

/// <summary>Where a person lives on the first day of a leave, and so what granting it would change.</summary>
public class AbsenceHousingImpactDto
{
    public bool HasStay { get; init; }

    public Guid? StayId { get; init; }

    public Guid? AccommodationId { get; init; }

    public string? AccommodationName { get; init; }

    public DateOnly? StayStartDate { get; init; }

    public DateOnly? StayEndDate { get; init; }
}

public record GetAbsenceHousingImpactQuery(Guid EmployeeId, DateOnly StartDate, DateOnly EndDate)
    : IRequest<AbsenceHousingImpactDto>;

public class GetAbsenceHousingImpactQueryHandler
    : IRequestHandler<GetAbsenceHousingImpactQuery, AbsenceHousingImpactDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAbsenceHousingImpactQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AbsenceHousingImpactDto> Handle(
        GetAbsenceHousingImpactQuery request,
        CancellationToken cancellationToken)
    {
        // Where somebody lives is not for everyone who may book their leave: the people who decide it.
        if (!AbsenceRules.CanReview(_currentUserService.Role))
        {
            throw new ForbiddenAccessException("You may not see where staff are housed.");
        }

        var stay = await AbsenceHousing.FindStay(
            _context.AccommodationStays.AsNoTracking(), request.EmployeeId, request.StartDate)
            .Select(s => new
            {
                s.Id,
                s.AccommodationId,
                Name = s.Accommodation.Name ?? s.Accommodation.Address,
                s.StartDate,
                s.EndDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        return stay is null
            ? new AbsenceHousingImpactDto()
            : new AbsenceHousingImpactDto
            {
                HasStay = true,
                StayId = stay.Id,
                AccommodationId = stay.AccommodationId,
                AccommodationName = stay.Name,
                StayStartDate = stay.StartDate,
                StayEndDate = stay.EndDate
            };
    }
}

/// <summary>
/// Takes a person off their accommodation for the days of a leave, and puts them back after it.
/// </summary>
/// <remarks>
/// A stay's end date is the last day counted, so releasing for a leave that starts on a given day
/// ends the stay the day before: the leave days are not charged to the housing. The return is a
/// second stay in the same place from the day after the leave, carrying over what the first was
/// charged to, because "back from holiday" should not need somebody to remember to put them in a bed.
/// </remarks>
internal static class AbsenceHousing
{
    /// <summary>The stay a person has on the first day of a leave, if any.</summary>
    public static IQueryable<AccommodationStay> FindStay(
        IQueryable<AccommodationStay> stays, Guid employeeId, DateOnly leaveStart) =>
        stays.Where(s => s.EmployeeId == employeeId
            && s.StartDate <= leaveStart
            && (s.EndDate == null || s.EndDate >= leaveStart));

    /// <summary>
    /// Whether leaving housing is the answer when nobody was asked: annual leave takes a person
    /// off it, anything else (sick leave, training) leaves it to a person to decide.
    /// </summary>
    public static bool ReleasesByDefault(AbsenceType type) => type == AbsenceType.AnnualLeave;

    /// <summary>
    /// Ends the person's stay the day before the leave and, when asked, books them back in from the
    /// day after it. Changes are added to the context and saved by the caller, so they land with the
    /// approval or not at all. A stay that begins on or after the first leave day is left alone.
    /// </summary>
    /// <returns>True when a stay was ended.</returns>
    public static async Task<bool> ReleaseAsync(
        IApplicationDbContext context,
        Guid employeeId,
        DateOnly leaveStart,
        DateOnly leaveEnd,
        bool returnAfter,
        CancellationToken cancellationToken)
    {
        var stay = await FindStay(context.AccommodationStays, employeeId, leaveStart)
            .FirstOrDefaultAsync(cancellationToken);

        if (stay is null || stay.StartDate >= leaveStart)
        {
            return false;
        }

        var originalEnd = stay.EndDate;
        var comeBack = leaveEnd.AddDays(1);

        stay.EndDate = leaveStart.AddDays(-1);
        stay.Note = Append(stay.Note, $"Off housing for leave {leaveStart:dd.MM.yyyy}-{leaveEnd:dd.MM.yyyy}");

        if (returnAfter && (originalEnd is null || originalEnd >= comeBack))
        {
            try
            {
                await StayRules.EnsureNoOverlapAsync(
                    context, employeeId, stay.Id, comeBack, originalEnd, cancellationToken);

                context.AccommodationStays.Add(new AccommodationStay
                {
                    AccommodationId = stay.AccommodationId,
                    EmployeeId = employeeId,
                    StartDate = comeBack,
                    EndDate = originalEnd,
                    ProjectId = stay.ProjectId,
                    Note = "Back from leave"
                });
            }
            catch (ConflictException)
            {
                // They have been housed somewhere else for those days. Leave that as it is; the stay
                // above is still ended, which is the part that mattered.
            }
        }

        return true;
    }

    private static string Append(string? existing, string addition)
    {
        var text = string.IsNullOrWhiteSpace(existing) ? addition : $"{existing} | {addition}";

        return text.Length <= 500 ? text : text[..500];
    }
}
