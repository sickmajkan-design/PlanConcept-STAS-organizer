using Construction.Application.Common;
using Construction.Application.Features.WeeklySiteReports.Commands.SendWeeklyReportReminders;
using Construction.Application.Features.WeeklySiteReports.Queries.GetWeeklySiteReportsSummary;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The report for a week is due on the Monday after it by noon. The foreman posted to a site that sent nothing
/// is reminded on the Tuesday morning, once, and the office sees which sites are late.
/// </summary>
/// <remarks>
/// Europe/Belgrade is two hours ahead of UTC until the clocks go back on 25 October 2026, so Tuesday 20 October
/// 07:00 is 05:00 UTC, and the week that is over is Monday 12 to Sunday 18 October, ISO week 42.
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class WeeklyReportReminderTests : IntegrationTestBase, IDisposable
{
    private static readonly DateTime MondayMorning = new(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime MondayAfterDeadline = new(2026, 10, 19, 11, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TuesdayEarly = new(2026, 10, 20, 4, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TuesdayMorning = new(2026, 10, 20, 6, 0, 0, DateTimeKind.Utc);

    /// <summary>The suite runs on UTC; these days and hours are Belgrade's, so the clock is Belgrade's for each test.</summary>
    public WeeklyReportReminderTests(DatabaseFixture fixture) : base(fixture)
    {
        CompanyTime.Configure("Europe/Belgrade", out _);
    }

    public new void Dispose()
    {
        CompanyTime.Configure("UTC", out _);
        base.Dispose();
    }

    private sealed record Site(Project Project, User Foreman, Employee Employee);

    private Task<Site> SeedSiteAsync() =>
        InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope);
            var foreman = await TestData.SeedUserAsync(scope, UserRole.Foreman, employee.Id);
            var project = await TestData.SeedProjectAsync(scope);

            scope.Db.EmployeeProjects.Add(new EmployeeProject
            {
                EmployeeId = employee.Id,
                ProjectId = project.Id,
                StartDate = new DateOnly(2026, 10, 1),
            });
            await scope.Db.SaveChangesAsync();

            return new Site(project, foreman, employee);
        });

    private Task SendReportAsync(Site site, WeeklyReportStatus status = WeeklyReportStatus.Submitted) =>
        InScope(async scope =>
        {
            scope.Db.WeeklySiteReports.Add(new WeeklySiteReport
            {
                ProjectId = site.Project.Id,
                SubmittedByEmployeeId = site.Employee.Id,
                IsoYear = 2026,
                IsoWeek = 42,
                Type = WeeklyReportType.SignedHours,
                Quantity = 80,
                FileName = "satnica.pdf",
                ContentType = "application/pdf",
                SizeBytes = 10,
                StorageKey = $"weekly-site-reports/{Guid.NewGuid():N}.pdf",
                Status = status,
            });
            await scope.Db.SaveChangesAsync();
        });

    private Task<int> Sweep(DateTime at) =>
        InScope(scope =>
        {
            scope.Clock.FreezeAt(at);
            return scope.Send(new SendWeeklyReportRemindersCommand());
        });

    private Task<int> RemindersAsync(Site site) =>
        InScope(async scope => await scope.Db.Notifications
            .CountAsync(n => n.UserId == site.Foreman.Id && n.Type == NotificationType.WeeklyReportDue));

    [Fact]
    public async Task Nothing_is_sent_on_Monday_or_before_seven_on_Tuesday()
    {
        var site = await SeedSiteAsync();

        await Sweep(MondayMorning);
        await Sweep(MondayAfterDeadline);
        await Sweep(TuesdayEarly);

        Assert.Equal(0, await RemindersAsync(site));
    }

    [Fact]
    public async Task The_foreman_is_reminded_on_Tuesday_morning_and_only_once()
    {
        var site = await SeedSiteAsync();

        await Sweep(TuesdayMorning);
        await Sweep(TuesdayMorning.AddHours(1));
        await Sweep(TuesdayMorning.AddDays(1));

        Assert.Equal(1, await RemindersAsync(site));
    }

    [Fact]
    public async Task A_site_that_sent_its_report_is_not_reminded()
    {
        var site = await SeedSiteAsync();
        await SendReportAsync(site);

        await Sweep(TuesdayMorning);

        Assert.Equal(0, await RemindersAsync(site));
    }

    [Fact]
    public async Task A_report_sent_back_still_counts_as_sent_so_the_foreman_is_not_nagged_for_a_first_one()
    {
        var site = await SeedSiteAsync();
        await SendReportAsync(site, WeeklyReportStatus.CorrectionRequested);

        await Sweep(TuesdayMorning);

        Assert.Equal(0, await RemindersAsync(site));
    }

    [Fact]
    public async Task The_office_sees_a_site_as_late_only_after_Monday_noon()
    {
        var site = await SeedSiteAsync();

        var before = await InScope(scope =>
        {
            scope.Clock.FreezeAt(MondayMorning);
            return scope.Send(new GetWeeklySiteReportsSummaryQuery());
        });
        var after = await InScope(scope =>
        {
            scope.Clock.FreezeAt(MondayAfterDeadline);
            return scope.Send(new GetWeeklySiteReportsSummaryQuery());
        });

        Assert.DoesNotContain(before.Overdue, o => o.ProjectId == site.Project.Id);
        var late = Assert.Single(after.Overdue, o => o.ProjectId == site.Project.Id);
        Assert.Contains($"{site.Employee.FirstName} {site.Employee.LastName}", late.ForemanNames);
        Assert.Equal(42, after.IsoWeek);
    }

    [Fact]
    public async Task A_site_with_its_report_is_not_late()
    {
        var site = await SeedSiteAsync();
        await SendReportAsync(site);

        var summary = await InScope(scope =>
        {
            scope.Clock.FreezeAt(MondayAfterDeadline);
            return scope.Send(new GetWeeklySiteReportsSummaryQuery());
        });

        Assert.DoesNotContain(summary.Overdue, o => o.ProjectId == site.Project.Id);
    }
}
