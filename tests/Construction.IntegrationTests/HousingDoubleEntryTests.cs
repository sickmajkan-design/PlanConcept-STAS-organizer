using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Costs.Commands.RecordGeneralExpense;
using Construction.Application.Features.Costs.Commands.UpdateGeneralExpense;
using Construction.Application.Features.Finance;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// A housing expense names the accommodation it is for, and is refused on a day
/// that accommodation's rent is already counted from its rate — while another
/// accommodation's rate says nothing about it.
/// </summary>
/// <remarks>
/// Recording a cost cannot be dated far back, so these use days within the
/// last year — and each test its own single day, with a rate that covers just
/// that day, so no test's rate reaches another's and none reaches the months
/// the cost tests measure.
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class HousingDoubleEntryTests : IntegrationTestBase
{
    public HousingDoubleEntryTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    // Autumn of last year at the latest: outside the March and June 2026 the cost tests measure.
    private static int nextOffset = 340;

    /// <summary>A day of its own, three days apart from any other test's so the day before it is always free.</summary>
    private static DateOnly FreshDay() =>
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-Interlocked.Add(ref nextOffset, 3));

    private async Task<T> AsSuperAdminAsync<T>(Func<TestScope, Task<T>> action)
    {
        return await InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
            scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

            return await action(scope);
        });
    }

    /// <summary>An accommodation, with a rate covering only <paramref name="rentedOn"/> when one is given.</summary>
    private Task<Guid> SeedAccommodationAsync(DateOnly? rentedOn = null) => InScope(async scope =>
    {
        var accommodation = await TestData.SeedAccommodationAsync(scope);

        if (rentedOn is { } day)
        {
            scope.Db.AccommodationRates.Add(new AccommodationRate
            {
                AccommodationId = accommodation.Id,
                Amount = 300m,
                StartDate = day,
                EndDate = day,
            });
            await scope.Db.SaveChangesAsync();
        }

        return accommodation.Id;
    });

    private static RecordGeneralExpenseCommand Housing(Guid? accommodationId, DateOnly day, decimal amount = 300m) => new()
    {
        Category = GeneralExpenseCategory.Housing,
        Amount = amount,
        OccurredOn = day,
        AccommodationId = accommodationId,
    };

    [Fact]
    public async Task A_housing_expense_for_an_accommodation_whose_rent_is_already_counted_is_refused()
    {
        var day = FreshDay();
        var flat = await SeedAccommodationAsync(rentedOn: day);

        await Assert.ThrowsAsync<ConflictException>(() =>
            AsSuperAdminAsync(scope => scope.Send(Housing(flat, day))));
    }

    [Fact]
    public async Task Another_accommodations_rate_says_nothing_about_this_ones_rent()
    {
        var day = FreshDay();
        await SeedAccommodationAsync(rentedOn: day);
        var otherFlat = await SeedAccommodationAsync();

        var recorded = await AsSuperAdminAsync(scope => scope.Send(Housing(otherFlat, day)));

        Assert.Equal(otherFlat, recorded.AccommodationId);
        Assert.Equal(300m, recorded.Amount);
    }

    [Fact]
    public async Task A_day_the_accommodation_has_no_rate_is_not_affected()
    {
        var day = FreshDay();
        var flat = await SeedAccommodationAsync(rentedOn: day);

        var dayBefore = await AsSuperAdminAsync(scope => scope.Send(Housing(flat, day.AddDays(-1))));

        Assert.Equal(flat, dayBefore.AccommodationId);
    }

    [Fact]
    public async Task A_housing_expense_must_name_its_accommodation_and_no_other_category_may()
    {
        var day = FreshDay();
        var flat = await SeedAccommodationAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            AsSuperAdminAsync(scope => scope.Send(Housing(null, day))));

        await Assert.ThrowsAsync<ConflictException>(() =>
            AsSuperAdminAsync(scope => scope.Send(new RecordGeneralExpenseCommand
            {
                Category = GeneralExpenseCategory.Bookkeeping,
                Amount = 40m,
                OccurredOn = day,
                AccommodationId = flat,
            })));

        var bookkeeping = await AsSuperAdminAsync(scope => scope.Send(new RecordGeneralExpenseCommand
        {
            Category = GeneralExpenseCategory.Bookkeeping,
            Amount = 40m,
            OccurredOn = day,
        }));
        Assert.Null(bookkeeping.AccommodationId);
    }

    [Fact]
    public async Task An_accommodation_that_does_not_exist_is_refused()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsSuperAdminAsync(scope => scope.Send(Housing(Guid.NewGuid(), FreshDay()))));
    }

    [Fact]
    public async Task Moving_an_expense_into_housing_on_a_rented_day_is_refused_but_correcting_an_old_row_is_not()
    {
        var day = FreshDay();
        var flat = await SeedAccommodationAsync(rentedOn: day);

        var other = await AsSuperAdminAsync(scope => scope.Send(new RecordGeneralExpenseCommand
        {
            Category = GeneralExpenseCategory.Other,
            Amount = 10m,
            OccurredOn = day,
        }));

        await Assert.ThrowsAsync<ConflictException>(() =>
            AsSuperAdminAsync(scope => scope.Send(new UpdateGeneralExpenseCommand
            {
                Id = other.Id,
                Category = GeneralExpenseCategory.Housing,
                Amount = 10m,
                OccurredOn = day,
                AccommodationId = flat,
            })));

        // A housing row entered before an accommodation was required, naming none:
        // its amount can still be corrected, so what was recorded is not locked.
        Guid oldRow = default;
        await InScope(async scope =>
        {
            var expense = new GeneralExpense
            {
                Category = GeneralExpenseCategory.Housing,
                Amount = 100m,
                OccurredOn = day,
            };
            scope.Db.GeneralExpenses.Add(expense);
            await scope.Db.SaveChangesAsync();
            oldRow = expense.Id;
        });

        var corrected = await AsSuperAdminAsync(scope => scope.Send(new UpdateGeneralExpenseCommand
        {
            Id = oldRow,
            Category = GeneralExpenseCategory.Housing,
            Amount = 120m,
            OccurredOn = day,
        }));
        Assert.Equal(120m, corrected.Amount);
    }

    [Fact]
    public async Task Housing_expenses_that_already_overlap_are_counted_for_the_warning()
    {
        var day = FreshDay();
        var flat = await SeedAccommodationAsync(rentedOn: day);
        var otherFlat = await SeedAccommodationAsync();

        await InScope(async scope =>
        {
            // Naming the accommodation whose rate covers the day: counted twice.
            scope.Db.GeneralExpenses.Add(new GeneralExpense { Category = GeneralExpenseCategory.Housing, Amount = 100m, OccurredOn = day, AccommodationId = flat });
            // Naming another accommodation: its rent is not the one the rate counts.
            scope.Db.GeneralExpenses.Add(new GeneralExpense { Category = GeneralExpenseCategory.Housing, Amount = 100m, OccurredOn = day, AccommodationId = otherFlat });
            // Naming none, from before it was required: any rate that day makes it a possible double.
            scope.Db.GeneralExpenses.Add(new GeneralExpense { Category = GeneralExpenseCategory.Housing, Amount = 100m, OccurredOn = day });
            // A day with no rate, and another category.
            scope.Db.GeneralExpenses.Add(new GeneralExpense { Category = GeneralExpenseCategory.Housing, Amount = 50m, OccurredOn = day.AddDays(-1), AccommodationId = flat });
            scope.Db.GeneralExpenses.Add(new GeneralExpense { Category = GeneralExpenseCategory.Bookkeeping, Amount = 50m, OccurredOn = day });
            await scope.Db.SaveChangesAsync();
        });

        var result = await AsSuperAdminAsync(scope => scope.Send(new GetFinanceByProjectQuery
        {
            From = day.AddDays(-1),
            To = day,
        }));

        Assert.Equal(2, result.HousingDoubleEntries);
    }
}
