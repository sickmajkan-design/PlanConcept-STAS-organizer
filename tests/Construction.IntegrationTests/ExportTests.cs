using ClosedXML.Excel;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Costs.Commands.RecordVehicleExpense;
using Construction.Application.Features.Exports.Queries;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The exports are read back with a spreadsheet reader rather than checked for
/// a non-empty byte array.
/// </summary>
/// <remarks>
/// A workbook that opens is not the bar. The bar is that a date sorts, a
/// money column sums, and a total past 24 hours does not wrap back round to
/// zero — none of which a length assertion can tell you, and all of which are
/// the reason this is an .xlsx and not a CSV.
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class ExportTests : IntegrationTestBase
{
    public ExportTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static readonly DateOnly March = new(2026, 3, 2);

    private static void ActAs(TestScope scope, User user, Guid? employeeId = null) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, employeeId, user.Email);

    private static IXLWorksheet Open(ExportFile file)
    {
        using var stream = new MemoryStream(file.Content);
        var workbook = new XLWorkbook(stream);

        return workbook.Worksheets.First();
    }

    [Fact]
    public async Task A_timesheet_export_writes_hours_as_a_duration_not_as_text()
    {
        // The point of the format: somebody sums the column. Text would add
        // up to nothing and look fine doing it.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await SeedApprovedShiftAsync(employee.Id, project.Id, March, hours: 8, breakMinutes: 30);

        var file = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new ExportTimeEntriesQuery
            {
                From = March,
                To = March.AddDays(7),
                EmployeeId = employee.Id
            });
        });

        var sheet = Open(file);

        // Column 7 is the worked duration; row 2 is the first data row.
        var worked = sheet.Cell(2, 7);

        // 450 minutes as a fraction of a 1440-minute day, which is how Excel
        // stores a duration and therefore what makes the column summable.
        //
        // Asserted on the value rather than on `DataType`, which reports
        // DateTime here: ClosedXML classifies a cell by its number format, and
        // `[h]:mm` is a time format. The stored value is a plain number either
        // way — verified by reading it straight back as one.
        Assert.Equal(450d / 1440d, worked.GetValue<double>(), 6);

        // The square brackets are the load-bearing part: without them a
        // monthly total past 24 hours wraps back round to zero.
        Assert.Equal("[h]:mm", worked.Style.NumberFormat.Format);
    }

    [Fact]
    public async Task A_timesheet_export_writes_the_date_as_a_date()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await SeedApprovedShiftAsync(employee.Id, project.Id, March, hours: 8);

        var file = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new ExportTimeEntriesQuery
            {
                From = March,
                To = March.AddDays(7),
                EmployeeId = employee.Id
            });
        });

        var sheet = Open(file);
        var date = sheet.Cell(2, 3);

        Assert.Equal(XLDataType.DateTime, date.DataType);
        Assert.Equal(March.ToDateTime(TimeOnly.MinValue), date.GetDateTime());
    }

    [Fact]
    public async Task Headings_come_back_in_the_language_that_was_asked_for()
    {
        // The file outlives the request: it gets emailed to an accountant
        // whose browser never had an opinion about the language.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await SeedApprovedShiftAsync(employee.Id, project.Id, March, hours: 8);

        var serbian = Open(await ExportAsync(admin, employee.Id, language: null));
        var english = Open(await ExportAsync(admin, employee.Id, language: "en"));

        Assert.Equal("Radnik", serbian.Cell(1, 1).GetString());
        Assert.Equal("Employee", english.Cell(1, 1).GetString());

        // Serbian is the default rather than English: the system is built for
        // a Serbian company, so an unset language gets what most users read.
        Assert.Equal("Radni sati", serbian.Name);
    }

    [Fact]
    public async Task Serbian_diacritics_survive_the_round_trip()
    {
        // The single reason this is a workbook and not a CSV: Excel opens a
        // UTF-8 CSV as Windows-1250 and turns every š into mojibake.
        var employee = await InScope(scope =>
            TestData.SeedEmployeeAsync(scope, firstName: "Đorđe", lastName: "Šućur"));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await SeedApprovedShiftAsync(employee.Id, project.Id, March, hours: 8);

        var sheet = Open(await ExportAsync(admin, employee.Id, language: null));

        Assert.Equal("Đorđe Šućur", sheet.Cell(2, 1).GetString());
    }

    [Fact]
    public async Task An_export_only_carries_hours_somebody_signed_off()
    {
        // A spreadsheet mixing approved and unreviewed hours is not a payroll
        // document, and nothing in the file would say which rows were which.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await SeedApprovedShiftAsync(employee.Id, project.Id, March, hours: 8);
        await SeedApprovedShiftAsync(
            employee.Id, project.Id, March.AddDays(1), hours: 8,
            status: TimeEntryStatus.Submitted);

        var sheet = Open(await ExportAsync(admin, employee.Id, language: null));

        // Header plus exactly one data row.
        Assert.Equal(2, sheet.LastRowUsed()!.RowNumber());
    }

    [Fact]
    public async Task A_worker_cannot_export_timesheets()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new ExportTimeEntriesQuery
            {
                From = March,
                To = March.AddDays(7)
            });
        }));
    }

    [Fact]
    public async Task A_foreman_exporting_project_costs_gets_no_labour_columns()
    {
        // The export goes through the report, so it withholds exactly what the
        // screen does — there is no second place for the rule to drift.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, 0m));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var foreman = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Foreman));

        await SeedApprovedShiftAsync(employee.Id, project.Id, March, hours: 8);
        await SeedIssuedMaterialAsync(foreman, material.Id, project.Id);

        var asForeman = Open(await ExportCostsAsync(foreman, project.Id));
        var asAdmin = Open(await ExportCostsAsync(admin, project.Id));

        Assert.Equal("Gradilište", asForeman.Cell(1, 1).GetString());
        Assert.Equal("Materijal", asForeman.Cell(1, 2).GetString());

        // The office gets the hours and the labour cost in between.
        Assert.Equal("Sati", asAdmin.Cell(1, 2).GetString());
        Assert.Equal("Trošak rada", asAdmin.Cell(1, 3).GetString());
    }

    [Fact]
    public async Task A_cost_export_ends_with_a_total_that_matches_the_report()
    {
        // An exported report that does not add up to the screen it came from
        // is the first thing anyone would query.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, 0m));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var foreman = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Foreman));

        await SeedIssuedMaterialAsync(foreman, material.Id, project.Id);

        var sheet = Open(await ExportCostsAsync(admin, project.Id));
        var lastRow = sheet.LastRowUsed()!.RowNumber();

        Assert.Equal("Sve zajedno", sheet.Cell(lastRow, 1).GetString());

        // 10 units at 30 each, issued to the site.
        var total = sheet.Cell(lastRow, sheet.LastColumnUsed()!.ColumnNumber());
        Assert.Equal(XLDataType.Number, total.DataType);
        Assert.Equal(300d, total.GetDouble(), 2);
    }

    [Fact]
    public async Task A_fleet_export_leaves_the_consumption_total_blank()
    {
        // An average of averages is not the fleet's consumption, and a
        // plausible wrong number is worse than a gap.
        var vehicle = await InScope(scope => TestData.SeedVehicleAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new RecordVehicleExpenseCommand
            {
                VehicleId = vehicle.Id,
                Kind = VehicleExpenseKind.Fuel,
                Amount = 10_000m,
                Litres = 50m,
                OdometerKm = 100_000,
                OccurredOn = March
            });
        });

        var file = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new ExportVehicleCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                VehicleId = vehicle.Id
            });
        });

        var sheet = Open(file);
        var lastRow = sheet.LastRowUsed()!.RowNumber();

        Assert.Equal("Sve zajedno", sheet.Cell(lastRow, 1).GetString());
        // Column 5 is l/100 km.
        Assert.True(sheet.Cell(lastRow, 5).IsEmpty());
    }

    [Fact]
    public async Task A_period_wider_than_the_limit_is_refused()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => InScope(scope =>
            {
                ActAs(scope, admin);
                return scope.Send(new ExportTimeEntriesQuery
                {
                    From = March,
                    To = March.AddDays(ExportQueryValidator<ExportTimeEntriesQuery>.MaxDays + 1)
                });
            }));
    }

    [Fact]
    public async Task The_file_is_named_after_what_it_holds_and_when()
    {
        // A folder of exports has to still be readable a month later.
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var file = await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new ExportTimeEntriesQuery
            {
                From = March,
                To = March.AddDays(6)
            });
        });

        Assert.Equal("work-hours-2026-03-02-2026-03-08.xlsx", file.FileName);
        Assert.All(file.FileName, c => Assert.True(c < 128, "the name must stay ASCII"));
    }

    // ---- helpers ---------------------------------------------------------

    private Task<ExportFile> ExportAsync(User actor, Guid employeeId, string? language) =>
        InScope(scope =>
        {
            ActAs(scope, actor);
            return scope.Send(new ExportTimeEntriesQuery
            {
                From = March,
                To = March.AddDays(7),
                EmployeeId = employeeId,
                Language = language
            });
        });

    private Task<ExportFile> ExportCostsAsync(User actor, Guid projectId) =>
        InScope(scope =>
        {
            ActAs(scope, actor);
            return scope.Send(new ExportProjectCostsQuery
            {
                From = March,
                To = March.AddMonths(1),
                ProjectId = projectId
            });
        });

    private Task SeedIssuedMaterialAsync(User actor, Guid materialId, Guid projectId) =>
        InScope(async scope =>
        {
            ActAs(scope, actor);

            await scope.Send(
                new Application.Features.Costs.Commands.RecordMaterialMovement
                    .RecordMaterialMovementCommand
                {
                    MaterialId = materialId,
                    Kind = MaterialMovementKind.In,
                    Quantity = 100m,
                    UnitPrice = 30m,
                    OccurredOn = March
                });

            await scope.Send(
                new Application.Features.Costs.Commands.RecordMaterialMovement
                    .RecordMaterialMovementCommand
                {
                    MaterialId = materialId,
                    Kind = MaterialMovementKind.Out,
                    Quantity = 10m,
                    ProjectId = projectId,
                    OccurredOn = March.AddDays(1)
                });
        });

    private Task SeedApprovedShiftAsync(
        Guid employeeId,
        Guid projectId,
        DateOnly day,
        int hours,
        int breakMinutes = 0,
        TimeEntryStatus status = TimeEntryStatus.Approved) =>
        InScope(async scope =>
        {
            var startedAt = day.ToDateTime(new TimeOnly(7, 0), DateTimeKind.Utc);

            scope.Db.TimeEntries.Add(new TimeEntry
            {
                EmployeeId = employeeId,
                ProjectId = projectId,
                StartedAt = startedAt,
                EndedAt = startedAt.AddHours(hours),
                BreakMinutes = breakMinutes,
                WorkType = WorkType.Regular,
                Status = status
            });

            await scope.Db.SaveChangesAsync();
        });
}
