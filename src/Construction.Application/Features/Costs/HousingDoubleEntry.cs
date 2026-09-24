using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs;

/// <summary>
/// Keeps the same rent from being entered twice.
/// </summary>
/// <remarks>
/// The company's cost holds the rent of every accommodation from its rates
/// (see <c>GetCompanyCostsQuery</c>) and, separately, every general expense.
/// A general expense in the <see cref="GeneralExpenseCategory.Housing"/>
/// category on a day an accommodation has a rate in force would count that
/// rent a second time. It is refused at entry, with the way out in the message,
/// rather than left for someone to notice in the totals.
/// </remarks>
public static class HousingDoubleEntry
{
    /// <summary>Whether any accommodation has a rate in force on the day.</summary>
    public static Task<bool> IsRentedOnAsync(
        IApplicationDbContext context,
        DateOnly day,
        CancellationToken cancellationToken) =>
        context.AccommodationRates
            .AsNoTracking()
            .AnyAsync(r => r.StartDate <= day && (r.EndDate == null || r.EndDate >= day), cancellationToken);

    /// <summary>Refuses a housing expense on a day rent is already counted from the accommodation rates.</summary>
    public static async Task EnsureNotCountedTwiceAsync(
        IApplicationDbContext context,
        GeneralExpenseCategory category,
        DateOnly day,
        CancellationToken cancellationToken)
    {
        if (category == GeneralExpenseCategory.Housing
            && await IsRentedOnAsync(context, day, cancellationToken))
        {
            throw new ConflictException(
                "Housing rent is already counted from the accommodation rates for this date. "
                + "Enter it there, or use another category, so it is not counted twice.");
        }
    }
}
