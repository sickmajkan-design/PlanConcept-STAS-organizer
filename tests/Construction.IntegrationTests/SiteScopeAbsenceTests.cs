using System.IO.Compression;
using System.Text;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Absences.Commands.RequestAbsence;
using Construction.Application.Features.Absences.Queries.GetAbsences;
using Construction.Application.Features.Exports.Queries;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// Leave follows the site rule (a foreman sees and records leave only for the people on their own sites), and a day is
/// one thing: sick leave and annual leave never overlap, granted or waiting.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class SiteScopeAbsenceTests : IntegrationTestBase
{
    public SiteScopeAbsenceTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private record Setup(User Foreman, Employee ForemanEmployee, Employee Crew, Employee Stranger);

    private Task<Setup> SeedAsync() =>
        InScope(async scope =>
        {
            var boss = await TestData.SeedEmployeeAsync(scope);
            var user = await TestData.SeedUserAsync(scope, UserRole.Foreman, boss.Id);
            var mine = await TestData.SeedProjectAsync(scope);
            var other = await TestData.SeedProjectAsync(scope);
            var crew = await TestData.SeedEmployeeAsync(scope);
            var stranger = await TestData.SeedEmployeeAsync(scope);
            // The seed gives everybody the same name; the export test needs to tell them apart.
            crew.LastName = "Cvjetkovic" + Guid.NewGuid().ToString("N")[..6];
            stranger.LastName = "Strancic" + Guid.NewGuid().ToString("N")[..6];

            foreach (var (employee, project) in new[] { (boss, mine), (crew, mine), (stranger, other) })
            {
                scope.Db.EmployeeProjects.Add(new EmployeeProject
                {
                    EmployeeId = employee.Id,
                    ProjectId = project.Id,
                    StartDate = new DateOnly(2030, 1, 1),
                });
            }

            await scope.Db.SaveChangesAsync();
            return new Setup(user, boss, crew, stranger);
        });

    private Task<T> AsForeman<T>(Setup s, Func<TestScope, Task<T>> action) =>
        InScope(scope =>
        {
            scope.CurrentUser.SignInAs(s.Foreman.Id, UserRole.Foreman, s.ForemanEmployee.Id, s.Foreman.Email);
            return action(scope);
        });

    private Task<T> AsAdmin<T>(Func<TestScope, Task<T>> action) =>
        InScope(async scope =>
        {
            // A real account: the absence records who entered it.
            var admin = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin, null);
            scope.CurrentUser.SignInAs(admin.Id, UserRole.SuperAdmin, null, admin.Email);
            return await action(scope);
        });

    private static RequestAbsenceCommand Leave(Guid employeeId, AbsenceType type, int fromDay, int toDay, bool approve = false)
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(fromDay);
        return new RequestAbsenceCommand
        {
            EmployeeId = employeeId,
            Type = type,
            StartDate = start,
            EndDate = start.AddDays(toDay - fromDay),
            Approve = approve,
        };
    }

    [Fact]
    public async Task A_foreman_sees_and_exports_leave_of_their_own_crew_only()
    {
        var s = await SeedAsync();
        await AsAdmin(scope => scope.Send(Leave(s.Crew.Id, AbsenceType.AnnualLeave, 10, 12)));
        await AsAdmin(scope => scope.Send(Leave(s.Stranger.Id, AbsenceType.AnnualLeave, 10, 12)));

        var list = await AsForeman(s, scope => scope.Send(new GetAbsencesQuery { PageSize = 100 }));
        Assert.Contains(list.Items, a => a.EmployeeId == s.Crew.Id);
        Assert.DoesNotContain(list.Items, a => a.EmployeeId == s.Stranger.Id);

        var window = DateOnly.FromDateTime(DateTime.UtcNow);
        var file = await AsForeman(s, scope => scope.Send(new ExportAbsencesQuery { From = window, To = window.AddDays(60) }));
        var text = SpreadsheetText(file.Content);
        Assert.Contains(s.Crew.LastName, text);
        Assert.DoesNotContain(s.Stranger.LastName, text);
    }

    [Fact]
    public async Task A_foreman_records_leave_for_their_crew_but_not_for_another_site()
    {
        var s = await SeedAsync();

        await AsForeman(s, scope => scope.Send(Leave(s.Crew.Id, AbsenceType.SickLeave, 1, 2)));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsForeman(s, scope => scope.Send(Leave(s.Stranger.Id, AbsenceType.SickLeave, 1, 2))));
    }

    [Theory]
    [InlineData(AbsenceType.AnnualLeave, AbsenceType.SickLeave)]
    [InlineData(AbsenceType.SickLeave, AbsenceType.AnnualLeave)]
    public async Task Leave_and_sick_leave_cannot_overlap_once_one_is_granted(AbsenceType first, AbsenceType second)
    {
        var s = await SeedAsync();
        await AsAdmin(scope => scope.Send(Leave(s.Crew.Id, first, 5, 9, approve: true)));

        await Assert.ThrowsAsync<ConflictException>(() =>
            AsAdmin(scope => scope.Send(Leave(s.Crew.Id, second, 9, 11))));

        // The day after is free.
        await AsAdmin(scope => scope.Send(Leave(s.Crew.Id, second, 10, 11)));
    }

    private static string SpreadsheetText(byte[] xlsx)
    {
        using var zip = new ZipArchive(new MemoryStream(xlsx));
        using var reader = new StreamReader(zip.GetEntry("xl/sharedStrings.xml")!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
