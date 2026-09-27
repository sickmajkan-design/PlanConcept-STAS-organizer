using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Absences;

/// <summary>
/// Reads the data the leave rules need: the firm's holidays and each person's approved
/// annual-leave working days. Shared by the balance and by the payroll, so a day is
/// counted the same way wherever it is asked about.
/// </summary>
public static class LeaveData
{
    /// <summary>
    /// The public holidays of the country the firm counts leave by; empty when none is set,
    /// in which case only weekends are non-working.
    /// </summary>
    public static async Task<IReadOnlySet<DateOnly>> HolidaysAsync(
        IApplicationDbContext context,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var country = await context.CompanySettings
            .AsNoTracking()
            .Select(c => c.LeaveHolidayCountryCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(country))
        {
            return new HashSet<DateOnly>();
        }

        var code = country.Trim().ToUpperInvariant();

        var days = await context.PublicHolidays
            .AsNoTracking()
            .Where(h => h.CountryCode == code && h.Date >= from && h.Date <= to)
            .Select(h => h.Date)
            .ToListAsync(cancellationToken);

        return days.ToHashSet();
    }

    /// <summary>
    /// For each person, every approved annual-leave working day inside the window. A stretch
    /// that runs past either end only counts the days that fall inside it.
    /// </summary>
    public static async Task<Dictionary<Guid, List<DateOnly>>> ApprovedLeaveDaysAsync(
        IApplicationDbContext context,
        IReadOnlyCollection<Guid> employeeIds,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var result = employeeIds.ToDictionary(id => id, _ => new List<DateOnly>());

        if (employeeIds.Count == 0)
        {
            return result;
        }

        var holidays = await HolidaysAsync(context, from, to, cancellationToken);

        var stretches = await context.Absences
            .AsNoTracking()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                && a.Type == AbsenceType.AnnualLeave
                && a.Status == AbsenceStatus.Approved
                && a.StartDate <= to
                && a.EndDate >= from)
            .Select(a => new { a.EmployeeId, a.StartDate, a.EndDate })
            .ToListAsync(cancellationToken);

        foreach (var stretch in stretches)
        {
            result[stretch.EmployeeId].AddRange(
                LeaveCalculator.WorkingDaysWithin(stretch.StartDate, stretch.EndDate, from, to, holidays));
        }

        // Approved stretches of one person do not overlap, but a day is never counted twice regardless.
        foreach (var id in result.Keys.ToList())
        {
            result[id] = result[id].Distinct().ToList();
        }

        return result;
    }
}
