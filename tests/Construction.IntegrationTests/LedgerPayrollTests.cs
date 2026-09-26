using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Employees.Commands.AssignEmployeeToProject;
using Construction.Application.Features.Ledgers.Commands;
using Construction.Application.Features.Ledgers.Commands.CreateLedger;
using Construction.Application.Features.Ledgers.Commands.SetLedgerCell;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.Ledgers.Queries.ExportLedger;
using Construction.Application.Features.Ledgers.Queries.GetLedgerById;
using Construction.Application.Features.Ledgers.Queries.GetLedgerChecks;
using Construction.Application.Features.Ledgers.Queries.GetLedgerSectionRows;
using Construction.Application.Features.Ledgers.Queries.GetLedgerSummary;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// The monthly payroll template: a ledger whose columns work themselves out.
/// The numbers here are the arithmetic of the spreadsheet it replaces —
/// pay = rate × hours − advance + difference + holiday, billed = hours × client
/// rate, margin = billed − pay, result = margin less every cost.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class LedgerPayrollTests : IntegrationTestBase
{
    public LedgerPayrollTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private sealed class Month
    {
        public required LedgerDetailDto Ledger { get; init; }

        public required User SuperAdmin { get; init; }

        public Guid Column(string key) =>
            Ledger.Columns.Single(c => c.SystemKey == key).Id;
    }

    private async Task<Month> NewMonthAsync(
        bool populate = false,
        int year = 2026,
        int month = 9,
        bool hoursFromApp = false)
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var ledger = await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(superAdmin.Id, superAdmin.Role, null, superAdmin.Email);

            return scope.Send(new CreateLedgerCommand
            {
                Name = $"Obračun {Unique()}",
                Year = year,
                Month = month,
                // Hours are typed from the signed timesheets unless a test is
                // about the template that takes them from the app.
                Template = hoursFromApp ? LedgerTemplates.PayrollAppHours : LedgerTemplates.Payroll,
                PopulateFromProjects = populate,
            });
        });

        return new Month { Ledger = ledger, SuperAdmin = superAdmin };
    }

    private Task<T> AsSuperAdmin<T>(Month month, Func<TestScope, Task<T>> action) =>
        InScope(scope =>
        {
            scope.CurrentUser.SignInAs(month.SuperAdmin.Id, month.SuperAdmin.Role, null, month.SuperAdmin.Email);
            return action(scope);
        });

    private Task DoAsSuperAdmin(Month month, Func<TestScope, Task> action) =>
        InScope(scope =>
        {
            scope.CurrentUser.SignInAs(month.SuperAdmin.Id, month.SuperAdmin.Role, null, month.SuperAdmin.Email);
            return action(scope);
        });

    private async Task<Guid> AddSectionAsync(Month month, string? name = null) =>
        (await AsSuperAdmin(month, s => s.Send(new AddLedgerSectionCommand
        {
            LedgerId = month.Ledger.Id,
            Name = name ?? $"Klijent {Unique()}",
        }))).Id;

    private async Task<Guid> AddRowAsync(Month month, Guid sectionId, string label = "Radnik", Guid? employeeId = null) =>
        (await AsSuperAdmin(month, s => s.Send(new AddLedgerRowCommand
        {
            SectionId = sectionId,
            Label = label,
            EmployeeId = employeeId,
        }))).Id;

    private Task SetAsync(Month month, Guid rowId, string key, string? value) =>
        DoAsSuperAdmin(month, s => s.Send(new SetLedgerCellCommand
        {
            RowId = rowId,
            ColumnId = month.Column(key),
            Value = value,
        }));

    private async Task<LedgerRowDto> ReadRowAsync(Month month, Guid sectionId, Guid rowId)
    {
        var section = await AsSuperAdmin(month, s => s.Send(new GetLedgerSectionRowsQuery(month.Ledger.Id, sectionId)));

        return section.Rows.Single(r => r.Id == rowId);
    }

    private static decimal Value(Month month, LedgerRowDto row, string key)
    {
        var cell = row.Cells.SingleOrDefault(c => c.ColumnId == month.Column(key));

        return LedgerCellMath.ParseNumeric(cell?.Value);
    }

    /// <summary>The worked example: 20/h for 160 h, billed at 33/h, with the usual costs.</summary>
    private async Task<(Guid Section, Guid Row)> SeedTypicalWorkerAsync(Month month)
    {
        var section = await AddSectionAsync(month);
        var row = await AddRowAsync(month, section);

        await SetAsync(month, row, LedgerTemplates.Keys.WorkerRate, "20");
        await SetAsync(month, row, LedgerTemplates.Keys.Week(1), "40");
        await SetAsync(month, row, LedgerTemplates.Keys.Week(2), "42");
        await SetAsync(month, row, LedgerTemplates.Keys.Week(3), "38");
        await SetAsync(month, row, LedgerTemplates.Keys.Week(4), "40");
        await SetAsync(month, row, LedgerTemplates.Keys.ClientRate, "33");
        await SetAsync(month, row, LedgerTemplates.Keys.Contributions, "1000");
        await SetAsync(month, row, LedgerTemplates.Keys.Housing, "600");
        await SetAsync(month, row, LedgerTemplates.Keys.Bonus, "100");

        return (section, row);
    }

    // ---- the template ----------------------------------------------------

    [Fact]
    public async Task The_template_builds_columns_with_formulas_that_never_loop()
    {
        var month = await NewMonthAsync();

        var formulaColumns = month.Ledger.Columns.Where(c => c.IsFormula).Select(c => c.SystemKey).ToList();

        // The hour columns are not formulas: hours are typed from the signed timesheets.
        var expected = new List<string>
        {
            "hours", "pay", "billing", "margin", "result", "workerRate", "fuel", "rent", "housing",
        };

        Assert.Equal(expected.OrderBy(k => k), formulaColumns.OrderBy(k => k));

        var columns = await InScope(scope => Task.FromResult(scope.Db.LedgerColumns
            .Where(c => c.LedgerId == month.Ledger.Id)
            .Select(c => new { c.Id, c.FormulaJson })
            .ToList()));

        var calculator = new LedgerCalculator(columns.Select(c => (c.Id, LedgerFormula.Parse(c.FormulaJson))));

        Assert.False(calculator.HasCycle());
    }

    [Fact]
    public async Task Pay_billing_margin_and_result_follow_the_spreadsheets_arithmetic()
    {
        var month = await NewMonthAsync();
        var (section, rowId) = await SeedTypicalWorkerAsync(month);

        var row = await ReadRowAsync(month, section, rowId);

        Assert.Equal(160m, Value(month, row, LedgerTemplates.Keys.Hours));
        Assert.Equal(3200m, Value(month, row, LedgerTemplates.Keys.Pay));         // 20 × 160
        Assert.Equal(5280m, Value(month, row, LedgerTemplates.Keys.Billing));     // 160 × 33
        Assert.Equal(2080m, Value(month, row, LedgerTemplates.Keys.Margin));      // 5280 − 3200
        Assert.Equal(380m, Value(month, row, LedgerTemplates.Keys.Result));       // 2080 − 1000 − 600 − 100
    }

    [Fact]
    public async Task An_advance_is_paid_earlier_and_does_not_change_the_firms_result()
    {
        var month = await NewMonthAsync();
        var (section, rowId) = await SeedTypicalWorkerAsync(month);

        await SetAsync(month, rowId, LedgerTemplates.Keys.Advance, "300");

        var row = await ReadRowAsync(month, section, rowId);

        Assert.Equal(2900m, Value(month, row, LedgerTemplates.Keys.Pay));         // 3200 − 300
        Assert.Equal(2380m, Value(month, row, LedgerTemplates.Keys.Margin));
        Assert.Equal(380m, Value(month, row, LedgerTemplates.Keys.Result));       // unchanged
    }

    [Fact]
    public async Task A_missing_value_counts_as_zero_and_a_computed_cell_is_marked_computed()
    {
        var month = await NewMonthAsync();
        var section = await AddSectionAsync(month);
        var rowId = await AddRowAsync(month, section);

        var row = await ReadRowAsync(month, section, rowId);
        var pay = row.Cells.Single(c => c.ColumnId == month.Column(LedgerTemplates.Keys.Pay));

        Assert.Equal(0m, LedgerCellMath.ParseNumeric(pay.Value));
        Assert.True(pay.IsComputed);
        Assert.False(pay.IsOverride);
    }

    // ---- manual overrides ------------------------------------------------

    [Fact]
    public async Task A_typed_value_overrides_the_calculation_and_is_marked_until_cleared()
    {
        var month = await NewMonthAsync();
        var (section, rowId) = await SeedTypicalWorkerAsync(month);

        await SetAsync(month, rowId, LedgerTemplates.Keys.Pay, "3000");

        var overridden = await ReadRowAsync(month, section, rowId);
        var pay = overridden.Cells.Single(c => c.ColumnId == month.Column(LedgerTemplates.Keys.Pay));

        Assert.True(pay.IsOverride);
        Assert.False(pay.IsComputed);
        Assert.Equal(3000m, LedgerCellMath.ParseNumeric(pay.Value));
        Assert.Equal(2280m, Value(month, overridden, LedgerTemplates.Keys.Margin)); // 5280 − 3000

        await SetAsync(month, rowId, LedgerTemplates.Keys.Pay, null);

        var restored = await ReadRowAsync(month, section, rowId);

        Assert.Equal(3200m, Value(month, restored, LedgerTemplates.Keys.Pay));
        Assert.True(restored.Cells.Single(c => c.ColumnId == month.Column(LedgerTemplates.Keys.Pay)).IsComputed);
    }

    // ---- the firm's total ------------------------------------------------

    [Fact]
    public async Task The_summary_adds_every_section_and_nets_income_against_costs()
    {
        var month = await NewMonthAsync();
        await SeedTypicalWorkerAsync(month);
        await SeedTypicalWorkerAsync(month);

        var summary = await AsSuperAdmin(month, s => s.Send(new GetLedgerSummaryQuery(month.Ledger.Id)));

        decimal Box(string label) => summary.Boxes.Single(b => b.Label == label).Value;

        Assert.Equal(4160m, Box("Ukupan prihod (marža)"));   // 2 × 2080
        Assert.Equal(200m, Box("Regres"));
        Assert.Equal(2000m, Box("Doprinosi"));
        Assert.Equal(1200m, Box("Stanovi"));

        // 4160 − 200 − 2000 − 1200, with the manual boxes still at zero.
        Assert.Equal(760m, summary.NetTotal);
    }

    // ---- copying a month -------------------------------------------------

    [Fact]
    public async Task A_copied_month_keeps_working_formulas_and_boxes_but_no_figures()
    {
        var source = await NewMonthAsync();
        await SeedTypicalWorkerAsync(source);

        var copy = await AsSuperAdmin(source, s => s.Send(new CreateLedgerCommand
        {
            Name = $"Kopija {Unique()}",
            Year = 2026,
            Month = 10,
            CopyFromLedgerId = source.Ledger.Id,
        }));

        var next = new Month { Ledger = copy, SuperAdmin = source.SuperAdmin };

        Assert.Equal(source.Ledger.Columns.Count, copy.Columns.Count);
        Assert.Equal(
            source.Ledger.Columns.Select(c => c.SystemKey),
            copy.Columns.Select(c => c.SystemKey));
        Assert.All(copy.Columns.Where(c => c.IsFormula), c => Assert.NotNull(c.SystemKey));

        // The copy's formulas must point at the copy's own columns, not the old ones.
        var sectionId = await AddSectionAsync(next);
        var rowId = await AddRowAsync(next, sectionId);
        await SetAsync(next, rowId, LedgerTemplates.Keys.WorkerRate, "10");
        await SetAsync(next, rowId, LedgerTemplates.Keys.Week(1), "8");

        var row = await ReadRowAsync(next, sectionId, rowId);

        Assert.Equal(80m, Value(next, row, LedgerTemplates.Keys.Pay));

        var summary = await AsSuperAdmin(next, s => s.Send(new GetLedgerSummaryQuery(copy.Id)));

        Assert.Equal(8, summary.Boxes.Count);

        // The copied worker carries last month's regres (100) but not the contributions,
        // which come off each month's payslip, and no hours: a further -100. The row
        // just added earns -80.
        Assert.Equal(-180m, summary.NetTotal);
    }

    // ---- column safety ---------------------------------------------------

    [Fact]
    public async Task A_column_a_formula_uses_cannot_be_removed()
    {
        var month = await NewMonthAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            DoAsSuperAdmin(month, s => s.Send(new DeleteLedgerColumnCommand(month.Column(LedgerTemplates.Keys.Hours)))));
    }

    [Fact]
    public async Task A_computed_column_can_be_renamed_but_keeps_its_type()
    {
        var month = await NewMonthAsync();
        var pay = month.Ledger.Columns.Single(c => c.SystemKey == LedgerTemplates.Keys.Pay);

        var renamed = await AsSuperAdmin(month, s => s.Send(new UpdateLedgerColumnCommand
        {
            Id = pay.Id,
            Name = "Plata radnika",
            DataType = LedgerColumnDataType.Text,
        }));

        Assert.Equal("Plata radnika", renamed.Name);
        Assert.Equal(pay.DataType, renamed.DataType);
        Assert.True(renamed.IsFormula);
        Assert.Equal(LedgerTemplates.Keys.Pay, renamed.SystemKey);
    }

    // ---- checks ----------------------------------------------------------

    [Fact]
    public async Task Hours_without_a_client_price_are_flagged()
    {
        var month = await NewMonthAsync();
        var section = await AddSectionAsync(month);
        var rowId = await AddRowAsync(month, section, "Luka");

        await SetAsync(month, rowId, LedgerTemplates.Keys.WorkerRate, "14");
        await SetAsync(month, rowId, LedgerTemplates.Keys.Week(1), "40");

        var checks = await AsSuperAdmin(month, s => s.Send(new GetLedgerChecksQuery { LedgerId = month.Ledger.Id }));

        var issue = Assert.Single(checks, c => c.Kind == LedgerCheckKinds.MissingClientRate);
        Assert.Equal("Luka", issue.RowLabel);
        Assert.Equal(40m, issue.Amount);
    }

    [Fact]
    public async Task A_figure_typed_over_a_calculation_is_flagged()
    {
        var month = await NewMonthAsync();
        var (_, rowId) = await SeedTypicalWorkerAsync(month);

        await SetAsync(month, rowId, LedgerTemplates.Keys.Pay, "3000");

        var checks = await AsSuperAdmin(month, s => s.Send(new GetLedgerChecksQuery { LedgerId = month.Ledger.Id }));

        var issue = Assert.Single(checks, c => c.Kind == LedgerCheckKinds.ManualOverride);
        Assert.Equal("Zarada radnika", issue.ColumnName);
    }

    [Fact]
    public async Task One_person_with_more_hours_than_a_month_across_sections_is_flagged()
    {
        var month = await NewMonthAsync();
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        var first = await AddSectionAsync(month, "Prvi klijent");
        var second = await AddSectionAsync(month, "Drugi klijent");
        var rowA = await AddRowAsync(month, first, "Ivo", employee.Id);
        var rowB = await AddRowAsync(month, second, "Ivo", employee.Id);

        await SetAsync(month, rowA, LedgerTemplates.Keys.Week(1), "150");
        await SetAsync(month, rowB, LedgerTemplates.Keys.Week(1), "60");

        var checks = await AsSuperAdmin(month, s => s.Send(new GetLedgerChecksQuery { LedgerId = month.Ledger.Id }));

        var issue = Assert.Single(checks, c => c.Kind == LedgerCheckKinds.HoursAcrossSections);
        Assert.Equal(210m, issue.Amount);
        Assert.Contains("Drugi klijent", issue.OtherSections);
    }

    [Fact]
    public async Task A_clean_month_has_nothing_to_check()
    {
        var month = await NewMonthAsync();

        var checks = await AsSuperAdmin(month, s => s.Send(new GetLedgerChecksQuery { LedgerId = month.Ledger.Id }));

        Assert.Empty(checks);
    }

    // ---- a first draft from the people already on projects ----------------

    [Fact]
    public async Task A_new_month_can_start_with_a_section_per_project_and_a_row_per_person()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope, $"Gradilište {Unique()}"));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope, firstName: "Sanel", lastName: $"Ledger{Unique()}"));

        await InScope(async scope =>
        {
            var admin = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
            scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);
            await scope.Send(new AssignEmployeeToProjectCommand(employee.Id, project.Id));
        });

        var month = await NewMonthAsync(populate: true, year: DateTime.UtcNow.Year, month: DateTime.UtcNow.Month);

        var detail = await AsSuperAdmin(month, s => s.Send(new GetLedgerByIdQuery(month.Ledger.Id)));
        var section = Assert.Single(detail.Sections, s => s.ProjectId == project.Id);
        var rows = await AsSuperAdmin(month, s => s.Send(new GetLedgerSectionRowsQuery(month.Ledger.Id, section.Id)));

        var row = Assert.Single(rows.Rows);
        Assert.Equal(employee.Id, row.EmployeeId);
        Assert.Equal($"Sanel {employee.LastName}", row.Label);
    }

    // ---- figures the system already knows -------------------------------

    /// <summary>Someone on a project, with shifts on chosen days of September 2026.</summary>
    private async Task<(Employee Employee, Project Project)> SeedWorkerWithShiftsAsync(
        (int Day, int Hours, TimeEntryStatus Status)[] shifts,
        decimal? hourlyRate = null)
    {
        return await InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope, firstName: "Sanel", lastName: $"Sat{Unique()}");
            var project = await TestData.SeedProjectAsync(scope, $"Gradiliste {Unique()}");

            var admin = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
            scope.CurrentUser.SignInAs(admin.Id, admin.Role, null, admin.Email);
            await scope.Send(new AssignEmployeeToProjectCommand(employee.Id, project.Id));

            foreach (var (day, hours, status) in shifts)
            {
                var start = new DateTime(2026, 9, day, 8, 0, 0, DateTimeKind.Utc);

                scope.Db.TimeEntries.Add(new TimeEntry
                {
                    EmployeeId = employee.Id,
                    ProjectId = project.Id,
                    StartedAt = start,
                    EndedAt = start.AddHours(hours),
                    Status = status,
                });
            }

            if (hourlyRate is { } rate)
            {
                scope.Db.EmployeeRates.Add(new EmployeeRate
                {
                    EmployeeId = employee.Id,
                    RateType = RateType.Hourly,
                    HourlyRate = rate,
                    StartDate = new DateOnly(2026, 1, 1),
                });
            }

            await scope.Db.SaveChangesAsync();

            return (employee, project);
        });
    }

    private async Task<(Guid Section, Guid Row)> RowForAsync(Month month, Employee employee, Project project)
    {
        var section = (await AsSuperAdmin(month, s => s.Send(new AddLedgerSectionCommand
        {
            LedgerId = month.Ledger.Id,
            Name = project.Name,
            ProjectId = project.Id,
        }))).Id;

        return (section, await AddRowAsync(month, section, $"{employee.FirstName} {employee.LastName}", employee.Id));
    }

    [Fact]
    public async Task Hours_come_from_approved_time_entries_week_by_week_on_the_sections_project()
    {
        // September 2026: KW36 is 1-6 Sept, KW37 is 7-13.
        var (employee, project) = await SeedWorkerWithShiftsAsync(
        [
            (1, 8, TimeEntryStatus.Approved),
            (2, 8, TimeEntryStatus.Approved),
            (8, 10, TimeEntryStatus.Approved),
            (9, 6, TimeEntryStatus.Submitted),   // waiting for approval: not counted
        ]);
        var month = await NewMonthAsync(hoursFromApp: true);

        var (section, rowId) = await RowForAsync(month, employee, project);
        var row = await ReadRowAsync(month, section, rowId);

        Assert.Equal(16m, Value(month, row, LedgerTemplates.Keys.Week(1)));
        Assert.Equal(10m, Value(month, row, LedgerTemplates.Keys.Week(2)));
        Assert.Equal(26m, Value(month, row, LedgerTemplates.Keys.Hours));
        Assert.True(row.Cells.Single(c => c.ColumnId == month.Column(LedgerTemplates.Keys.Week(1))).IsComputed);
    }

    [Fact]
    public async Task Hours_on_another_project_are_not_counted_in_this_sections_row()
    {
        var (employee, _) = await SeedWorkerWithShiftsAsync([(1, 8, TimeEntryStatus.Approved)]);
        var other = await InScope(scope => TestData.SeedProjectAsync(scope, $"Drugo {Unique()}"));
        var month = await NewMonthAsync();

        var (section, rowId) = await RowForAsync(month, employee, other);
        var row = await ReadRowAsync(month, section, rowId);

        Assert.Equal(0m, Value(month, row, LedgerTemplates.Keys.Hours));
    }

    [Fact]
    public async Task The_hourly_rate_comes_from_the_employees_rate_in_force()
    {
        var (employee, project) = await SeedWorkerWithShiftsAsync([(1, 10, TimeEntryStatus.Approved)], hourlyRate: 17.5m);
        var month = await NewMonthAsync(hoursFromApp: true);

        var (section, rowId) = await RowForAsync(month, employee, project);
        await SetAsync(month, rowId, LedgerTemplates.Keys.ClientRate, "30");

        var row = await ReadRowAsync(month, section, rowId);

        Assert.Equal(17.5m, Value(month, row, LedgerTemplates.Keys.WorkerRate));
        Assert.Equal(175m, Value(month, row, LedgerTemplates.Keys.Pay));      // 17.5 x 10
        Assert.Equal(300m, Value(month, row, LedgerTemplates.Keys.Billing));  // 10 x 30
        Assert.Equal(125m, Value(month, row, LedgerTemplates.Keys.Margin));
    }

    [Fact]
    public async Task Hours_typed_for_someone_with_no_timesheet_are_ordinary_input_not_an_override()
    {
        var month = await NewMonthAsync();
        var section = await AddSectionAsync(month);
        var rowId = await AddRowAsync(month, section, "Kooperanti");

        await SetAsync(month, rowId, LedgerTemplates.Keys.Week(1), "40");

        var row = await ReadRowAsync(month, section, rowId);
        var cell = row.Cells.Single(c => c.ColumnId == month.Column(LedgerTemplates.Keys.Week(1)));

        Assert.Equal(40m, LedgerCellMath.ParseNumeric(cell.Value));
        Assert.False(cell.IsOverride);
        Assert.False(cell.IsComputed);

        var checks = await AsSuperAdmin(month, s => s.Send(new GetLedgerChecksQuery { LedgerId = month.Ledger.Id }));
        Assert.DoesNotContain(checks, c => c.Kind == LedgerCheckKinds.ManualOverride);
    }

    [Fact]
    public async Task Typing_over_a_timesheet_figure_is_an_override_and_is_flagged_when_it_disagrees()
    {
        var (employee, project) = await SeedWorkerWithShiftsAsync([(1, 8, TimeEntryStatus.Approved)]);
        var month = await NewMonthAsync(hoursFromApp: true);
        var (section, rowId) = await RowForAsync(month, employee, project);

        await SetAsync(month, rowId, LedgerTemplates.Keys.Week(1), "12");

        var row = await ReadRowAsync(month, section, rowId);
        var cell = row.Cells.Single(c => c.ColumnId == month.Column(LedgerTemplates.Keys.Week(1)));

        Assert.True(cell.IsOverride);
        Assert.Equal(12m, LedgerCellMath.ParseNumeric(cell.Value));

        var checks = await AsSuperAdmin(month, s => s.Send(new GetLedgerChecksQuery { LedgerId = month.Ledger.Id }));
        var flagged = Assert.Single(checks, c => c.Kind == LedgerCheckKinds.ManualOverride);
        Assert.Equal(LedgerTemplates.WeekName(1, 2026, 9), flagged.ColumnName);
    }

    [Fact]
    public async Task Submitted_hours_that_are_not_yet_approved_are_flagged_so_they_are_not_forgotten()
    {
        var (employee, project) = await SeedWorkerWithShiftsAsync(
        [
            (1, 8, TimeEntryStatus.Approved),
            (2, 6, TimeEntryStatus.Submitted),
        ]);
        var month = await NewMonthAsync();
        await RowForAsync(month, employee, project);

        var checks = await AsSuperAdmin(month, s => s.Send(new GetLedgerChecksQuery { LedgerId = month.Ledger.Id }));

        var issue = Assert.Single(checks, c => c.Kind == LedgerCheckKinds.UnreviewedHours);
        Assert.Equal(6m, issue.Amount);
    }

    [Fact]
    public async Task The_summary_counts_hours_that_come_from_the_timesheet()
    {
        var (employee, project) = await SeedWorkerWithShiftsAsync(
            [(1, 10, TimeEntryStatus.Approved)], hourlyRate: 20m);
        var month = await NewMonthAsync(hoursFromApp: true);
        var (_, rowId) = await RowForAsync(month, employee, project);
        await SetAsync(month, rowId, LedgerTemplates.Keys.ClientRate, "33");

        var summary = await AsSuperAdmin(month, s => s.Send(new GetLedgerSummaryQuery(month.Ledger.Id)));

        // 10 h x 33 billed - 10 h x 20 paid: a row with nothing typed except a price.
        Assert.Equal(130m, summary.Boxes.Single(b => b.Label == "Ukupan prihod (marža)").Value);
    }

    // ---- the customer's rules --------------------------------------------

    [Fact]
    public async Task Leave_is_added_and_an_advance_is_taken_off_and_the_names_say_so()
    {
        var month = await NewMonthAsync();
        var (section, row) = await SeedTypicalWorkerAsync(month);

        Assert.Equal("Godišnji odmor (+)", month.Ledger.Columns.Single(c => c.SystemKey == LedgerTemplates.Keys.Holiday).Name);
        Assert.Equal("Akontacija (−)", month.Ledger.Columns.Single(c => c.SystemKey == LedgerTemplates.Keys.Advance).Name);

        // 160 h at 20 is 3200; leave 300 is added, an advance of 500 already paid is taken off.
        await SetAsync(month, row, LedgerTemplates.Keys.Holiday, "300");
        await SetAsync(month, row, LedgerTemplates.Keys.Advance, "500");

        var read = await ReadRowAsync(month, section, row);

        Assert.Equal(3000m, Value(month, read, LedgerTemplates.Keys.Pay));
    }

    [Fact]
    public async Task Hours_are_typed_from_the_signed_timesheets_and_the_apps_hours_do_not_fill_them()
    {
        var (employee, project) = await SeedWorkerWithShiftsAsync([(1, 8, TimeEntryStatus.Approved)]);
        var month = await NewMonthAsync();

        var (section, rowId) = await RowForAsync(month, employee, project);
        var row = await ReadRowAsync(month, section, rowId);

        Assert.Equal(0m, Value(month, row, LedgerTemplates.Keys.Hours));
        Assert.All(
            Enumerable.Range(1, LedgerTemplates.WeekColumns),
            week => Assert.False(row.Cells.Any(c => c.ColumnId == month.Column(LedgerTemplates.Keys.Week(week)) && c.IsComputed)));
    }

    [Fact]
    public async Task Typed_hours_far_from_the_apps_are_flagged_as_a_prompt_and_close_ones_are_not()
    {
        var (employee, project) = await SeedWorkerWithShiftsAsync(
        [
            (1, 8, TimeEntryStatus.Approved),
            (2, 8, TimeEntryStatus.Approved),
        ]);
        var month = await NewMonthAsync();
        var (_, rowId) = await RowForAsync(month, employee, project);

        Task<IReadOnlyList<LedgerCheckDto>> Checks() =>
            AsSuperAdmin(month, s => s.Send(new GetLedgerChecksQuery { LedgerId = month.Ledger.Id }));

        // The app has 16 h approved. 40 h typed is far from it.
        await SetAsync(month, rowId, LedgerTemplates.Keys.Week(1), "40");
        var flagged = Assert.Single(await Checks(), c => c.Kind == LedgerCheckKinds.HoursDifferFromApp);
        Assert.Equal(40m, flagged.Amount);
        Assert.Equal(16m, flagged.ReferenceAmount);

        // 18 h is within the tolerance of a few hours: not worth mentioning.
        await SetAsync(month, rowId, LedgerTemplates.Keys.Week(1), "18");
        Assert.DoesNotContain(await Checks(), c => c.Kind == LedgerCheckKinds.HoursDifferFromApp);
    }

    [Fact]
    public async Task Typed_hours_for_someone_the_app_knows_nothing_about_are_not_a_disagreement()
    {
        var month = await NewMonthAsync();
        var section = await AddSectionAsync(month);
        var rowId = await AddRowAsync(month, section, "Kooperanti");

        await SetAsync(month, rowId, LedgerTemplates.Keys.Week(1), "80");

        var checks = await AsSuperAdmin(month, s => s.Send(new GetLedgerChecksQuery { LedgerId = month.Ledger.Id }));

        Assert.DoesNotContain(checks, c => c.Kind == LedgerCheckKinds.HoursDifferFromApp);
    }

    [Fact]
    public async Task A_worker_with_hours_and_no_contributions_entered_is_flagged_but_zero_is_an_answer()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var month = await NewMonthAsync();
        var section = await AddSectionAsync(month);
        var rowId = await AddRowAsync(month, section, "Radnik", employee.Id);
        var loose = await AddRowAsync(month, section, "Kooperanti");

        await SetAsync(month, rowId, LedgerTemplates.Keys.Week(1), "40");
        await SetAsync(month, loose, LedgerTemplates.Keys.Week(1), "40");

        Task<IReadOnlyList<LedgerCheckDto>> Checks() =>
            AsSuperAdmin(month, s => s.Send(new GetLedgerChecksQuery { LedgerId = month.Ledger.Id }));

        // Only the person on the payroll is asked; the row with no name is not.
        var flagged = Assert.Single(await Checks(), c => c.Kind == LedgerCheckKinds.MissingContributions);
        Assert.Equal(rowId, flagged.RowId);

        await SetAsync(month, rowId, LedgerTemplates.Keys.Contributions, "0");

        Assert.DoesNotContain(await Checks(), c => c.Kind == LedgerCheckKinds.MissingContributions);
    }

    [Fact]
    public async Task The_template_that_takes_hours_from_the_app_still_fills_the_weeks()
    {
        var month = await NewMonthAsync(hoursFromApp: true);

        var formulaColumns = month.Ledger.Columns.Where(c => c.IsFormula).Select(c => c.SystemKey).ToList();

        Assert.All(
            Enumerable.Range(1, LedgerTemplates.WeekColumns).Select(LedgerTemplates.Keys.Week),
            key => Assert.Contains(key, formulaColumns));
    }

    // ---- fuel, rented cars and housing -----------------------------------

    private async Task<Vehicle> SeedVehicleForAsync(Employee employee, decimal fuelApproved, decimal fuelPending = 0m)
    {
        return await InScope(async scope =>
        {
            var vehicle = await TestData.SeedVehicleAsync(scope);
            vehicle.AssignedEmployeeId = employee.Id;

            void Fuel(decimal amount, VehicleExpenseStatus status) =>
                scope.Db.VehicleExpenses.Add(new VehicleExpense
                {
                    VehicleId = vehicle.Id,
                    Kind = VehicleExpenseKind.Fuel,
                    Amount = amount,
                    Litres = 20m,
                    OccurredOn = new DateOnly(2026, 9, 10),
                    Status = status,
                });

            Fuel(fuelApproved, VehicleExpenseStatus.Approved);

            if (fuelPending > 0)
            {
                Fuel(fuelPending, VehicleExpenseStatus.Pending);
            }

            await scope.Db.SaveChangesAsync();

            return vehicle;
        });
    }

    [Fact]
    public async Task Fuel_is_the_approved_fuel_of_the_vehicles_assigned_to_the_person()
    {
        var (employee, project) = await SeedWorkerWithShiftsAsync([(1, 8, TimeEntryStatus.Approved)]);
        await SeedVehicleForAsync(employee, fuelApproved: 120m, fuelPending: 999m);
        var month = await NewMonthAsync();

        var (section, rowId) = await RowForAsync(month, employee, project);
        var row = await ReadRowAsync(month, section, rowId);

        Assert.Equal(120m, Value(month, row, LedgerTemplates.Keys.Fuel));
        Assert.True(row.Cells.Single(c => c.ColumnId == month.Column(LedgerTemplates.Keys.Fuel)).IsComputed);
    }

    [Fact]
    public async Task A_rented_car_costs_its_monthly_amount_for_the_days_it_applied()
    {
        var (employee, project) = await SeedWorkerWithShiftsAsync([(1, 8, TimeEntryStatus.Approved)]);
        var vehicle = await SeedVehicleForAsync(employee, fuelApproved: 0m);

        await InScope(async scope =>
        {
            // Whole of September, then a second rate that begins on the 16th and so
            // applies to only the last fifteen days of the thirty.
            scope.Db.VehicleRentalRates.Add(new VehicleRentalRate
            {
                VehicleId = vehicle.Id,
                MonthlyAmount = 300m,
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 9, 15),
            });
            scope.Db.VehicleRentalRates.Add(new VehicleRentalRate
            {
                VehicleId = vehicle.Id,
                MonthlyAmount = 600m,
                StartDate = new DateOnly(2026, 9, 16),
            });
            await scope.Db.SaveChangesAsync();
        });

        var month = await NewMonthAsync();
        var (section, rowId) = await RowForAsync(month, employee, project);
        var row = await ReadRowAsync(month, section, rowId);

        // 15 days of 300 + 15 days of 600, out of 30.
        Assert.Equal(450m, Value(month, row, LedgerTemplates.Keys.Rent));
    }

    [Fact]
    public async Task A_persons_fuel_is_counted_once_on_their_first_row_not_on_every_section()
    {
        var (employee, project) = await SeedWorkerWithShiftsAsync([(1, 8, TimeEntryStatus.Approved)]);
        var other = await InScope(scope => TestData.SeedProjectAsync(scope, $"Drugo {Unique()}"));
        await SeedVehicleForAsync(employee, fuelApproved: 200m);
        var month = await NewMonthAsync();

        var (firstSection, firstRow) = await RowForAsync(month, employee, project);
        var (secondSection, secondRow) = await RowForAsync(month, employee, other);

        var first = await ReadRowAsync(month, firstSection, firstRow);
        var second = await ReadRowAsync(month, secondSection, secondRow);

        Assert.Equal(200m, Value(month, first, LedgerTemplates.Keys.Fuel));
        Assert.Equal(0m, Value(month, second, LedgerTemplates.Keys.Fuel));

        var summary = await AsSuperAdmin(month, s => s.Send(new GetLedgerSummaryQuery(month.Ledger.Id)));
        Assert.Equal(200m, summary.Boxes.Single(b => b.Label == "Gorivo").Value);
    }

    [Fact]
    public async Task Housing_is_the_persons_share_of_the_rent_on_the_project_the_stay_is_for()
    {
        var (employee, project) = await SeedWorkerWithShiftsAsync([(1, 8, TimeEntryStatus.Approved)]);

        await InScope(async scope =>
        {
            var flat = await TestData.SeedAccommodationAsync(scope);
            var colleague = await TestData.SeedEmployeeAsync(scope);

            scope.Db.AccommodationRates.Add(new AccommodationRate
            {
                AccommodationId = flat.Id,
                Amount = 600m,
                Kind = AccommodationChargeKind.Monthly,
                StartDate = new DateOnly(2026, 1, 1),
            });

            // Two people share the flat all month: 300 each.
            foreach (var person in new[] { employee, colleague })
            {
                scope.Db.AccommodationStays.Add(new AccommodationStay
                {
                    AccommodationId = flat.Id,
                    EmployeeId = person.Id,
                    ProjectId = project.Id,
                    StartDate = new DateOnly(2026, 1, 1),
                });
            }

            await scope.Db.SaveChangesAsync();
        });

        var month = await NewMonthAsync();
        var (section, rowId) = await RowForAsync(month, employee, project);
        var row = await ReadRowAsync(month, section, rowId);

        Assert.Equal(300m, Value(month, row, LedgerTemplates.Keys.Housing));
    }

    [Fact]
    public async Task Typing_a_different_fuel_figure_is_flagged_but_typing_where_the_system_knows_nothing_is_not()
    {
        var (withFuel, projectA) = await SeedWorkerWithShiftsAsync([(1, 8, TimeEntryStatus.Approved)]);
        var (withoutFuel, projectB) = await SeedWorkerWithShiftsAsync([(1, 8, TimeEntryStatus.Approved)]);
        await SeedVehicleForAsync(withFuel, fuelApproved: 100m);
        var month = await NewMonthAsync();

        var (_, differs) = await RowForAsync(month, withFuel, projectA);
        var (_, nothingKnown) = await RowForAsync(month, withoutFuel, projectB);

        await SetAsync(month, differs, LedgerTemplates.Keys.Fuel, "150");
        await SetAsync(month, nothingKnown, LedgerTemplates.Keys.Fuel, "80");

        var checks = await AsSuperAdmin(month, s => s.Send(new GetLedgerChecksQuery { LedgerId = month.Ledger.Id }));

        var flagged = Assert.Single(checks, c => c.Kind == LedgerCheckKinds.ManualOverride);
        Assert.Equal(differs, flagged.RowId);
    }

    // ---- the next month --------------------------------------------------

    [Fact]
    public async Task A_copied_month_keeps_rates_but_looks_at_its_own_weeks_and_starts_hours_empty()
    {
        var source = await NewMonthAsync(year: 2026, month: 9);
        var (sourceSection, sourceRow) = await SeedTypicalWorkerAsync(source);
        var sourceName = source.Ledger.Columns.Single(c => c.SystemKey == LedgerTemplates.Keys.Week(1)).Name;

        var copy = await AsSuperAdmin(source, s => s.Send(new CreateLedgerCommand
        {
            Name = $"Oktobar {Unique()}",
            Year = 2026,
            Month = 10,
            CopyFromLedgerId = source.Ledger.Id,
        }));
        var next = new Month { Ledger = copy, SuperAdmin = source.SuperAdmin };

        Assert.Equal("KW36", sourceName);
        Assert.Equal(LedgerTemplates.WeekName(1, 2026, 10), copy.Columns.Single(c => c.SystemKey == "week1").Name);
        Assert.NotEqual(sourceName, copy.Columns.Single(c => c.SystemKey == "week1").Name);

        var detail = await AsSuperAdmin(next, s => s.Send(new GetLedgerByIdQuery(copy.Id)));
        var newSection = detail.Sections.Single();
        var loaded = await AsSuperAdmin(next, s => s.Send(new GetLedgerSectionRowsQuery(copy.Id, newSection.Id)));
        var newRow = loaded.Rows.Single();

        // Rates and the fixed per-person amounts carried over; this month's hours did not.
        Assert.Equal(20m, Value(next, newRow, LedgerTemplates.Keys.WorkerRate));
        Assert.Equal(33m, Value(next, newRow, LedgerTemplates.Keys.ClientRate));
        // Contributions come off the payslip and are not always the same: not carried.
        Assert.Equal(0m, Value(next, newRow, LedgerTemplates.Keys.Contributions));
        Assert.Equal(100m, Value(next, newRow, LedgerTemplates.Keys.Bonus));
        Assert.Equal(0m, Value(next, newRow, LedgerTemplates.Keys.Hours));
        Assert.Equal(0m, Value(next, newRow, LedgerTemplates.Keys.Housing));
        Assert.NotEqual(sourceSection, newSection.Id);
        Assert.NotEqual(sourceRow, newRow.Id);
    }

    // ---- which weeks a month has -----------------------------------------

    [Theory]
    [InlineData(2026, 9, 5, 36, 40)]    // 1 Sept is a Tuesday: KW36-KW40
    [InlineData(2026, 8, 6, 31, 36)]    // 1 Aug is a Saturday, 31 Aug a Monday: six weeks
    [InlineData(2027, 2, 4, 5, 8)]      // 1 Feb is a Monday, 28 days: exactly four weeks
    public void The_weeks_of_a_month_are_the_calendar_weeks_it_touches(int year, int month, int count, int first, int last)
    {
        var weeks = LedgerTemplates.MonthWeeks(year, month);

        Assert.Equal(count, weeks.Count);
        Assert.Equal(first, weeks[0].IsoWeek);
        Assert.Equal(last, weeks[^1].IsoWeek);
        Assert.Equal(new DateOnly(year, month, 1), weeks[0].From);
        Assert.Equal(new DateOnly(year, month, DateTime.DaysInMonth(year, month)), weeks[^1].To);

        // No gaps and no overlaps.
        for (var i = 1; i < weeks.Count; i++)
        {
            Assert.Equal(weeks[i - 1].To.AddDays(1), weeks[i].From);
        }
    }

    // ---- export ----------------------------------------------------------

    [Fact]
    public async Task The_month_exports_as_a_spreadsheet_named_for_its_period()
    {
        var month = await NewMonthAsync(year: 2026, month: 9);
        await SeedTypicalWorkerAsync(month);

        var file = await AsSuperAdmin(month, s => s.Send(new ExportLedgerQuery(month.Ledger.Id)));

        Assert.Equal("evidencija-2026-09.xlsx", file.FileName);
        Assert.Contains("spreadsheetml", file.ContentType);
        // An .xlsx is a zip archive.
        Assert.True(file.Content.Length > 1000);
        Assert.Equal((byte)'P', file.Content[0]);
        Assert.Equal((byte)'K', file.Content[1]);
    }
}
