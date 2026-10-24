using Construction.Application.Features.Ledgers.Commands;
using Construction.Application.Features.Ledgers.Commands.CreateLedger;
using Construction.Application.Features.Ledgers.Commands.SetLedgerCell;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.Ledgers.Queries.GetLedgerChecks;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The office's approved weekly site report says how many hours a site worked in a whole week. The month's sheet
/// says so when the workers' typed hours for that week do not add up to it, and keeps quiet when they do, when
/// the report is not approved, or when the week is cut by the month's end.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class WeeklyReportLedgerCheckTests : IntegrationTestBase
{
    public WeeklyReportLedgerCheckTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>September 2026: the 7th to the 13th is a whole week (ISO 37); the 1st to the 6th is cut by the month's start.</summary>
    private async Task<IReadOnlyList<LedgerCheckDto>> ChecksAsync(
        int weekNumber, string typedHours, decimal? approvedHours, WeeklyReportStatus status = WeeklyReportStatus.Processed)
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope, $"Gradiliste {Unique()}"));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var ledger = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new CreateLedgerCommand
            {
                Name = $"Obračun {Unique()}",
                Year = 2026,
                Month = 9,
                Template = LedgerTemplates.Payroll,
            });
        });

        var section = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new AddLedgerSectionCommand { LedgerId = ledger.Id, Name = project.Name, ProjectId = project.Id });
        });

        var row = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new AddLedgerRowCommand { SectionId = section.Id, Label = "Radnik" });
        });

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new SetLedgerCellCommand
            {
                RowId = row.Id,
                ColumnId = ledger.Columns.Single(c => c.SystemKey == LedgerTemplates.Keys.Week(weekNumber)).Id,
                Value = typedHours,
            });
        });

        var week = LedgerTemplates.MonthWeeks(2026, 9)[weekNumber - 1];

        if (approvedHours is { } hours)
        {
            await InScope(async scope =>
            {
                scope.Db.WeeklySiteReports.Add(new WeeklySiteReport
                {
                    ProjectId = project.Id,
                    SubmittedByEmployeeId = employee.Id,
                    IsoYear = week.IsoYear,
                    IsoWeek = week.IsoWeek,
                    Type = WeeklyReportType.SignedHours,
                    Quantity = hours,
                    FileName = "satnica.pdf",
                    ContentType = "application/pdf",
                    SizeBytes = 10,
                    StorageKey = $"weekly-site-reports/{Guid.NewGuid():N}.pdf",
                    Status = status,
                });
                await scope.Db.SaveChangesAsync();
            });
        }

        return await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new GetLedgerChecksQuery { LedgerId = ledger.Id });
        });
    }

    [Fact]
    public async Task Hours_that_differ_from_the_approved_report_are_flagged_with_both_figures()
    {
        var checks = await ChecksAsync(weekNumber: 2, typedHours: "40", approvedHours: 80m);

        var check = Assert.Single(checks, c => c.Kind == LedgerCheckKinds.HoursDifferFromReport);
        Assert.Equal(40m, check.Amount);
        Assert.Equal(80m, check.ReferenceAmount);
    }

    [Fact]
    public async Task Hours_that_match_the_approved_report_are_not_flagged()
    {
        var checks = await ChecksAsync(weekNumber: 2, typedHours: "80", approvedHours: 80m);

        Assert.DoesNotContain(checks, c => c.Kind == LedgerCheckKinds.HoursDifferFromReport);
    }

    [Fact]
    public async Task A_report_that_is_not_approved_says_nothing()
    {
        var waiting = await ChecksAsync(2, "40", 80m, WeeklyReportStatus.Submitted);
        var sentBack = await ChecksAsync(2, "40", 80m, WeeklyReportStatus.CorrectionRequested);

        Assert.DoesNotContain(waiting, c => c.Kind == LedgerCheckKinds.HoursDifferFromReport);
        Assert.DoesNotContain(sentBack, c => c.Kind == LedgerCheckKinds.HoursDifferFromReport);
    }

    [Fact]
    public async Task A_week_cut_by_the_start_of_the_month_is_not_compared()
    {
        // 1-6 September holds only some of the days of its ISO week, so a whole week's hours cannot be set against it.
        var checks = await ChecksAsync(weekNumber: 1, typedHours: "40", approvedHours: 80m);

        Assert.DoesNotContain(checks, c => c.Kind == LedgerCheckKinds.HoursDifferFromReport);
    }
}
