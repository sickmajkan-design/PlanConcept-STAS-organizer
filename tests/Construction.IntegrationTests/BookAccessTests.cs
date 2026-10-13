using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attention;
using Construction.Application.Features.Authentication.Queries.GetCurrentUser;
using Construction.Application.Features.Book;
using Construction.Application.Features.Overview;
using Construction.Application.Features.Payouts;
using Construction.Application.Features.Recurring;
using Construction.Application.Features.Users.Commands.UpdateUser;
using Construction.Application.Features.Users.Queries.GetUserById;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The book of money is closed to everybody except a Super Admin until one switches it on for an account.
/// The grant is its own flag: the finance grant given for the older pages must not open it.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class BookAccessTests : IntegrationTestBase
{
    public BookAccessTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static int nextYear = 1999;

    private static int FreshYear() => Interlocked.Increment(ref nextYear);

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private async Task<User> SeedAsync(UserRole role, bool bookAccess = false, FinanceAccess finance = FinanceAccess.None)
    {
        return await InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, role);
            user.BookAccess = bookAccess;
            user.FinanceAccess = finance;
            await scope.Db.SaveChangesAsync();

            return user;
        });
    }

    private async Task<bool> AllowedAsync(User user, Func<TestScope, Task> action)
    {
        try
        {
            await InScope(async scope =>
            {
                ActAs(scope, user);
                await action(scope);
                return 0;
            });

            return true;
        }
        catch (ForbiddenAccessException)
        {
            return false;
        }
    }

    /// <summary>Every way into the book that reads, so a new one added later without the gate shows up here.</summary>
    private static IEnumerable<(string Name, Func<TestScope, Task> Call)> Reads(int year)
    {
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);

        yield return ("entries", s => s.Send(new GetBookEntriesQuery { From = from, To = to }));
        yield return ("totals", s => s.Send(new GetBookTotalsQuery { From = from, To = to }));
        yield return ("export", s => s.Send(new GetBookExportQuery { From = from, To = to }));
        yield return ("categories", s => s.Send(new GetBookCategoriesQuery()));
        yield return ("category rules", s => s.Send(new GetBookCategoryRulesQuery()));
        yield return ("receivables", s => s.Send(new GetReceivablesQuery()));
        yield return ("payables", s => s.Send(new GetPayablesQuery()));
        yield return ("overview", s => s.Send(new GetOverviewQuery { From = from, To = to }));
        yield return ("cashflow", s => s.Send(new GetCashflowQuery()));
        yield return ("forecast", s => s.Send(new GetForecastQuery()));
        yield return ("overview attention", s => s.Send(new GetOverviewAttentionQuery()));
        yield return ("recurring", s => s.Send(new GetRecurringExpensesQuery()));
        yield return ("recurring preview", s => s.Send(new GetRecurringPreviewQuery(year, 3)));
    }

    private static async Task WriteAsync(TestScope scope, int year)
    {
        var categoryId = await scope.Db.BookCategories
            .Where(c => c.SystemKey == BookRules.Keys.FixedCosts)
            .Select(c => c.Id)
            .SingleAsync();

        await scope.Send(new CreateBookEntryCommand
        {
            Kind = BookEntryKind.Cost,
            Amount = 10m,
            OccurredOn = new DateOnly(year, 3, 10),
            CategoryId = categoryId,
            Description = "Pristup",
        });
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Foreman)]
    public async Task Without_the_book_grant_nothing_in_it_opens_even_with_the_full_finance_grant(UserRole role)
    {
        var year = FreshYear();
        var user = await SeedAsync(role, bookAccess: false, finance: FinanceAccess.Full);

        foreach (var (name, call) in Reads(year))
        {
            Assert.False(await AllowedAsync(user, call), $"{name} opened without the book grant");
        }

        Assert.False(await AllowedAsync(user, scope => WriteAsync(scope, year)), "an entry was written");
        Assert.False(
            await AllowedAsync(user, scope => scope.Send(new GetPayoutsQuery { Year = year, Month = 3 })),
            "payouts opened");
    }

    [Fact]
    public async Task The_super_admin_always_has_the_book_without_any_flag()
    {
        var year = FreshYear();
        var superAdmin = await SeedAsync(UserRole.SuperAdmin);

        foreach (var (name, call) in Reads(year))
        {
            Assert.True(await AllowedAsync(superAdmin, call), $"{name} refused to the super admin");
        }

        Assert.True(await AllowedAsync(superAdmin, scope => WriteAsync(scope, year)));
    }

    [Fact]
    public async Task An_admin_with_the_grant_reads_and_writes()
    {
        var year = FreshYear();
        var admin = await SeedAsync(UserRole.Admin, bookAccess: true);

        foreach (var (name, call) in Reads(year))
        {
            Assert.True(await AllowedAsync(admin, call), $"{name} refused to an admin with the grant");
        }

        Assert.True(await AllowedAsync(admin, scope => WriteAsync(scope, year)));
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Foreman)]
    public async Task A_site_role_with_the_grant_reads_but_may_not_write(UserRole role)
    {
        var year = FreshYear();
        var user = await SeedAsync(role, bookAccess: true);

        Assert.True(await AllowedAsync(user, scope => scope.Send(new GetBookEntriesQuery
        {
            From = new DateOnly(year, 1, 1),
            To = new DateOnly(year, 12, 31),
        })));

        Assert.False(await AllowedAsync(user, scope => WriteAsync(scope, year)));
    }

    [Fact]
    public async Task Payouts_need_the_book_grant_and_the_full_finance_grant_together()
    {
        var year = FreshYear();
        var onlyBook = await SeedAsync(UserRole.Admin, bookAccess: true, finance: FinanceAccess.None);
        var both = await SeedAsync(UserRole.Admin, bookAccess: true, finance: FinanceAccess.Full);

        Assert.False(await AllowedAsync(onlyBook, s => s.Send(new GetPayoutsQuery { Year = year, Month = 3 })));
        Assert.True(await AllowedAsync(both, s => s.Send(new GetPayoutsQuery { Year = year, Month = 3 })));
    }

    [Theory]
    [InlineData(UserRole.Worker)]
    [InlineData(UserRole.Customer)]
    public async Task A_value_stored_on_a_worker_or_customer_never_opens_the_book(UserRole role)
    {
        var year = FreshYear();
        var user = await SeedAsync(role, bookAccess: true);

        foreach (var (name, call) in Reads(year))
        {
            Assert.False(await AllowedAsync(user, call), $"{name} opened for a {role}");
        }
    }

    [Fact]
    public async Task Taking_the_grant_away_applies_to_the_very_next_request()
    {
        var year = FreshYear();
        var admin = await SeedAsync(UserRole.Admin, bookAccess: true);
        var superAdmin = await SeedAsync(UserRole.SuperAdmin);

        Task Read(TestScope s) => s.Send(new GetBookTotalsQuery
        {
            From = new DateOnly(year, 1, 1),
            To = new DateOnly(year, 12, 31),
        });

        Assert.True(await AllowedAsync(admin, Read));

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new UpdateUserCommand
            {
                Id = admin.Id,
                Email = admin.Email,
                Role = UserRole.Admin,
                BookAccess = false,
            });
        });

        Assert.False(await AllowedAsync(admin, Read));
    }

    [Fact]
    public async Task Only_the_super_admin_can_hand_the_grant_out()
    {
        var superAdmin = await SeedAsync(UserRole.SuperAdmin);
        var admin = await SeedAsync(UserRole.Admin, bookAccess: true);
        var foreman = await SeedAsync(UserRole.Foreman);

        Task Grant(User caller, bool value) => InScope(scope =>
        {
            ActAs(scope, caller);
            return scope.Send(new UpdateUserCommand
            {
                Id = foreman.Id,
                Email = foreman.Email,
                Role = UserRole.Foreman,
                BookAccess = value,
            });
        });

        async Task<bool> StoredAsync() => await InScope(scope => scope.Db.Users
            .Where(u => u.Id == foreman.Id)
            .Select(u => u.BookAccess)
            .SingleAsync());

        // An admin may manage accounts, but the request must not smuggle the grant through.
        await Grant(admin, true);
        Assert.False(await StoredAsync());

        await Grant(superAdmin, true);
        Assert.True(await StoredAsync());

        await Grant(superAdmin, false);
        Assert.False(await StoredAsync());
    }

    [Fact]
    public async Task A_request_that_leaves_the_grant_out_keeps_it_as_it_was()
    {
        var superAdmin = await SeedAsync(UserRole.SuperAdmin);
        var admin = await SeedAsync(UserRole.Admin, bookAccess: true);

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new UpdateUserCommand { Id = admin.Id, Email = admin.Email, Role = UserRole.Admin });
        });

        var stored = await InScope(scope => scope.Db.Users
            .Where(u => u.Id == admin.Id)
            .Select(u => u.BookAccess)
            .SingleAsync());

        Assert.True(stored);
    }

    [Fact]
    public async Task The_grant_cannot_be_given_to_a_worker()
    {
        var superAdmin = await SeedAsync(UserRole.SuperAdmin);
        var worker = await SeedAsync(UserRole.Worker);

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new UpdateUserCommand
            {
                Id = worker.Id,
                Email = worker.Email,
                Role = UserRole.Worker,
                BookAccess = true,
            });
        }));
    }

    [Fact]
    public async Task Moving_an_account_to_a_worker_role_takes_the_grant_away()
    {
        var superAdmin = await SeedAsync(UserRole.SuperAdmin);
        var foreman = await SeedAsync(UserRole.Foreman, bookAccess: true);

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new UpdateUserCommand
            {
                Id = foreman.Id,
                Email = foreman.Email,
                Role = UserRole.Worker,
            });
        });

        var stored = await InScope(scope => scope.Db.Users
            .Where(u => u.Id == foreman.Id)
            .Select(u => u.BookAccess)
            .SingleAsync());

        Assert.False(stored);
    }

    [Fact]
    public async Task The_account_data_tells_the_panel_whether_to_offer_the_book()
    {
        var superAdmin = await SeedAsync(UserRole.SuperAdmin);
        var granted = await SeedAsync(UserRole.Admin, bookAccess: true);
        var plain = await SeedAsync(UserRole.Admin, finance: FinanceAccess.Full);

        var fromSuper = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetCurrentUserQuery());
        });
        var fromGranted = await InScope(scope =>
        {
            ActAs(scope, granted);
            return scope.Send(new GetCurrentUserQuery());
        });
        var fromPlain = await InScope(scope =>
        {
            ActAs(scope, plain);
            return scope.Send(new GetCurrentUserQuery());
        });
        var listed = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetUserByIdQuery(granted.Id));
        });

        Assert.True(fromSuper.BookAccess);
        Assert.True(fromGranted.BookAccess);
        Assert.False(fromPlain.BookAccess);
        Assert.True(listed.BookAccess);
    }

    [Fact]
    public async Task Late_money_does_not_leak_into_the_attention_list_of_an_account_without_the_grant()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var number = $"BA-{Guid.NewGuid():N}"[..14];

        await InScope(async scope =>
        {
            scope.Db.Invoices.Add(new Invoice
            {
                ProjectId = project.Id,
                Number = number,
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-60),
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
                Amount = 500m,
                PayrollYear = 2026,
                PayrollMonth = 1,
                Status = InvoiceStatus.Issued,
            });
            await scope.Db.SaveChangesAsync();
            return 0;
        });

        var withFinanceOnly = await SeedAsync(UserRole.Admin, bookAccess: false, finance: FinanceAccess.Full);
        var withBook = await SeedAsync(UserRole.Admin, bookAccess: true, finance: FinanceAccess.None);

        async Task<bool> SeesLateInvoices(User user)
        {
            var attention = await InScope(scope =>
            {
                ActAs(scope, user);
                return scope.Send(new GetAttentionQuery());
            });

            return attention.Groups.Any(g => g.Key == "lateInvoices");
        }

        Assert.False(await SeesLateInvoices(withFinanceOnly));
        Assert.True(await SeesLateInvoices(withBook));
    }
}
