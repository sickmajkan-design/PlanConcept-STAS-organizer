using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Costs.Commands.RecordGeneralExpense;
using Construction.Application.Features.Costs.Commands.UpdateGeneralExpense;
using Construction.Application.Features.Finance;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The rent of every accommodation is counted from its rates; a housing
/// expense on a day a rate is in force would count it a second time.
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

    private Task SeedRateOnAsync(DateOnly day) => InScope(async scope =>
    {
        var accommodation = await TestData.SeedAccommodationAsync(scope);

        scope.Db.AccommodationRates.Add(new AccommodationRate
        {
            AccommodationId = accommodation.Id,
            Amount = 300m,
            StartDate = day,
            EndDate = day,
        });
        await scope.Db.SaveChangesAsync();
    });

    [Fact]
    public async Task A_housing_expense_on_a_day_rent_is_already_counted_is_refused()
    {
        var day = FreshDay();
        await SeedRateOnAsync(day);

        await Assert.ThrowsAsync<ConflictException>(() =>
            AsSuperAdminAsync(scope => scope.Send(new RecordGeneralExpenseCommand
            {
                Category = GeneralExpenseCategory.Housing,
                Amount = 300m,
                OccurredOn = day,
            })));
    }

    [Fact]
    public async Task Other_categories_and_days_with_no_rate_are_not_affected()
    {
        var day = FreshDay();
        await SeedRateOnAsync(day);

        var bookkeeping = await AsSuperAdminAsync(scope => scope.Send(new RecordGeneralExpenseCommand
        {
            Category = GeneralExpenseCategory.Bookkeeping,
            Amount = 40m,
            OccurredOn = day,
        }));
        Assert.Equal(40m, bookkeeping.Amount);

        var dayBefore = await AsSuperAdminAsync(scope => scope.Send(new RecordGeneralExpenseCommand
        {
            Category = GeneralExpenseCategory.Housing,
            Amount = 300m,
            OccurredOn = day.AddDays(-1),
        }));
        Assert.Equal(300m, dayBefore.Amount);
    }

    [Fact]
    public async Task Moving_an_expense_into_housing_on_a_rented_day_is_refused_but_correcting_an_old_row_is_not()
    {
        var day = FreshDay();
        await SeedRateOnAsync(day);

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
            })));

        // A row entered before the rule existed and already overlapping: its amount can still be fixed.
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
        await SeedRateOnAsync(day);

        await InScope(async scope =>
        {
            scope.Db.GeneralExpenses.Add(new GeneralExpense { Category = GeneralExpenseCategory.Housing, Amount = 100m, OccurredOn = day });
            scope.Db.GeneralExpenses.Add(new GeneralExpense { Category = GeneralExpenseCategory.Housing, Amount = 50m, OccurredOn = day.AddDays(-1) });
            scope.Db.GeneralExpenses.Add(new GeneralExpense { Category = GeneralExpenseCategory.Bookkeeping, Amount = 50m, OccurredOn = day });
            await scope.Db.SaveChangesAsync();
        });

        var result = await AsSuperAdminAsync(scope => scope.Send(new GetFinanceByProjectQuery
        {
            From = day.AddDays(-1),
            To = day,
        }));

        Assert.Equal(1, result.HousingDoubleEntries);
    }
}
