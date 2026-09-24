using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Costs;

/// <summary>
/// Ties a housing expense to the accommodation it is for, and keeps that
/// accommodation's rent from being entered twice.
/// </summary>
/// <remarks>
/// The company's cost holds the rent of every accommodation from its rates
/// (see <c>GetCompanyCostsQuery</c>) and, separately, every general expense.
/// A <see cref="GeneralExpenseCategory.Housing"/> expense for an accommodation
/// on a day that accommodation has a rate in force would count that rent a
/// second time, so it is refused at entry, with the way out in the message.
/// Each expense names its accommodation because there are many, and one
/// accommodation's rate says nothing about another's rent.
/// </remarks>
public static class HousingDoubleEntry
{
    /// <summary>Whether the accommodation has a rate in force on the day.</summary>
    public static Task<bool> IsRentedOnAsync(
        IApplicationDbContext context,
        Guid accommodationId,
        DateOnly day,
        CancellationToken cancellationToken) =>
        context.AccommodationRates
            .AsNoTracking()
            .AnyAsync(
                r => r.AccommodationId == accommodationId
                    && r.StartDate <= day
                    && (r.EndDate == null || r.EndDate >= day),
                cancellationToken);

    /// <summary>
    /// Checks the accommodation an expense names against its category and date.
    /// Only housing names one, and it must; that accommodation must exist and
    /// must not already have its rent counted from a rate on that day.
    /// </summary>
    public static async Task EnsureValidAsync(
        IApplicationDbContext context,
        GeneralExpenseCategory category,
        Guid? accommodationId,
        DateOnly day,
        CancellationToken cancellationToken)
    {
        if (category != GeneralExpenseCategory.Housing)
        {
            if (accommodationId is not null)
            {
                throw new ConflictException("An accommodation can only be named on a housing expense.");
            }

            return;
        }

        if (accommodationId is not { } id)
        {
            throw new ConflictException(
                "Choose which accommodation this housing cost is for.");
        }

        if (!await context.Accommodations.AnyAsync(a => a.Id == id, cancellationToken))
        {
            throw new NotFoundException(nameof(Accommodation), id);
        }

        if (await IsRentedOnAsync(context, id, day, cancellationToken))
        {
            throw new ConflictException(
                "This accommodation's rent is already counted from its rates for this date. "
                + "Enter it there, or use another category, so it is not counted twice.");
        }
    }
}
