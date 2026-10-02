using Construction.Application.Features.Branches;
using Construction.Application.Features.Ledgers.Commands;
using Construction.Application.Features.Ledgers.Commands.CreateLedger;
using Construction.Application.Features.Ledgers.Commands.UpdateLedger;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.Ledgers.Queries.GetLedgerById;
using Construction.Application.Features.Ledgers.Queries.GetLedgerChecks;
using Construction.Application.Features.Ledgers.Queries.GetLedgers;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// A business unit runs its own monthly payroll: the first draft holds the people that unit employed
/// that month (whatever site they worked on), the list can be narrowed to it, and a person who is not
/// the unit's is flagged rather than silently paid under it.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class LedgerBranchTests : IntegrationTestBase
{
    public LedgerBranchTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private async Task<User> SuperAdminAsync() =>
        await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

    private sealed record World(Branch First, Branch Second, Employee OfFirst, Employee OfSecond, Employee OfNone, Project Site);

    /// <summary>Two units, a person employed by each in September 2026 and one by neither, all posted to one site.</summary>
    private Task<World> SeedWorldAsync() => InScope(async scope =>
    {
        var first = new Branch { Name = $"Led1 {Unique()}", Color = "#3457D5" };
        var second = new Branch { Name = $"Led2 {Unique()}", Color = "#0F8A5F" };
        scope.Db.Branches.AddRange(first, second);

        var ofFirst = await TestData.SeedEmployeeAsync(scope);
        var ofSecond = await TestData.SeedEmployeeAsync(scope);
        var ofNone = await TestData.SeedEmployeeAsync(scope);
        var site = await TestData.SeedProjectAsync(scope);
        await scope.Db.SaveChangesAsync();

        scope.Db.EmployeeBranches.AddRange(
            new EmployeeBranch { EmployeeId = ofFirst.Id, BranchId = first.Id, StartDate = new DateOnly(2026, 1, 1) },
            new EmployeeBranch { EmployeeId = ofSecond.Id, BranchId = second.Id, StartDate = new DateOnly(2026, 1, 1) });

        foreach (var person in new[] { ofFirst, ofSecond, ofNone })
        {
            scope.Db.EmployeeProjects.Add(new EmployeeProject
            {
                EmployeeId = person.Id,
                ProjectId = site.Id,
                StartDate = new DateOnly(2026, 8, 1),
            });
        }

        await scope.Db.SaveChangesAsync();

        return new World(first, second, ofFirst, ofSecond, ofNone, site);
    });

    private Task<LedgerDetailDto> CreateAsync(User admin, Guid? branchId, bool populate = true) => InScope(scope =>
    {
        ActAs(scope, admin);

        return scope.Send(new CreateLedgerCommand
        {
            Name = $"Obračun {Unique()}",
            Year = 2026,
            Month = 9,
            BranchId = branchId,
            Template = LedgerTemplates.Payroll,
            PopulateFromProjects = populate,
        });
    });

    private Task<IReadOnlyList<Guid?>> EmployeesOnAsync(User admin, Guid ledgerId) => InScope(async scope =>
    {
        ActAs(scope, admin);

        var rows = new List<Guid?>();

        foreach (var section in (await scope.Send(new GetLedgerByIdQuery(ledgerId))).Sections)
        {
            rows.AddRange(scope.Db.LedgerRows.Where(r => r.SectionId == section.Id).Select(r => r.EmployeeId));
        }

        return (IReadOnlyList<Guid?>)rows;
    });

    [Fact]
    public async Task A_units_payroll_starts_with_the_people_that_unit_employed_that_month()
    {
        var admin = await SuperAdminAsync();
        var world = await SeedWorldAsync();

        var ledger = await CreateAsync(admin, world.First.Id);

        Assert.Equal(world.First.Id, ledger.BranchId);
        Assert.Equal(world.First.Name, ledger.BranchName);

        var people = await EmployeesOnAsync(admin, ledger.Id);

        Assert.Contains(world.OfFirst.Id, people);
        Assert.DoesNotContain(world.OfSecond.Id, people);
        Assert.DoesNotContain(world.OfNone.Id, people);
    }

    [Fact]
    public async Task A_company_wide_payroll_still_holds_everyone_on_the_site()
    {
        var admin = await SuperAdminAsync();
        var world = await SeedWorldAsync();

        var ledger = await CreateAsync(admin, branchId: null);

        Assert.Null(ledger.BranchId);

        var people = await EmployeesOnAsync(admin, ledger.Id);
        Assert.Contains(world.OfFirst.Id, people);
        Assert.Contains(world.OfSecond.Id, people);
        Assert.Contains(world.OfNone.Id, people);
    }

    [Fact]
    public async Task The_payroll_list_can_be_narrowed_to_one_unit()
    {
        var admin = await SuperAdminAsync();
        var world = await SeedWorldAsync();

        var mine = await CreateAsync(admin, world.First.Id, populate: false);
        await CreateAsync(admin, world.Second.Id, populate: false);

        var page = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetLedgersQuery { BranchId = world.First.Id, PageSize = 50 });
        });

        Assert.Equal([mine.Id], page.Items.Select(l => l.Id).ToArray());
        Assert.Equal(world.First.Name, page.Items.Single().BranchName);
    }

    [Fact]
    public async Task A_person_the_unit_did_not_employ_is_flagged_with_who_did()
    {
        var admin = await SuperAdminAsync();
        var world = await SeedWorldAsync();

        // The company-wide month has everybody; assigning it to the first unit afterwards leaves
        // the others on its books by mistake — which is exactly what the check is for.
        var ledger = await CreateAsync(admin, branchId: null);

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new UpdateLedgerCommand
            {
                Id = ledger.Id,
                Name = ledger.Name,
                Year = ledger.Year,
                Month = ledger.Month,
                BranchId = world.First.Id,
            });
        });

        var checks = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetLedgerChecksQuery { LedgerId = ledger.Id });
        });

        var flagged = checks.Where(c => c.Kind == LedgerCheckKinds.EmployeeInOtherUnit).ToList();

        // The shared database holds other people on other sites, so look only at the three of this test.
        using var check = Fixture.CreateScope();
        Guid RowOf(Employee person) => check.Db.LedgerRows
            .Where(r => r.Section.LedgerId == ledger.Id && r.EmployeeId == person.Id)
            .Select(r => r.Id)
            .Single();

        // Employed by the second unit: flagged, naming that unit.
        Assert.Equal(world.Second.Name, flagged.Single(c => c.RowId == RowOf(world.OfSecond)).OtherBranchName);

        // Employed by nobody: flagged too, with no unit named.
        Assert.Null(flagged.Single(c => c.RowId == RowOf(world.OfNone)).OtherBranchName);

        // The unit's own person is not flagged.
        Assert.DoesNotContain(flagged, c => c.RowId == RowOf(world.OfFirst));
    }

    [Fact]
    public async Task A_company_wide_payroll_has_nothing_to_check_against_a_unit()
    {
        var admin = await SuperAdminAsync();
        await SeedWorldAsync();

        var ledger = await CreateAsync(admin, branchId: null);

        var checks = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new GetLedgerChecksQuery { LedgerId = ledger.Id });
        });

        Assert.DoesNotContain(checks, c => c.Kind == LedgerCheckKinds.EmployeeInOtherUnit);
    }

    [Fact]
    public async Task A_copied_month_keeps_the_unit_of_its_source()
    {
        var admin = await SuperAdminAsync();
        var world = await SeedWorldAsync();

        var source = await CreateAsync(admin, world.First.Id, populate: false);

        var copy = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new CreateLedgerCommand
            {
                Name = $"Kopija {Unique()}",
                Year = 2026,
                Month = 10,
                CopyFromLedgerId = source.Id,
            });
        });

        Assert.Equal(world.First.Id, copy.BranchId);
    }

    [Fact]
    public async Task A_payroll_cannot_name_a_unit_that_does_not_exist()
    {
        var admin = await SuperAdminAsync();

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.NotFoundException>(
            () => CreateAsync(admin, Guid.NewGuid(), populate: false));
    }
}
