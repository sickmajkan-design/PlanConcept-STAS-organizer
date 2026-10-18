using Construction.Application.Features.Dashboard.Snapshots;
using Construction.Domain.Enums;
using Construction.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class DashboardSnapshotTests : IntegrationTestBase
{
    private static readonly DateTime Day1 = new(2031, 6, 10, 9, 0, 0, DateTimeKind.Utc);

    public DashboardSnapshotTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private Task<Guid> NewUserAsync() => InScope(async scope => (await TestData.SeedUserAsync(scope, UserRole.Admin)).Id);

    private Task RecordAsync(Guid userId, DateTime at, params (string Key, decimal Value)[] values) =>
        InScope(scope =>
        {
            scope.Clock.FreezeAt(at);
            scope.CurrentUser.SignInAs(userId, UserRole.Admin, null, "snap@example.com");
            return scope.Send(new RecordDashboardSnapshotsCommand { Values = values.ToDictionary(v => v.Key, v => v.Value) });
        });

    private Task<DashboardSnapshotsDto> ReadAsync(Guid userId, DateTime at) =>
        InScope(scope =>
        {
            scope.Clock.FreezeAt(at);
            scope.CurrentUser.SignInAs(userId, UserRole.Admin, null, "snap@example.com");
            return scope.Send(new GetDashboardSnapshotsQuery());
        });

    [Fact]
    public async Task Yesterdays_last_figure_is_handed_back_the_next_day_and_today_keeps_the_latest()
    {
        var user = await NewUserAsync();

        await RecordAsync(user, Day1, ("attention.absenceRequests|all", 3));
        await RecordAsync(user, Day1.AddHours(5), ("attention.absenceRequests|all", 4));

        var next = await ReadAsync(user, Day1.AddDays(1));

        Assert.Equal(4, next.Yesterday["attention.absenceRequests|all"]);
        Assert.Empty(next.Today);

        await RecordAsync(user, Day1.AddDays(1), ("attention.absenceRequests|all", 6));
        var again = await ReadAsync(user, Day1.AddDays(1).AddHours(2));

        Assert.Equal(6, again.Today["attention.absenceRequests|all"]);
        Assert.Equal(4, again.Yesterday["attention.absenceRequests|all"]);
    }

    [Fact]
    public async Task After_a_gap_there_is_no_baseline_because_only_exactly_yesterday_counts()
    {
        var user = await NewUserAsync();

        await RecordAsync(user, Day1, ("kpi.activeEmployees|all", 20));

        var later = await ReadAsync(user, Day1.AddDays(3));

        Assert.Empty(later.Yesterday);
    }

    [Fact]
    public async Task One_accounts_figures_are_never_shown_to_another()
    {
        var first = await NewUserAsync();
        var second = await NewUserAsync();

        await RecordAsync(first, Day1, ("kpi.activeEmployees|all", 20));

        var other = await ReadAsync(second, Day1.AddDays(1));

        Assert.Empty(other.Yesterday);
    }

    [Fact]
    public async Task Recording_removes_what_is_older_than_the_days_kept()
    {
        var user = await NewUserAsync();

        await RecordAsync(user, Day1, ("kpi.toolsAvailable|all", 5));
        await RecordAsync(user, Day1.AddDays(5), ("kpi.toolsAvailable|all", 6));

        var rows = await InScope(scope => scope.Db.DashboardSnapshots.AsNoTracking().Where(s => s.UserId == user).ToListAsync());

        Assert.Single(rows);
        Assert.Equal(6, rows[0].Value);
    }

    [Fact]
    public async Task An_empty_name_or_too_many_figures_is_refused()
    {
        var user = await NewUserAsync();

        await Assert.ThrowsAsync<ValidationException>(() => RecordAsync(user, Day1, ("", 1)));
        await Assert.ThrowsAsync<ValidationException>(() =>
            RecordAsync(user, Day1, Enumerable.Range(0, 101).Select(i => ($"k{i}|all", (decimal)i)).ToArray()));
    }
}
