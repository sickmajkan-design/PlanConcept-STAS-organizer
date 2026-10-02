using Construction.Application.Features.Branches;
using Construction.Application.Features.Costs.Queries.GetCompanyCosts;
using Construction.Application.Features.Costs.Queries.GetCostRecords;
using Construction.Application.Features.Finance;
using Construction.Application.Features.TimeEntries;
using Construction.Application.Features.TimeEntries.Queries.GetTimeEntries;
using Construction.Application.Features.TimeEntries.Queries.GetTimeEntrySummary;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// A person employed by one unit can work on a site of another. Hours and pay are then counted
/// under the unit whose site it was (Site) or under the unit that employed the person that very
/// day (Employer) — and the second follows the dated history, not where the person is today.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class BranchBasisTests : IntegrationTestBase
{
    public BranchBasisTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    // Years before 2026 in a range no other test class uses, for the cost reports.
    private static int nextYear = 1750;

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private static async Task<User> SeedFinanceAdminAsync(TestScope scope)
    {
        var user = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
        user.FinanceAccess = FinanceAccess.Full;
        await scope.Db.SaveChangesAsync();

        return user;
    }

    private sealed record World(Branch Employer1, Branch Employer2, Branch SiteUnit, Employee Person, Project Site);

    /// <summary>
    /// A person employed by unit 1 until the end of May and by unit 2 from 1 June, working on a site
    /// that belongs to a third unit.
    /// </summary>
    private Task<World> SeedWorldAsync(int year) => InScope(async scope =>
    {
        var first = new Branch { Name = $"Emp1 {Unique()}", Color = "#3457D5" };
        var second = new Branch { Name = $"Emp2 {Unique()}", Color = "#0F8A5F" };
        var siteUnit = new Branch { Name = $"Site {Unique()}", Color = "#7C3AED" };
        scope.Db.Branches.AddRange(first, second, siteUnit);

        var person = await TestData.SeedEmployeeAsync(scope);
        var site = await TestData.SeedProjectAsync(scope);
        site.BranchId = siteUnit.Id;
        await scope.Db.SaveChangesAsync();

        scope.Db.EmployeeBranches.AddRange(
            new EmployeeBranch { EmployeeId = person.Id, BranchId = first.Id, StartDate = new DateOnly(year, 1, 1), EndDate = new DateOnly(year, 5, 31) },
            new EmployeeBranch { EmployeeId = person.Id, BranchId = second.Id, StartDate = new DateOnly(year, 6, 1) });
        await scope.Db.SaveChangesAsync();

        return new World(first, second, siteUnit, person, site);
    });

    private Task SeedShiftAsync(World world, DateOnly day) => InScope(async scope =>
    {
        var startedAt = day.ToDateTime(new TimeOnly(7, 0), DateTimeKind.Utc);

        scope.Db.TimeEntries.Add(new TimeEntry
        {
            EmployeeId = world.Person.Id,
            ProjectId = world.Site.Id,
            StartedAt = startedAt,
            EndedAt = startedAt.AddHours(8),
            WorkType = WorkType.Regular,
            Status = TimeEntryStatus.Approved,
        });

        await scope.Db.SaveChangesAsync();
    });

    private static async Task<IReadOnlyList<DateOnly>> HoursAsync(TestScope scope, Guid employeeId, Guid branchId, BranchBasis basis)
    {
        var page = await scope.Send(new GetTimeEntriesQuery
        {
            EmployeeId = employeeId,
            BranchId = branchId,
            Basis = basis,
            PageSize = 100,
        });

        return page.Items.Select(t => DateOnly.FromDateTime(t.StartedAt)).OrderBy(d => d).ToList();
    }

    [Fact]
    public async Task Hours_follow_the_site_or_the_unit_that_employed_the_person_that_day()
    {
        var year = Interlocked.Increment(ref nextYear);
        var world = await SeedWorldAsync(year);
        var march = new DateOnly(year, 3, 10);
        var july = new DateOnly(year, 7, 10);

        await SeedShiftAsync(world, march);
        await SeedShiftAsync(world, july);

        await InScope(async scope =>
        {
            ActAs(scope, await SeedFinanceAdminAsync(scope));

            // By site: both shifts were worked on the third unit's site, whoever employed the person.
            Assert.Equal([march, july], await HoursAsync(scope, world.Person.Id, world.SiteUnit.Id, BranchBasis.Site));
            Assert.Empty(await HoursAsync(scope, world.Person.Id, world.Employer1.Id, BranchBasis.Site));

            // By employer: March was unit 1's, July unit 2's — the history, not where they are today.
            Assert.Equal([march], await HoursAsync(scope, world.Person.Id, world.Employer1.Id, BranchBasis.Employer));
            Assert.Equal([july], await HoursAsync(scope, world.Person.Id, world.Employer2.Id, BranchBasis.Employer));

            // The site's own unit employs nobody here, so by employer it has none of these hours.
            Assert.Empty(await HoursAsync(scope, world.Person.Id, world.SiteUnit.Id, BranchBasis.Employer));
        });
    }

    [Fact]
    public async Task A_day_covered_by_no_employment_belongs_to_no_unit_by_employer()
    {
        var year = Interlocked.Increment(ref nextYear);
        var world = await SeedWorldAsync(year);
        var before = new DateOnly(year - 1, 12, 20);

        await SeedShiftAsync(world, before);

        await InScope(async scope =>
        {
            ActAs(scope, await SeedFinanceAdminAsync(scope));

            Assert.Empty(await HoursAsync(scope, world.Person.Id, world.Employer1.Id, BranchBasis.Employer));
            Assert.Empty(await HoursAsync(scope, world.Person.Id, world.Employer2.Id, BranchBasis.Employer));
        });
    }

    [Fact]
    public async Task The_hours_summary_can_be_narrowed_the_same_way()
    {
        var year = Interlocked.Increment(ref nextYear);
        var world = await SeedWorldAsync(year);

        await SeedShiftAsync(world, new DateOnly(year, 3, 10));
        await SeedShiftAsync(world, new DateOnly(year, 7, 10));

        await InScope(async scope =>
        {
            ActAs(scope, await SeedFinanceAdminAsync(scope));

            async Task<int> MinutesAsync(Guid branchId, BranchBasis basis)
            {
                var summary = await scope.Send(new GetTimeEntrySummaryQuery
                {
                    From = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    To = new DateTime(year, 12, 31, 23, 59, 0, DateTimeKind.Utc),
                    EmployeeId = world.Person.Id,
                    BranchId = branchId,
                    Basis = basis,
                });

                return summary.Rows.Sum(r => r.TotalMinutes);
            }

            Assert.Equal(16 * 60, await MinutesAsync(world.SiteUnit.Id, BranchBasis.Site));
            Assert.Equal(8 * 60, await MinutesAsync(world.Employer1.Id, BranchBasis.Employer));
            Assert.Equal(8 * 60, await MinutesAsync(world.Employer2.Id, BranchBasis.Employer));
            Assert.Equal(0, await MinutesAsync(world.Employer1.Id, BranchBasis.Site));
        });
    }

    [Fact]
    public async Task Manual_pay_in_the_company_costs_follows_the_employer_of_that_day()
    {
        var year = Interlocked.Increment(ref nextYear);
        var world = await SeedWorldAsync(year);

        await InScope(async scope =>
        {
            var admin = await SeedFinanceAdminAsync(scope);
            ActAs(scope, admin);

            // Paid in March (unit 1 employed them) and in July (unit 2). Neither is booked to a project.
            scope.Db.FinanceEntries.AddRange(
                new FinanceEntry { EmployeeId = world.Person.Id, Kind = FinanceEntryKind.WorkerPaymentDaily, Amount = 100m, OccurredOn = new DateOnly(year, 3, 10) },
                new FinanceEntry { EmployeeId = world.Person.Id, Kind = FinanceEntryKind.WorkerPaymentDaily, Amount = 70m, OccurredOn = new DateOnly(year, 7, 10) });
            await scope.Db.SaveChangesAsync();

            async Task<decimal> PayAsync(Guid? branchId, BranchBasis basis) =>
                (await scope.Send(new GetCompanyCostsQuery
                {
                    From = new DateOnly(year, 1, 1),
                    To = new DateOnly(year, 12, 31),
                    BranchId = branchId,
                    Basis = basis,
                })).ManualPay;

            Assert.Equal(170m, await PayAsync(null, BranchBasis.Site));
            Assert.Equal(100m, await PayAsync(world.Employer1.Id, BranchBasis.Employer));
            Assert.Equal(70m, await PayAsync(world.Employer2.Id, BranchBasis.Employer));

            // By site there is no site on these entries, so no unit owns them.
            Assert.Equal(0m, await PayAsync(world.SiteUnit.Id, BranchBasis.Site));
            Assert.Equal(0m, await PayAsync(world.Employer1.Id, BranchBasis.Site));
        });
    }

    [Fact]
    public async Task By_employer_the_units_add_up_to_the_pay_of_the_people_they_employed()
    {
        var year = Interlocked.Increment(ref nextYear);
        var world = await SeedWorldAsync(year);

        await InScope(async scope =>
        {
            ActAs(scope, await SeedFinanceAdminAsync(scope));

            scope.Db.FinanceEntries.AddRange(
                new FinanceEntry { EmployeeId = world.Person.Id, Kind = FinanceEntryKind.WorkerPaymentDaily, Amount = 55m, OccurredOn = new DateOnly(year, 2, 2) },
                new FinanceEntry { EmployeeId = world.Person.Id, Kind = FinanceEntryKind.WorkerPaymentDaily, Amount = 45m, OccurredOn = new DateOnly(year, 9, 9) });
            await scope.Db.SaveChangesAsync();

            async Task<decimal> PayAsync(Guid? branchId, BranchBasis basis) =>
                (await scope.Send(new GetCompanyCostsQuery { From = new DateOnly(year, 1, 1), To = new DateOnly(year, 12, 31), BranchId = branchId, Basis = basis })).ManualPay;

            var all = await PayAsync(null, BranchBasis.Employer);
            var one = await PayAsync(world.Employer1.Id, BranchBasis.Employer);
            var two = await PayAsync(world.Employer2.Id, BranchBasis.Employer);

            Assert.Equal(100m, all);
            Assert.Equal(all, one + two);
        });
    }

    [Fact]
    public async Task The_finance_entry_list_follows_the_same_choice()
    {
        var year = Interlocked.Increment(ref nextYear);
        var world = await SeedWorldAsync(year);

        await InScope(async scope =>
        {
            ActAs(scope, await SeedFinanceAdminAsync(scope));

            scope.Db.FinanceEntries.AddRange(
                new FinanceEntry { EmployeeId = world.Person.Id, Kind = FinanceEntryKind.WorkerPaymentDaily, Amount = 11m, OccurredOn = new DateOnly(year, 4, 4) },
                new FinanceEntry { EmployeeId = world.Person.Id, Kind = FinanceEntryKind.WorkerPaymentDaily, Amount = 22m, OccurredOn = new DateOnly(year, 8, 8) });
            await scope.Db.SaveChangesAsync();

            var inFirst = await scope.Send(new GetFinanceEntriesQuery
            {
                EmployeeId = world.Person.Id,
                BranchId = world.Employer1.Id,
                Basis = BranchBasis.Employer,
                PageSize = 50,
            });

            Assert.Equal([11m], inFirst.Items.Select(e => e.Amount).ToArray());
        });
    }

    [Fact]
    public async Task Labour_cost_is_counted_under_the_site_unit_or_the_employing_unit_and_both_add_up()
    {
        var year = Interlocked.Increment(ref nextYear);
        var world = await SeedWorldAsync(year);

        await SeedShiftAsync(world, new DateOnly(year, 3, 10));   // 8 h, employed by unit 1
        await SeedShiftAsync(world, new DateOnly(year, 7, 10));   // 8 h, employed by unit 2

        await InScope(async scope =>
        {
            ActAs(scope, await SeedFinanceAdminAsync(scope));

            scope.Db.EmployeeRates.Add(new EmployeeRate
            {
                EmployeeId = world.Person.Id,
                RateType = RateType.Hourly,
                HourlyRate = 10m,
                StartDate = new DateOnly(year, 1, 1),
            });
            await scope.Db.SaveChangesAsync();

            async Task<decimal> LabourAsync(Guid? branchId, BranchBasis basis) =>
                (await scope.Send(new GetCompanyCostsQuery { From = new DateOnly(year, 1, 1), To = new DateOnly(year, 12, 31), BranchId = branchId, Basis = basis })).Labour;

            // 16 hours at 10 in all; the site's unit owns all of it by site, the two employers half each by employer.
            Assert.Equal(160m, await LabourAsync(null, BranchBasis.Site));
            Assert.Equal(160m, await LabourAsync(world.SiteUnit.Id, BranchBasis.Site));
            Assert.Equal(0m, await LabourAsync(world.Employer1.Id, BranchBasis.Site));

            Assert.Equal(80m, await LabourAsync(world.Employer1.Id, BranchBasis.Employer));
            Assert.Equal(80m, await LabourAsync(world.Employer2.Id, BranchBasis.Employer));
            Assert.Equal(0m, await LabourAsync(world.SiteUnit.Id, BranchBasis.Employer));
        });
    }
}
