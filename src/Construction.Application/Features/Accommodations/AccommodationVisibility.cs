using Construction.Application.Common;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Accommodations.Models;
using Construction.Application.Features.Finance;
using Construction.Domain.Entities;

namespace Construction.Application.Features.Accommodations;

/// <summary>
/// What a Foreman or Project Manager sees of housing: only places where someone from their own
/// sites lives, and no rent, deposit or provider unless they hold the finance grant (the
/// accommodation cost and rate endpoints are already closed to them; the list must not reopen it).
/// </summary>
public static class AccommodationVisibility
{
    /// <summary>Own projects (null = not site-scoped, sees all) and whether amounts may be shown.</summary>
    public static async Task<(IReadOnlyList<Guid>? Own, bool SeesAmounts)> ForCallerAsync(
        IApplicationDbContext context,
        ICurrentUserService user,
        IDateTimeProvider clock,
        CancellationToken cancellationToken)
    {
        var own = await SiteScope.OwnProjectIdsAsync(context, user, clock.Today, cancellationToken);

        return (own, own is null || await FinanceRules.HasFullAsync(context, user, cancellationToken));
    }

    public static IQueryable<Accommodation> Narrow(this IQueryable<Accommodation> query, IReadOnlyList<Guid>? own) =>
        own is null
            ? query
            : query.Where(a => a.Stays.Any(s => s.ProjectId != null && own.Contains(s.ProjectId.Value)));

    public static IQueryable<AccommodationStay> Narrow(this IQueryable<AccommodationStay> query, IReadOnlyList<Guid>? own) =>
        own is null
            ? query
            : query.Where(s => s.ProjectId != null && own.Contains(s.ProjectId.Value));

    public static void HideAmounts(IEnumerable<AccommodationDto> items)
    {
        foreach (var item in items)
        {
            item.DepositAmount = null;
            item.CurrentMonthlyAmount = null;
            item.CurrentProvider = null;
        }
    }
}
