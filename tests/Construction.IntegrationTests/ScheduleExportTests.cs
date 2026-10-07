using ClosedXML.Excel;
using Construction.Application.Features.Exports.Queries;
using Construction.Application.Features.Planning;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class ScheduleExportTests : IntegrationTestBase
{
    public ScheduleExportTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    // Monday 2031-09-01.
    private static readonly DateOnly Monday = new(2031, 9, 1);

    private static XLWorkbook Open(ExportFile file) => new(new MemoryStream(file.Content));

    private Task<ExportFile> ExportAsync(ExportScheduleQuery query) =>
        InScopeAs(UserRole.Admin, scope => scope.Send(query));

    [Fact]
    public async Task The_week_on_paper_has_a_row_per_person_a_column_per_day_and_the_site_in_each_cell()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope, firstName: "Paper", lastName: $"Worker{Guid.NewGuid():N}"[..14]));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope, name: $"Paper site {Guid.NewGuid():N}"[..20]));
        await InScopeAs(UserRole.Admin, scope => scope.Send(new SetEmployeeScheduleCommand { EmployeeId = employee.Id, ProjectId = site.Id, From = Monday, To = Monday.AddDays(2) }));

        var file = await ExportAsync(new ExportScheduleQuery { From = Monday, To = Monday.AddDays(6) });

        using var book = Open(file);
        var sheet = book.Worksheet(1);

        // Header: two fixed columns, then Monday to Sunday.
        Assert.Contains("Pon", sheet.Cell(1, 3).GetString());
        Assert.Contains("Ned", sheet.Cell(1, 9).GetString());

        var row = sheet.RowsUsed().Single(r => r.Cell(1).GetString() == employee.FullName);
        Assert.Equal(site.Name, row.Cell(3).GetString());
        Assert.Equal(site.Name, row.Cell(5).GetString());
        Assert.Equal(string.Empty, row.Cell(6).GetString());
        Assert.StartsWith("schedule-2031-09-01-2031-09-07", file.FileName);
    }

    [Fact]
    public async Task Leave_shows_instead_of_the_site_and_the_second_sheet_lists_who_is_on_each_site_each_day()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope, firstName: "Leave", lastName: $"Taker{Guid.NewGuid():N}"[..14]));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope, name: $"Crew site {Guid.NewGuid():N}"[..20]));
        await InScopeAs(UserRole.Admin, scope => scope.Send(new SetEmployeeScheduleCommand { EmployeeId = employee.Id, ProjectId = site.Id, From = Monday, To = Monday.AddDays(4) }));
        await InScope(async scope =>
        {
            scope.Db.Absences.Add(new Construction.Domain.Entities.Absence
            {
                EmployeeId = employee.Id,
                Type = AbsenceType.AnnualLeave,
                Status = AbsenceStatus.Approved,
                StartDate = Monday.AddDays(1),
                EndDate = Monday.AddDays(1),
            });
            await scope.Db.SaveChangesAsync();
        });

        var file = await ExportAsync(new ExportScheduleQuery { From = Monday, To = Monday.AddDays(4), Language = "en" });

        using var book = Open(file);
        var person = book.Worksheet(1).RowsUsed().Single(r => r.Cell(1).GetString() == employee.FullName);
        Assert.Equal("Leave", person.Cell(4).GetString());

        var bySite = book.Worksheet(2).RowsUsed().Where(r => r.Cell(1).GetString() == site.Name).ToList();
        // Tuesday is left out: the only person posted there is on leave, and the site asks for nobody.
        Assert.Equal(4, bySite.Count);
        Assert.DoesNotContain(bySite, r => r.Cell(2).GetDateTime() == Monday.AddDays(1).ToDateTime(TimeOnly.MinValue));
        Assert.All(bySite, r => Assert.Equal(employee.FullName, r.Cell(5).GetString()));
    }

    [Fact]
    public async Task A_period_too_long_for_a_page_is_refused()
    {
        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => ExportAsync(new ExportScheduleQuery { From = Monday, To = Monday.AddDays(ExportScheduleQueryValidator.MaxPaperDays) }));
    }
}
