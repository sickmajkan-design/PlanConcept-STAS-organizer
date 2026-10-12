using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Book;
using Construction.Application.Features.Recurring;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Costs that come back on their own. Templates are shared by the whole database, so each test names its own
/// and reads back only the entries of the templates it made.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class RecurringExpenseTests : IntegrationTestBase
{
    public RecurringExpenseTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private async Task<T> AsSuperAdminAsync<T>(Func<TestScope, Task<T>> action)
    {
        return await InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
            scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

            return await action(scope);
        });
    }

    private Task<RecurringExpenseDto> CreateAsync(
        RecurringFrequency frequency = RecurringFrequency.Monthly,
        DateOnly? start = null,
        DateOnly? end = null,
        decimal amount = 100m,
        int day = 5) =>
        AsSuperAdminAsync(async scope =>
        {
            var fixedCosts = await scope.Db.BookCategories
                .Where(c => c.SystemKey == BookRules.Keys.FixedCosts).Select(c => c.Id).SingleAsync();

            return await scope.Send(new CreateRecurringExpenseCommand
            {
                Name = $"Najam {Unique()}",
                CategoryId = fixedCosts,
                Amount = amount,
                Frequency = frequency,
                StartMonth = start ?? new DateOnly(2024, 1, 1),
                DayOfMonth = day,
                EndDate = end,
            });
        });

    private Task<List<BookEntry>> EntriesOfAsync(Guid templateId) =>
        InScope(scope => scope.Db.BookEntries
            .IgnoreQueryFilters()
            .Where(e => e.RecurringExpenseId == templateId)
            .OrderBy(e => e.PeriodYear).ThenBy(e => e.PeriodMonth)
            .ToListAsync());

    [Fact]
    public async Task A_template_falls_in_the_periods_its_frequency_and_dates_allow()
    {
        var monthly = new RecurringExpense { StartMonth = new DateOnly(2024, 1, 1), Frequency = RecurringFrequency.Monthly, IsActive = true };
        var quarterly = new RecurringExpense { StartMonth = new DateOnly(2024, 2, 1), Frequency = RecurringFrequency.Quarterly, IsActive = true };
        var annual = new RecurringExpense { StartMonth = new DateOnly(2024, 6, 1), Frequency = RecurringFrequency.Annual, IsActive = true };
        var ended = new RecurringExpense
        {
            StartMonth = new DateOnly(2024, 1, 1), Frequency = RecurringFrequency.Monthly, IsActive = true,
            EndDate = new DateOnly(2024, 3, 1),
        };
        var off = new RecurringExpense { StartMonth = new DateOnly(2024, 1, 1), Frequency = RecurringFrequency.Monthly, IsActive = false };

        Assert.False(RecurringSchedule.Applies(monthly, 2023, 12));
        Assert.True(RecurringSchedule.Applies(monthly, 2024, 1));
        Assert.True(RecurringSchedule.Applies(monthly, 2025, 7));

        Assert.True(RecurringSchedule.Applies(quarterly, 2024, 2));
        Assert.False(RecurringSchedule.Applies(quarterly, 2024, 3));
        Assert.True(RecurringSchedule.Applies(quarterly, 2024, 5));

        Assert.True(RecurringSchedule.Applies(annual, 2025, 6));
        Assert.False(RecurringSchedule.Applies(annual, 2025, 7));

        Assert.True(RecurringSchedule.Applies(ended, 2024, 3));
        Assert.False(RecurringSchedule.Applies(ended, 2024, 4));

        Assert.False(RecurringSchedule.Applies(off, 2024, 5));
    }

    [Fact]
    public async Task Generating_twice_makes_each_period_once()
    {
        var template = await CreateAsync(start: new DateOnly(2025, 1, 1));

        var first = await AsSuperAdminAsync(scope => scope.Send(new GenerateRecurringExpensesCommand(2025, 3)));
        var second = await AsSuperAdminAsync(scope => scope.Send(new GenerateRecurringExpensesCommand(2025, 3)));

        Assert.True(first >= 1);
        Assert.Equal(0, second);

        var entries = await EntriesOfAsync(template.Id);
        var entry = Assert.Single(entries);

        Assert.Equal(BookEntryKind.Cost, entry.Kind);
        Assert.Equal(BookEntrySource.Template, entry.Source);
        Assert.Equal(100m, entry.Amount);
        Assert.Equal(new DateOnly(2025, 3, 5), entry.OccurredOn);
        Assert.Equal(new DateOnly(2025, 3, 5), entry.DueDate);
        Assert.Null(entry.PaidOn);
        Assert.Equal(template.Name, entry.Description);
    }

    [Fact]
    public async Task An_entry_that_was_deleted_on_purpose_does_not_come_back()
    {
        var template = await CreateAsync(start: new DateOnly(2025, 1, 1));

        await AsSuperAdminAsync(scope => scope.Send(new GenerateRecurringExpensesCommand(2025, 4)));

        var entry = Assert.Single(await EntriesOfAsync(template.Id));

        await AsSuperAdminAsync(async scope =>
        {
            await scope.Send(new DeleteBookEntryCommand(entry.Id));
            return 0;
        });

        await AsSuperAdminAsync(scope => scope.Send(new GenerateRecurringExpensesCommand(2025, 4)));

        var all = await EntriesOfAsync(template.Id);

        Assert.Single(all);
        Assert.True(all[0].IsDeleted);
    }

    [Fact]
    public async Task A_new_amount_applies_from_the_next_period_and_leaves_what_was_made()
    {
        var template = await CreateAsync(start: new DateOnly(2025, 1, 1), amount: 100m);

        await AsSuperAdminAsync(scope => scope.Send(new GenerateRecurringExpensesCommand(2025, 5)));

        await AsSuperAdminAsync(scope => scope.Send(new UpdateRecurringExpenseCommand
        {
            Id = template.Id,
            Name = template.Name,
            CategoryId = template.CategoryId,
            Amount = 150m,
            Frequency = RecurringFrequency.Monthly,
            StartMonth = template.StartMonth,
            DayOfMonth = template.DayOfMonth,
        }));

        await AsSuperAdminAsync(scope => scope.Send(new GenerateRecurringExpensesCommand(2025, 6)));

        var entries = await EntriesOfAsync(template.Id);

        Assert.Equal(2, entries.Count);
        Assert.Equal(100m, entries[0].Amount);
        Assert.Equal(150m, entries[1].Amount);
    }

    [Fact]
    public async Task A_template_that_is_off_makes_nothing_and_keeps_what_it_made()
    {
        var template = await CreateAsync(start: new DateOnly(2025, 1, 1));

        await AsSuperAdminAsync(scope => scope.Send(new GenerateRecurringExpensesCommand(2025, 7)));

        var off = await AsSuperAdminAsync(scope => scope.Send(new ToggleRecurringExpenseCommand(template.Id)));
        Assert.False(off.IsActive);

        await AsSuperAdminAsync(scope => scope.Send(new GenerateRecurringExpensesCommand(2025, 8)));

        var entries = await EntriesOfAsync(template.Id);

        Assert.Single(entries);
        Assert.Equal(7, entries[0].PeriodMonth);
    }

    [Fact]
    public async Task The_automatic_pass_makes_an_entry_only_on_or_after_its_day()
    {
        var early = await CreateAsync(start: new DateOnly(2025, 1, 1), day: 2);
        var late = await CreateAsync(start: new DateOnly(2025, 1, 1), day: 25);

        await InScope(scope => RecurringSchedule.GenerateAsync(
            scope.Db, 2025, 9, new DateOnly(2025, 9, 10), onlyDue: true, recordedBy: null, CancellationToken.None));

        Assert.Single(await EntriesOfAsync(early.Id));
        Assert.Empty(await EntriesOfAsync(late.Id));

        await InScope(scope => RecurringSchedule.GenerateAsync(
            scope.Db, 2025, 9, new DateOnly(2025, 9, 26), onlyDue: true, recordedBy: null, CancellationToken.None));

        Assert.Single(await EntriesOfAsync(early.Id));
        Assert.Single(await EntriesOfAsync(late.Id));
    }

    [Fact]
    public async Task Entries_are_not_made_for_a_month_to_come()
    {
        var next = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(2);

        await Assert.ThrowsAsync<ConflictException>(() => AsSuperAdminAsync(scope =>
            scope.Send(new GenerateRecurringExpensesCommand(next.Year, next.Month))));
    }

    [Fact]
    public async Task The_preview_shows_what_a_month_makes_and_what_it_already_made()
    {
        var template = await CreateAsync(start: new DateOnly(2025, 1, 1));

        var before = await AsSuperAdminAsync(scope => scope.Send(new GetRecurringPreviewQuery(2025, 10)));
        Assert.False(before.Single(r => r.RecurringExpenseId == template.Id).AlreadyMade);

        await AsSuperAdminAsync(scope => scope.Send(new GenerateRecurringExpensesCommand(2025, 10)));

        var after = await AsSuperAdminAsync(scope => scope.Send(new GetRecurringPreviewQuery(2025, 10)));
        Assert.True(after.Single(r => r.RecurringExpenseId == template.Id).AlreadyMade);
    }

    [Fact]
    public async Task A_recurring_cost_must_be_filed_under_a_cost_category()
    {
        await Assert.ThrowsAsync<ConflictException>(() => AsSuperAdminAsync(async scope =>
        {
            var collection = await scope.Db.BookCategories
                .Where(c => c.SystemKey == BookRules.Keys.Collection).Select(c => c.Id).SingleAsync();

            return await scope.Send(new CreateRecurringExpenseCommand
            {
                Name = "Pogresno",
                CategoryId = collection,
                Amount = 10m,
                Frequency = RecurringFrequency.Monthly,
                StartMonth = new DateOnly(2025, 1, 1),
                DayOfMonth = 1,
            });
        }));
    }
}
