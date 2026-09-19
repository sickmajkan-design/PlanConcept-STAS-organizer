using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Accommodations.Models;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations;

/// <summary>Fills in how many people live in each accommodation today.</summary>
public static class AccommodationOccupancy
{
    public static async Task FillCurrentOccupantsAsync(
        IApplicationDbContext context,
        IReadOnlyCollection<AccommodationDto> accommodations,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (accommodations.Count == 0)
        {
            return;
        }

        var ids = accommodations.Select(a => a.Id).ToList();

        var counts = await context.AccommodationStays
            .AsNoTracking()
            .Where(s => ids.Contains(s.AccommodationId)
                && s.StartDate <= today
                && (s.EndDate == null || s.EndDate >= today))
            .GroupBy(s => s.AccommodationId)
            .Select(g => new { Id = g.Key, Count = g.Select(s => s.EmployeeId).Distinct().Count() })
            .ToListAsync(cancellationToken);

        var byId = counts.ToDictionary(c => c.Id, c => c.Count);

        foreach (var accommodation in accommodations)
        {
            accommodation.CurrentOccupants = byId.GetValueOrDefault(accommodation.Id);
        }
    }
}
