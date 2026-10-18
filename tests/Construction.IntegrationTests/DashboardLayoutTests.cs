using Construction.Application.Features.Dashboard.Commands;
using Construction.Application.Features.Dashboard.Models;
using Construction.Application.Features.Dashboard.Queries;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class DashboardLayoutTests : IntegrationTestBase
{
    public DashboardLayoutTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static DashboardWidgetDto Widget(string type, int x, int y, IReadOnlyDictionary<string, string>? settings = null) =>
        new() { Id = Guid.NewGuid(), Type = type, X = x, Y = y, W = 6, H = 12, Settings = settings };

    private Task<DashboardLayoutDto> SaveThenReadAsync(params DashboardWidgetDto[] widgets) =>
        InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, UserRole.Admin);
            scope.CurrentUser.SignInAs(user.Id, UserRole.Admin, null, user.Email);

            await scope.Send(new SaveDashboardLayoutCommand { Widgets = widgets });

            return await scope.Send(new GetDashboardLayoutQuery());
        });

    [Fact]
    public async Task A_first_visit_gets_the_six_widgets_that_are_looked_at_every_day()
    {
        var layout = await InScope(async scope =>
        {
            var user = await TestData.SeedUserAsync(scope, UserRole.Admin);
            scope.CurrentUser.SignInAs(user.Id, UserRole.Admin, null, user.Email);

            return await scope.Send(new GetDashboardLayoutQuery());
        });

        Assert.Equal(DashboardWidgetTypes.Default, layout.Widgets.Select(w => w.Type).ToList());
        Assert.Equal(6, layout.Widgets.Count);
        Assert.Contains(layout.Widgets, w => w.Type == DashboardWidgetTypes.Finance);
        Assert.DoesNotContain(layout.Widgets, w => DashboardWidgetTypes.LegacyFinance.Contains(w.Type));
    }

    [Fact]
    public async Task A_saved_layout_is_left_exactly_as_it_was()
    {
        var layout = await SaveThenReadAsync(
            Widget(DashboardWidgetTypes.NeedsAttention, 0, 0),
            Widget(DashboardWidgetTypes.LiveMap, 6, 0));

        Assert.Equal(
            [DashboardWidgetTypes.NeedsAttention, DashboardWidgetTypes.LiveMap],
            layout.Widgets.Select(w => w.Type).ToList());
    }

    [Fact]
    public async Task Five_old_finance_cards_become_one_in_the_place_of_the_first()
    {
        var layout = await SaveThenReadAsync(
            Widget(DashboardWidgetTypes.NeedsAttention, 0, 0),
            Widget(DashboardWidgetTypes.FinanceOverview, 6, 0),
            Widget(DashboardWidgetTypes.CostTrend, 0, 12),
            Widget(DashboardWidgetTypes.CostBreakdown, 6, 12),
            Widget(DashboardWidgetTypes.SpendingTrend, 0, 24),
            Widget(DashboardWidgetTypes.FinanceStatistics, 6, 24),
            Widget(DashboardWidgetTypes.LiveMap, 0, 36));

        Assert.Equal(
            [DashboardWidgetTypes.NeedsAttention, DashboardWidgetTypes.Finance, DashboardWidgetTypes.LiveMap],
            layout.Widgets.Select(w => w.Type).ToList());

        var finance = layout.Widgets.Single(w => w.Type == DashboardWidgetTypes.Finance);
        Assert.Equal(6, finance.X);
        Assert.Equal(0, finance.Y);
        Assert.Null(finance.Settings);
    }

    [Theory]
    [InlineData(DashboardWidgetTypes.CostTrend, "trend", "months")]
    [InlineData(DashboardWidgetTypes.SpendingTrend, "trend", "period")]
    public async Task The_new_card_opens_on_the_tab_the_old_one_showed(string legacy, string tab, string trend)
    {
        var layout = await SaveThenReadAsync(Widget(legacy, 0, 0));

        var finance = Assert.Single(layout.Widgets);
        Assert.Equal(DashboardWidgetTypes.Finance, finance.Type);
        Assert.Equal(tab, finance.Settings!["tab"]);
        Assert.Equal(trend, finance.Settings["trend"]);
    }

    [Fact]
    public async Task A_cost_breakdown_keeps_the_project_it_was_set_to()
    {
        var project = Guid.NewGuid().ToString();

        var layout = await SaveThenReadAsync(
            Widget(DashboardWidgetTypes.CostBreakdown, 0, 0, new Dictionary<string, string> { ["projectId"] = project }));

        var finance = Assert.Single(layout.Widgets);
        Assert.Equal("structure", finance.Settings!["tab"]);
        Assert.Equal(project, finance.Settings["projectId"]);
    }

    [Fact]
    public async Task An_existing_finance_card_is_not_joined_by_a_second_one()
    {
        var layout = await SaveThenReadAsync(
            Widget(DashboardWidgetTypes.Finance, 0, 0),
            Widget(DashboardWidgetTypes.CostTrend, 6, 0));

        var finance = Assert.Single(layout.Widgets);
        Assert.Equal(DashboardWidgetTypes.Finance, finance.Type);
    }
}
