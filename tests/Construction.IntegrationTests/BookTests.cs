using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Book;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The book of money: its own entries, the money other tables already hold shown beside them,
/// the categories and who may write. Each test uses a year of its own so the shared database's
/// other rows cannot leak into a list.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class BookTests : IntegrationTestBase
{
    public BookTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static int nextYear = 1700;

    private static int FreshYear() => Interlocked.Increment(ref nextYear);

    private async Task<T> AsAsync<T>(
        UserRole role, FinanceAccess finance, Func<TestScope, Task<T>> action, bool bookAccess = true)
    {
        return await InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, role);
            user.FinanceAccess = finance;
            user.BookAccess = bookAccess;
            await scope.Db.SaveChangesAsync();
            scope.CurrentUser.SignInAs(user.Id, role, null, user.Email);

            return await action(scope);
        });
    }

    private static Task<Guid> CategoryAsync(TestScope scope, string key) =>
        scope.Db.BookCategories.Where(c => c.SystemKey == key).Select(c => c.Id).SingleAsync();

    private static CreateBookEntryCommand Cost(Guid categoryId, int year, decimal amount = 100m) => new()
    {
        Kind = BookEntryKind.Cost,
        Amount = amount,
        OccurredOn = new DateOnly(year, 3, 10),
        CategoryId = categoryId,
        Counterparty = "Knjigovodstvo d.o.o.",
        Description = "Mjesecna naknada",
    };

    [Fact]
    public async Task The_system_categories_are_seeded()
    {
        var keys = await InScope(scope => scope.Db.BookCategories
            .Where(c => c.SystemKey != null)
            .Select(c => c.SystemKey!)
            .ToListAsync());

        Assert.Contains(BookRules.Keys.Collection, keys);
        Assert.Contains(BookRules.Keys.WorkerPay, keys);
        Assert.Contains(BookRules.Keys.Fuel, keys);
        Assert.Contains(BookRules.Keys.FixedCosts, keys);
    }

    [Fact]
    public async Task An_entry_is_listed_with_the_money_other_tables_hold_and_only_its_own_is_editable()
    {
        var year = FreshYear();

        await InScope(async scope =>
        {
            scope.Db.GeneralExpenses.Add(new GeneralExpense
            {
                Category = GeneralExpenseCategory.Bookkeeping, Amount = 40m, OccurredOn = new DateOnly(year, 3, 5),
            });
            scope.Db.CompanyRevenues.Add(new CompanyRevenue
            {
                Amount = 250m, OccurredOn = new DateOnly(year, 3, 20), Source = CompanyRevenueSource.Other,
            });
            await scope.Db.SaveChangesAsync();
        });

        var page = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, async scope =>
        {
            var fixedCosts = await CategoryAsync(scope, BookRules.Keys.FixedCosts);
            await scope.Send(Cost(fixedCosts, year, 100m));

            return await scope.Send(new GetBookEntriesQuery
            {
                From = new DateOnly(year, 1, 1),
                To = new DateOnly(year, 12, 31),
            });
        });

        Assert.Equal(3, page.TotalCount);

        var own = Assert.Single(page.Items, r => r.Source == "BookEntry");
        Assert.True(own.Editable);
        Assert.Equal("Fiksni troškovi", own.CategoryName);
        Assert.Equal(100m, own.Amount);

        var general = Assert.Single(page.Items, r => r.Source == "GeneralExpense");
        Assert.False(general.Editable);
        Assert.Equal("Fiksni troškovi", general.CategoryName);

        var revenue = Assert.Single(page.Items, r => r.Source == "CompanyRevenue");
        Assert.Equal("Income", revenue.Kind);
        Assert.Equal("Naplata klijentu", revenue.CategoryName);
    }

    [Fact]
    public async Task The_list_filters_by_kind_category_and_search()
    {
        var year = FreshYear();

        var (fixedId, fuelId) = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, async scope =>
        {
            var fixedCosts = await CategoryAsync(scope, BookRules.Keys.FixedCosts);
            var fuel = await CategoryAsync(scope, BookRules.Keys.Fuel);

            await scope.Send(Cost(fixedCosts, year, 100m));
            await scope.Send(Cost(fuel, year, 60m) with { Counterparty = "DKV", Description = "Gorivo oktobar" });

            return (fixedCosts, fuel);
        });

        var byCategory = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, scope => scope.Send(new GetBookEntriesQuery
        {
            From = new DateOnly(year, 1, 1),
            To = new DateOnly(year, 12, 31),
            CategoryId = fuelId,
        }));

        Assert.Equal(60m, Assert.Single(byCategory.Items).Amount);

        var bySearch = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, scope => scope.Send(new GetBookEntriesQuery
        {
            From = new DateOnly(year, 1, 1),
            To = new DateOnly(year, 12, 31),
            Search = "KNJIGOVODSTVO",
        }));

        Assert.Equal(100m, Assert.Single(bySearch.Items).Amount);
        Assert.NotEqual(fixedId, fuelId);

        var incomes = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, scope => scope.Send(new GetBookEntriesQuery
        {
            From = new DateOnly(year, 1, 1),
            To = new DateOnly(year, 12, 31),
            Kind = BookEntryKind.Income,
        }));

        Assert.Empty(incomes.Items);
    }

    [Fact]
    public async Task The_totals_cover_the_whole_selection_and_not_a_page()
    {
        var year = FreshYear();

        var totals = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, async scope =>
        {
            var fixedCosts = await CategoryAsync(scope, BookRules.Keys.FixedCosts);
            var collection = await CategoryAsync(scope, BookRules.Keys.Collection);

            for (var i = 0; i < 3; i++)
            {
                await scope.Send(Cost(fixedCosts, year, 100m));
            }

            await scope.Send(new CreateBookEntryCommand
            {
                Kind = BookEntryKind.Income,
                Amount = 700m,
                OccurredOn = new DateOnly(year, 3, 12),
                CategoryId = collection,
                Description = "Prihod",
            });

            return await scope.Send(new GetBookTotalsQuery
            {
                From = new DateOnly(year, 1, 1),
                To = new DateOnly(year, 12, 31),
            });
        });

        Assert.Equal(700m, totals.Inflow);
        Assert.Equal(300m, totals.Outflow);
        Assert.Equal(4, totals.Count);
    }

    [Fact]
    public async Task A_cost_must_be_filed_under_a_cost_category()
    {
        var year = FreshYear();

        await Assert.ThrowsAsync<ConflictException>(() => AsAsync(UserRole.SuperAdmin, FinanceAccess.None, async scope =>
        {
            var collection = await CategoryAsync(scope, BookRules.Keys.Collection);
            return await scope.Send(Cost(collection, year));
        }));
    }

    [Fact]
    public async Task An_income_or_cost_needs_a_category_but_a_payment_falls_back_to_collection()
    {
        var year = FreshYear();

        await Assert.ThrowsAsync<ConflictException>(() => AsAsync(UserRole.SuperAdmin, FinanceAccess.None, scope =>
            scope.Send(Cost(Guid.Empty, year) with { CategoryId = null })));

        var payment = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, scope => scope.Send(new CreateBookEntryCommand
        {
            Kind = BookEntryKind.Payment,
            Amount = 500m,
            OccurredOn = new DateOnly(year, 5, 1),
        }));

        Assert.Equal("Naplata klijentu", payment.CategoryName);
        Assert.Equal(new DateOnly(year, 5, 1), payment.PaidOn);
    }

    [Fact]
    public async Task Sending_the_same_client_request_twice_makes_one_entry()
    {
        var year = FreshYear();
        var clientRequestId = Guid.NewGuid();

        var ids = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, async scope =>
        {
            var fixedCosts = await CategoryAsync(scope, BookRules.Keys.FixedCosts);
            var command = Cost(fixedCosts, year) with { ClientRequestId = clientRequestId };

            var first = await scope.Send(command);
            var second = await scope.Send(command);

            return (first.Id, second.Id);
        });

        Assert.Equal(ids.Item1, ids.Item2);

        var count = await InScope(scope => scope.Db.BookEntries.CountAsync(e => e.ClientRequestId == clientRequestId));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task A_deleted_entry_is_hidden_but_kept()
    {
        var year = FreshYear();

        var id = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, async scope =>
        {
            var fixedCosts = await CategoryAsync(scope, BookRules.Keys.FixedCosts);
            var entry = await scope.Send(Cost(fixedCosts, year));
            await scope.Send(new DeleteBookEntryCommand(entry.Id));

            return entry.Id;
        });

        var page = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, scope => scope.Send(new GetBookEntriesQuery
        {
            From = new DateOnly(year, 1, 1),
            To = new DateOnly(year, 12, 31),
        }));

        Assert.Empty(page.Items);

        var kept = await InScope(scope => scope.Db.BookEntries
            .IgnoreQueryFilters()
            .AnyAsync(e => e.Id == id && e.IsDeleted));

        Assert.True(kept);
    }

    [Theory]
    [InlineData(UserRole.Admin, FinanceAccess.None, false, false, false)]
    [InlineData(UserRole.Admin, FinanceAccess.Full, false, false, false)]
    [InlineData(UserRole.Admin, FinanceAccess.None, true, true, true)]
    [InlineData(UserRole.ProjectManager, FinanceAccess.Full, true, true, false)]
    [InlineData(UserRole.ProjectManager, FinanceAccess.Full, false, false, false)]
    [InlineData(UserRole.Foreman, FinanceAccess.None, false, false, false)]
    public async Task Reading_needs_the_book_grant_and_writing_also_an_admin_role(
        UserRole role, FinanceAccess finance, bool bookAccess, bool canRead, bool canWrite)
    {
        var year = FreshYear();

        async Task<bool> Succeeds(Func<TestScope, Task> action)
        {
            try
            {
                await AsAsync(role, finance, async scope =>
                {
                    await action(scope);
                    return 0;
                }, bookAccess);

                return true;
            }
            catch (ForbiddenAccessException)
            {
                return false;
            }
        }

        Assert.Equal(canRead, await Succeeds(scope => scope.Send(new GetBookEntriesQuery
        {
            From = new DateOnly(year, 1, 1),
            To = new DateOnly(year, 12, 31),
        })));

        Assert.Equal(canWrite, await Succeeds(async scope =>
        {
            var fixedCosts = await CategoryAsync(scope, BookRules.Keys.FixedCosts);
            await scope.Send(Cost(fixedCosts, year));
        }));
    }

    [Fact]
    public async Task A_system_category_can_be_renamed_but_not_switched_off()
    {
        var category = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, async scope =>
        {
            var fuel = await scope.Db.BookCategories.AsNoTracking().SingleAsync(c => c.SystemKey == BookRules.Keys.Fuel);

            await Assert.ThrowsAsync<ConflictException>(() => scope.Send(new UpdateBookCategoryCommand
            {
                Id = fuel.Id,
                Name = fuel.Name,
                SortOrder = fuel.SortOrder,
                IsActive = false,
            }));

            return fuel;
        });

        Assert.True(category.IsActive);
    }

    [Fact]
    public async Task A_category_the_owner_added_can_be_switched_off_and_is_then_refused_for_new_entries()
    {
        var year = FreshYear();

        await Assert.ThrowsAsync<ConflictException>(() => AsAsync(UserRole.SuperAdmin, FinanceAccess.None, async scope =>
        {
            var created = await scope.Send(new CreateBookCategoryCommand
            {
                Name = $"Ured {Guid.NewGuid():N}",
                Kind = BookCategoryKind.Cost,
                Bucket = BookCategoryBucket.Fixed,
            });

            await scope.Send(new UpdateBookCategoryCommand
            {
                Id = created.Id,
                Name = created.Name,
                SortOrder = created.SortOrder,
                IsActive = false,
            });

            return await scope.Send(Cost(created.Id, year));
        }));
    }

    [Fact]
    public async Task The_first_matching_rule_by_priority_is_suggested_and_nothing_is_filed()
    {
        var text = $"zz{Guid.NewGuid():N}";

        var suggestion = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, async scope =>
        {
            var fuel = await CategoryAsync(scope, BookRules.Keys.Fuel);
            var other = await CategoryAsync(scope, BookRules.Keys.Other);

            await scope.Send(new CreateBookCategoryRuleCommand { Text = text, CategoryId = fuel, Priority = 1 });
            await scope.Send(new CreateBookCategoryRuleCommand { Text = text, CategoryId = other, Priority = 2 });

            return await scope.Send(new SuggestBookCategoryQuery($"Racun {text.ToUpperInvariant()} oktobar"));
        });

        Assert.Equal("Gorivo", suggestion.CategoryName);
        Assert.Equal(text, suggestion.MatchedRule);

        var none = await AsAsync(UserRole.SuperAdmin, FinanceAccess.None, scope =>
            scope.Send(new SuggestBookCategoryQuery("nema pravila za ovo qqq")));

        Assert.Null(none.CategoryId);
    }
}
