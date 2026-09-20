using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Accommodations.Costs;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs.Queries.GetProjectCosts;

/// <summary>What one site paid towards one accommodation over a period.</summary>
public sealed record ProjectAccommodationShare(
    Guid ProjectId,
    Guid AccommodationId,
    string AccommodationName,
    decimal Cost);

/// <summary>
/// What housing cost each site over the period: the share of every
/// accommodation's rent that fell on the people staying there for that site.
/// Rent for empty days, one-off charges and stays with no project belong to no
/// site and stay out of this figure; they are still on the accommodation's own
/// cost page.
/// </summary>
public static class ProjectAccommodationCosts
{
    public static async Task<List<ProjectAccommodationShare>> LoadAsync(
        IApplicationDbContext context,
        DateOnly from,
        DateOnly to,
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        var stays = await context.AccommodationStays
            .AsNoTracking()
            .Where(s => s.ProjectId != null
                && s.StartDate <= to
                && (s.EndDate == null || s.EndDate >= from))
            .ToListAsync(cancellationToken);

        var shares = new List<ProjectAccommodationShare>();

        if (stays.Count == 0)
        {
            return shares;
        }

        // Rent is split among everyone in the place, so stays without a
        // project have to be loaded too, or a site would be charged for the
        // whole flat while a colleague on another job slept in it.
        var accommodationIds = stays.Select(s => s.AccommodationId).Distinct().ToList();

        var allStays = await context.AccommodationStays
            .AsNoTracking()
            .Where(s => accommodationIds.Contains(s.AccommodationId)
                && s.StartDate <= to
                && (s.EndDate == null || s.EndDate >= from))
            .ToListAsync(cancellationToken);

        var rates = await context.AccommodationRates
            .AsNoTracking()
            .Where(r => accommodationIds.Contains(r.AccommodationId)
                && r.StartDate <= to
                && (r.EndDate == null || r.EndDate >= from))
            .ToListAsync(cancellationToken);

        var names = await context.Accommodations
            .AsNoTracking()
            .Where(a => accommodationIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Name, a.Address })
            .ToDictionaryAsync(
                a => a.Id,
                a => string.IsNullOrWhiteSpace(a.Name) ? a.Address : a.Name!,
                cancellationToken);

        var none = new Dictionary<Guid, string>();

        foreach (var accommodationId in accommodationIds)
        {
            var summary = AccommodationCostCalculator.Calculate(
                rates.Where(r => r.AccommodationId == accommodationId).ToList(),
                allStays.Where(s => s.AccommodationId == accommodationId).ToList(),
                from,
                to,
                none,
                none);

            foreach (var share in summary.ByProject)
            {
                if (share.ProjectId is not { } shareProject
                    || (projectId != null && projectId != shareProject))
                {
                    continue;
                }

                shares.Add(new ProjectAccommodationShare(
                    shareProject,
                    accommodationId,
                    names.GetValueOrDefault(accommodationId, "?"),
                    share.Cost));
            }
        }

        return shares;
    }
}
