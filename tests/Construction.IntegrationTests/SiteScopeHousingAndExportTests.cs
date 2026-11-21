using System.IO.Compression;
using System.Text;
using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Accommodations.Queries.GetAccommodationById;
using Construction.Application.Features.Accommodations.Queries.GetAccommodations;
using Construction.Application.Features.Accommodations.Stays;
using Construction.Application.Features.Exports.Queries;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// A foreman's reach into housing and spreadsheets is the same as on the lists: their own sites, and no rent or
/// deposit without the finance grant. Both were open before (the list showed every flat with its rent, the export
/// every project and every worker's phone).
/// </summary>
[Collection(DatabaseCollection.Name)]
public class SiteScopeHousingAndExportTests : IntegrationTestBase
{
    public SiteScopeHousingAndExportTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private record Setup(User Foreman, Employee ForemanEmployee, Project Mine, Project Other, Employee Worker);

    private Task<Setup> SeedAsync() =>
        InScope(async scope =>
        {
            var employee = await TestData.SeedEmployeeAsync(scope);
            var user = await TestData.SeedUserAsync(scope, UserRole.Foreman, employee.Id);
            var mine = await TestData.SeedProjectAsync(scope);
            var other = await TestData.SeedProjectAsync(scope);
            var worker = await TestData.SeedEmployeeAsync(scope);

            scope.Db.EmployeeProjects.Add(new EmployeeProject
            {
                EmployeeId = employee.Id,
                ProjectId = mine.Id,
                StartDate = new DateOnly(2030, 1, 1),
            });
            await scope.Db.SaveChangesAsync();

            return new Setup(user, employee, mine, other, worker);
        });

    private Task<T> AsForeman<T>(Setup s, Func<TestScope, Task<T>> action) =>
        InScope(scope =>
        {
            scope.CurrentUser.SignInAs(s.Foreman.Id, UserRole.Foreman, s.ForemanEmployee.Id, s.Foreman.Email);
            return action(scope);
        });

    private Task<Accommodation> HousingAsync(Setup s, Guid? projectId) =>
        InScope(async scope =>
        {
            var housing = await TestData.SeedAccommodationAsync(scope);
            housing.DepositAmount = 999m;
            await TestData.SeedAccommodationRateAsync(scope, housing.Id);

            if (projectId is { } id)
            {
                scope.Db.AccommodationStays.Add(new AccommodationStay
                {
                    AccommodationId = housing.Id,
                    EmployeeId = (await TestData.SeedEmployeeAsync(scope)).Id,
                    ProjectId = id,
                    StartDate = new DateOnly(2026, 1, 1),
                });
            }

            await scope.Db.SaveChangesAsync();
            return housing;
        });

    [Fact]
    public async Task A_foreman_sees_housing_of_their_own_site_without_rent_or_deposit_and_not_the_rest()
    {
        var s = await SeedAsync();
        var own = await HousingAsync(s, s.Mine.Id);
        var foreign = await HousingAsync(s, s.Other.Id);

        var list = await AsForeman(s, scope => scope.Send(new GetAccommodationsQuery { PageSize = 100 }));

        var seen = Assert.Single(list.Items, a => a.Id == own.Id);
        Assert.DoesNotContain(list.Items, a => a.Id == foreign.Id);
        Assert.Null(seen.CurrentMonthlyAmount);
        Assert.Null(seen.DepositAmount);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsForeman(s, scope => scope.Send(new GetAccommodationByIdQuery(foreign.Id))));

        // The office still sees the amounts.
        var asAdmin = await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(Guid.NewGuid(), UserRole.SuperAdmin, null, "tests@example.com");
            return scope.Send(new GetAccommodationByIdQuery(own.Id));
        });
        Assert.NotNull(asAdmin.CurrentMonthlyAmount);
        Assert.Equal(999m, asAdmin.DepositAmount);
    }

    [Fact]
    public async Task A_foreman_cannot_house_somebody_on_another_site()
    {
        var s = await SeedAsync();
        var housing = await HousingAsync(s, null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsForeman(s, scope => scope.Send(new AddAccommodationStayCommand
            {
                AccommodationId = housing.Id,
                EmployeeId = s.Worker.Id,
                ProjectId = s.Other.Id,
            })));
    }

    [Fact]
    public async Task A_foreman_gets_only_their_own_site_in_the_project_export()
    {
        var s = await SeedAsync();

        var file = await AsForeman(s, scope => scope.Send(new ExportProjectsQuery()));
        var text = SpreadsheetText(file.Content);

        Assert.Contains(s.Mine.Name, text);
        Assert.DoesNotContain(s.Other.Name, text);

        var all = await InScope(scope =>
        {
            scope.CurrentUser.SignInAs(Guid.NewGuid(), UserRole.SuperAdmin, null, "tests@example.com");
            return scope.Send(new ExportProjectsQuery());
        });
        Assert.Contains(s.Other.Name, SpreadsheetText(all.Content));
    }

    [Fact]
    public async Task A_foreman_cannot_edit_another_site_or_book_material_to_it()
    {
        var s = await SeedAsync();
        var material = await InScope(scope => TestData.SeedMaterialAsync(scope, 10));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsForeman(s, scope => scope.Send(new Construction.Application.Features.Projects.Commands.UpdateProject.UpdateProjectCommand { Id = s.Other.Id, Name = "x" })));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsForeman(s, scope => scope.Send(new Construction.Application.Features.Costs.Commands.RecordMaterialMovement.RecordMaterialMovementCommand
            {
                MaterialId = material.Id,
                ProjectId = s.Other.Id,
                Quantity = 1,
            })));
    }

    private static string SpreadsheetText(byte[] xlsx)
    {
        using var zip = new ZipArchive(new MemoryStream(xlsx));
        var entry = zip.GetEntry("xl/sharedStrings.xml")!;
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
