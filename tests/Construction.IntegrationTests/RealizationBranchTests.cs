using Construction.Application.Features.Projects.Realization.Queries;
using Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReportsSummary;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The home board's realization and weekly-report widgets follow the business unit chosen in the header: a unit
/// sees its own sites and their payments, not the whole company's.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class RealizationBranchTests : IntegrationTestBase
{
    public RealizationBranchTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private sealed record World(Branch Mine, Branch Other, Project MySite, Project OtherSite);

    private Task<World> SeedAsync() => InScope(async scope =>
    {
        var mine = new Branch { Name = $"Mine {Unique()}", Color = "#3457D5" };
        var other = new Branch { Name = $"Other {Unique()}", Color = "#D55734" };
        scope.Db.Branches.AddRange(mine, other);

        var mySite = await TestData.SeedProjectAsync(scope);
        var otherSite = await TestData.SeedProjectAsync(scope);
        await scope.Db.SaveChangesAsync();

        mySite.BranchId = mine.Id;
        otherSite.BranchId = other.Id;
        scope.Db.ProjectRevenues.Add(new ProjectRevenue { ProjectId = mySite.Id, Amount = 100m, OccurredOn = new DateOnly(2026, 3, 1) });
        scope.Db.ProjectRevenues.Add(new ProjectRevenue { ProjectId = otherSite.Id, Amount = 900m, OccurredOn = new DateOnly(2026, 3, 1) });
        await scope.Db.SaveChangesAsync();

        return new World(mine, other, mySite, otherSite);
    });

    [Fact]
    public async Task The_plan_and_the_payments_cover_only_the_chosen_unit()
    {
        var world = await SeedAsync();

        var plan = await InScope(scope => scope.Send(new GetAnnualRealizationPlanQuery { Year = 2026, BranchId = world.Mine.Id }));

        var row = Assert.Single(plan.Rows);
        Assert.Equal(world.MySite.Id, row.ProjectId);
        Assert.Equal(100m, row.RealizedThisYear);

        var payments = await InScope(scope => scope.Send(new GetProjectRevenuesQuery { PageSize = 50, BranchId = world.Other.Id }));

        var payment = Assert.Single(payments.Items);
        Assert.Equal(900m, payment.Amount);

        var whole = await InScope(scope => scope.Send(new GetAnnualRealizationPlanQuery { Year = 2026 }));
        Assert.Contains(whole.Rows, r => r.ProjectId == world.OtherSite.Id);
    }

    [Fact]
    public async Task The_weekly_report_summary_can_be_narrowed_to_a_unit_with_nothing_in_it()
    {
        var world = await SeedAsync();

        var summary = await InScope(scope => scope.Send(new GetWeeklySiteReportsSummaryQuery { BranchId = world.Mine.Id }));

        Assert.Equal(0, summary.AwaitingReview);
        Assert.Empty(summary.Overdue);
    }
}
