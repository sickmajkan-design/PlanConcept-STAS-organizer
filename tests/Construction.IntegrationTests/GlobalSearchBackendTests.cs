using System.Text;
using Construction.Application.Features.Absences.Queries.GetAbsences;
using Construction.Application.Features.ArticleOrders.Queries.GetArticleOrders;
using Construction.Application.Features.FuelCards.Queries;
using Construction.Application.Features.FuelTransactions;
using Construction.Application.Features.Refunds.Queries.GetRefunds;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// What the Ctrl+K search asks the lists for: each list that the palette searches must find a record by the
/// words a person would type, and say nothing for words that are not there.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class GlobalSearchBackendTests : IntegrationTestBase
{
    public GlobalSearchBackendTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private async Task<User> AdminAsync()
    {
        return await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));
    }

    private static void SignIn(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    // ---- fuel cards -----------------------------------------------------------

    [Fact]
    public async Task A_fuel_card_is_found_by_its_number_its_vehicle_or_its_td_number()
    {
        var admin = await AdminAsync();
        var td = "TD" + Unique();
        var card = "9" + Guid.NewGuid().ToString("N")[..12];

        var vehicle = await InScope(async scope =>
        {
            var v = await TestData.SeedVehicleAsync(scope);
            v.TdNumber = td;
            scope.Db.FuelCards.Add(new FuelCard { VehicleId = v.Id, Provider = "DKV", CardNumber = card });
            await scope.Db.SaveChangesAsync();
            return v;
        });

        foreach (var term in new[] { card, card[3..9], td.ToLowerInvariant(), vehicle.RegistrationNumber })
        {
            var page = await InScope(scope =>
            {
                SignIn(scope, admin);
                return scope.Send(new GetFuelCardsQuery { Search = term, PageSize = 20 });
            });

            Assert.Contains(page.Items, c => c.CardNumber == card);
        }

        var none = await InScope(scope =>
        {
            SignIn(scope, admin);
            return scope.Send(new GetFuelCardsQuery { Search = "zzz-" + Unique() });
        });

        Assert.Empty(none.Items);
    }

    // ---- DKV statement rows -----------------------------------------------------

    [Fact]
    public async Task A_statement_row_is_found_by_its_card_or_by_the_vehicle_it_belongs_to()
    {
        var admin = await AdminAsync();
        var td = "TD" + Unique();
        var card = "7043" + Guid.NewGuid().ToString("N")[..14];

        await InScope(async scope =>
        {
            var v = await TestData.SeedVehicleAsync(scope);
            v.TdNumber = td;
            scope.Db.FuelCards.Add(new FuelCard { VehicleId = v.Id, Provider = "DKV", CardNumber = card });
            await scope.Db.SaveChangesAsync();
        });

        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var csv = "Št. kartice/kataloga;Registrska številka vozila;Čas transakcije;Status računa;Stroškovna skupina;"
            + "Skupina izdelkov;Vrsta izdelka;Koda izdelka;Skupna vrednost bruto (valuta računa);Država storitve\r\n"
            + $"{card};SD SMART;{day:dd.MM.yyyy} - 06:10;Obračunano;Stroški goriva;Dizelsko gorivo;Dizelsko gorivo;WA0009;55.00 EUR;DE";
        var bytes = Encoding.UTF8.GetBytes(csv);

        await InScope(scope =>
        {
            SignIn(scope, admin);
            return scope.Send(new ImportDkvStatementCommand
            {
                FileName = "x.csv",
                SizeBytes = bytes.Length,
                Content = new MemoryStream(bytes)
            });
        });

        foreach (var term in new[] { card, card[4..10], td.ToLowerInvariant() })
        {
            var page = await InScope(scope =>
            {
                SignIn(scope, admin);
                return scope.Send(new GetFuelTransactionsQuery { Search = term, PageSize = 20 });
            });

            Assert.Contains(page.Items, r => r.CardNumber == card);
        }

        var none = await InScope(scope =>
        {
            SignIn(scope, admin);
            return scope.Send(new GetFuelTransactionsQuery { Search = "zzz-" + Unique() });
        });

        Assert.Empty(none.Items);
    }

    // ---- absences ----------------------------------------------------------------

    [Fact]
    public async Task Leave_is_found_by_the_name_of_the_person()
    {
        var admin = await AdminAsync();
        var lastName = "Prezime" + Unique();
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope, lastName: lastName));

        await InScope(async scope =>
        {
            scope.Db.Absences.Add(new Absence
            {
                EmployeeId = employee.Id,
                Type = AbsenceType.AnnualLeave,
                Status = AbsenceStatus.Approved,
                StartDate = new DateOnly(2027, 1, 11),
                EndDate = new DateOnly(2027, 1, 15)
            });
            await scope.Db.SaveChangesAsync();
        });

        var found = await InScope(scope =>
        {
            SignIn(scope, admin);
            return scope.Send(new GetAbsencesQuery { Search = lastName.ToLowerInvariant(), PageSize = 20 });
        });
        var none = await InScope(scope =>
        {
            SignIn(scope, admin);
            return scope.Send(new GetAbsencesQuery { Search = "zzz-" + Unique() });
        });

        Assert.Contains(found.Items, a => a.EmployeeId == employee.Id);
        Assert.Empty(none.Items);
    }

    // ---- refunds -------------------------------------------------------------------

    [Fact]
    public async Task A_refund_is_found_by_what_it_was_for_or_by_who_asked()
    {
        var admin = await AdminAsync();
        var what = "rukavice" + Unique();
        var lastName = "Refund" + Unique();
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope, lastName: lastName));

        await InScope(async scope =>
        {
            scope.Db.Refunds.Add(new Refund
            {
                EmployeeId = employee.Id,
                RequestedByUserId = admin.Id,
                Amount = 12m,
                Currency = "EUR",
                ExpenseDate = new DateOnly(2026, 9, 20),
                Description = what
            });
            await scope.Db.SaveChangesAsync();
        });

        foreach (var term in new[] { what, lastName })
        {
            var page = await InScope(scope =>
            {
                SignIn(scope, admin);
                return scope.Send(new GetRefundsQuery { Search = term, PageSize = 20 });
            });

            Assert.Contains(page.Items, r => r.Description == what);
        }
    }

    // ---- article orders ----------------------------------------------------------------

    [Fact]
    public async Task An_order_is_found_by_an_article_in_it()
    {
        var admin = await AdminAsync();
        var article = "Svrdlo" + Unique();

        await InScope(async scope =>
        {
            scope.Db.ArticleOrders.Add(new ArticleOrder
            {
                RequestedByUserId = admin.Id,
                Status = ArticleOrderStatus.Requested,
                Items = [new ArticleOrderItem { Name = article, Quantity = 3 }]
            });
            await scope.Db.SaveChangesAsync();
        });

        var found = await InScope(scope =>
        {
            SignIn(scope, admin);
            return scope.Send(new GetArticleOrdersQuery { Search = article.ToLowerInvariant(), PageSize = 20 });
        });
        var none = await InScope(scope =>
        {
            SignIn(scope, admin);
            return scope.Send(new GetArticleOrdersQuery { Search = "zzz-" + Unique() });
        });

        Assert.Single(found.Items);
        Assert.Empty(none.Items);
    }
}
