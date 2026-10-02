using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Branches;
using Construction.Application.Features.Costs.Queries.GetCompanyCosts;
using Construction.Application.Features.Finance;
using Construction.Application.Features.Projects.Commands.CreateProject;
using Construction.Application.Features.Projects.Commands.UpdateProject;
using Construction.Application.Features.Projects.Queries.GetProjects;
using Construction.Application.Features.Vehicles.Queries.GetVehicles;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// Business units (poslovne jedinice): who may change them, how projects move between them,
/// and that every figure narrowed to a unit adds up to the figure for all of them.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class BranchTests : IntegrationTestBase
{
    public BranchTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    // Years before 2026, in a range no other test class uses (they take 1600, 1700, 1900 and 2000 on):
    // rentals and rents seeded for 2026 never end, so a later year is never empty.
    private static int nextYear = 1800;

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}"[..28];

    private static async Task SignInAsync(TestScope scope, UserRole role)
    {
        var user = await TestData.SeedUserAsync(scope, role);
        user.FinanceAccess = FinanceAccess.Full;
        await scope.Db.SaveChangesAsync();
        scope.CurrentUser.SignInAs(user.Id, role, null, user.Email);
    }

    private async Task<BranchDto> CreateBranchAsync(string? name = null)
    {
        return await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            return await scope.Send(new CreateBranchCommand { Name = name ?? UniqueName("Unit"), Color = "#3457D5" });
        });
    }

    [Fact]
    public async Task Management_creates_a_unit_and_a_foreman_may_not()
    {
        var branch = await CreateBranchAsync();

        Assert.True(branch.IsActive);
        Assert.Equal("#3457D5", branch.Color);

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.Foreman);

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => scope.Send(new CreateBranchCommand { Name = UniqueName("Nope"), Color = "#3457D5" }));
        });
    }

    [Fact]
    public async Task Two_units_cannot_share_a_name()
    {
        var name = UniqueName("Same");
        await CreateBranchAsync(name);

        await Assert.ThrowsAsync<ConflictException>(() => CreateBranchAsync(name));
    }

    [Fact]
    public async Task A_unit_with_projects_cannot_be_deleted_but_an_empty_one_can()
    {
        var used = await CreateBranchAsync();
        var empty = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);
            var project = await TestData.SeedProjectAsync(scope);
            project.BranchId = used.Id;
            await scope.Db.SaveChangesAsync();

            await Assert.ThrowsAsync<ConflictException>(() => scope.Send(new DeleteBranchCommand(used.Id)));

            await scope.Send(new DeleteBranchCommand(empty.Id));
            Assert.DoesNotContain(scope.Db.Branches, b => b.Id == empty.Id);
        });
    }

    [Fact]
    public async Task A_project_naming_a_unit_that_does_not_exist_is_refused()
    {
        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            await Assert.ThrowsAsync<NotFoundException>(() => scope.Send(new CreateProjectCommand
            {
                Name = UniqueName("Site"),
                BranchId = Guid.NewGuid(),
            }));
        });
    }

    [Fact]
    public async Task A_sub_project_always_takes_its_parents_unit_and_follows_it_when_it_changes()
    {
        var first = await CreateBranchAsync();
        var second = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            var main = await scope.Send(new CreateProjectCommand { Name = UniqueName("Main"), BranchId = first.Id });

            // Whatever a sub-project is sent with, it belongs to its parent's unit.
            var sub = await scope.Send(new CreateProjectCommand
            {
                Name = UniqueName("Sub"),
                ParentProjectId = main.Id,
                BranchId = second.Id,
            });
            Assert.Equal(first.Id, sub.BranchId);

            await scope.Send(new UpdateProjectCommand
            {
                Id = main.Id,
                Name = main.Name,
                Status = ProjectStatus.Active,
                BranchId = second.Id,
            });

            var moved = scope.Db.Projects.Single(p => p.Id == sub.Id);
            Assert.Equal(second.Id, moved.BranchId);
        });
    }

    [Fact]
    public async Task Setting_the_projects_of_a_unit_moves_the_chosen_ones_with_their_sub_projects_and_releases_the_rest()
    {
        var branch = await CreateBranchAsync();
        var other = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            var kept = await TestData.SeedProjectAsync(scope);
            var dropped = await TestData.SeedProjectAsync(scope);
            var chosen = await TestData.SeedProjectAsync(scope);
            var chosenSub = new Project { Name = UniqueName("Sub"), ParentProjectId = chosen.Id };
            scope.Db.Projects.Add(chosenSub);

            kept.BranchId = branch.Id;
            dropped.BranchId = branch.Id;
            chosen.BranchId = other.Id;
            await scope.Db.SaveChangesAsync();

            var result = await scope.Send(new SetBranchProjectsCommand
            {
                Id = branch.Id,
                ProjectIds = [kept.Id, chosen.Id],
            });

            Assert.Equal(3, result.ProjectCount);

            var ids = new[] { kept.Id, dropped.Id, chosen.Id, chosenSub.Id };
            var byId = scope.Db.Projects.Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id, p => p.BranchId);

            Assert.Equal(branch.Id, byId[kept.Id]);
            Assert.Null(byId[dropped.Id]);
            Assert.Equal(branch.Id, byId[chosen.Id]);
            Assert.Equal(branch.Id, byId[chosenSub.Id]);
        });
    }

    [Fact]
    public async Task The_project_list_narrows_to_one_unit()
    {
        var branch = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            var inside = await TestData.SeedProjectAsync(scope);
            await TestData.SeedProjectAsync(scope);
            inside.BranchId = branch.Id;
            await scope.Db.SaveChangesAsync();

            var page = await scope.Send(new GetProjectsQuery { BranchId = branch.Id, PageSize = 100 });

            Assert.Equal([inside.Id], page.Items.Select(p => p.Id).ToArray());
        });
    }

    [Fact]
    public async Task A_vehicle_with_its_own_unit_keeps_it_whatever_project_it_is_on()
    {
        var own = await CreateBranchAsync();
        var projectUnit = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            var project = await TestData.SeedProjectAsync(scope);
            project.BranchId = projectUnit.Id;

            var ownVehicle = await TestData.SeedVehicleAsync(scope);
            ownVehicle.BranchId = own.Id;
            ownVehicle.AssignedProjectId = project.Id;

            var followingVehicle = await TestData.SeedVehicleAsync(scope);
            followingVehicle.AssignedProjectId = project.Id;
            await scope.Db.SaveChangesAsync();

            var inOwn = await scope.Send(new GetVehiclesQuery { BranchId = own.Id, PageSize = 100 });
            var inProject = await scope.Send(new GetVehiclesQuery { BranchId = projectUnit.Id, PageSize = 100 });

            Assert.Equal([ownVehicle.Id], inOwn.Items.Select(v => v.Id).ToArray());
            Assert.Equal([followingVehicle.Id], inProject.Items.Select(v => v.Id).ToArray());
        });
    }

    [Fact]
    public async Task Company_costs_per_unit_add_up_to_the_costs_of_all_of_them()
    {
        var year = Interlocked.Increment(ref nextYear);
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);
        var day = new DateOnly(year, 5, 20);

        var first = await CreateBranchAsync();
        var second = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            var secondProject = await TestData.SeedProjectAsync(scope);
            secondProject.BranchId = second.Id;
            await scope.Db.SaveChangesAsync();

            // Booked to a unit directly, booked to a project of a unit, and booked to neither.
            scope.Db.GeneralExpenses.AddRange(
                new GeneralExpense { Category = GeneralExpenseCategory.Other, Amount = 100m, OccurredOn = day, BranchId = first.Id },
                new GeneralExpense { Category = GeneralExpenseCategory.Other, Amount = 40m, OccurredOn = day, ProjectId = secondProject.Id },
                new GeneralExpense { Category = GeneralExpenseCategory.Other, Amount = 7m, OccurredOn = day });
            await scope.Db.SaveChangesAsync();

            var all = await scope.Send(new GetCompanyCostsQuery { From = from, To = to });
            var inFirst = await scope.Send(new GetCompanyCostsQuery { From = from, To = to, BranchId = first.Id });
            var inSecond = await scope.Send(new GetCompanyCostsQuery { From = from, To = to, BranchId = second.Id });

            Assert.Equal(147m, all.Total);
            Assert.Equal(100m, inFirst.GeneralExpenses);
            Assert.Equal(40m, inSecond.GeneralExpenses);

            // What names no unit is in no unit's figure, so the units never add up to more than the whole.
            Assert.Equal(all.Total - 7m, inFirst.Total + inSecond.Total);
        });
    }

    [Fact]
    public async Task Revenue_of_a_unit_is_only_its_own()
    {
        var year = Interlocked.Increment(ref nextYear);
        var day = new DateOnly(year, 6, 1);

        var first = await CreateBranchAsync();
        var second = await CreateBranchAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            scope.Db.CompanyRevenues.AddRange(
                new CompanyRevenue { Amount = 200m, OccurredOn = day, Source = CompanyRevenueSource.Other, BranchId = first.Id },
                new CompanyRevenue { Amount = 30m, OccurredOn = day, Source = CompanyRevenueSource.Other, BranchId = second.Id });
            await scope.Db.SaveChangesAsync();

            var page = await scope.Send(new GetCompanyRevenuesQuery
            {
                BranchId = first.Id,
                From = day,
                To = day,
                PageSize = 50,
            });

            Assert.Equal([200m], page.Items.Select(r => r.Amount).ToArray());
        });
    }

    private static async Task SignInWithTaxGrantAsync(TestScope scope, UserRole role, bool granted)
    {
        var user = await TestData.SeedUserAsync(scope, role);
        user.CanViewCustomerTaxDetails = granted;
        await scope.Db.SaveChangesAsync();
        scope.CurrentUser.SignInAs(user.Id, role, null, user.Email);
    }

    private static CreateBranchCommand WithDetails(string name, BranchKind kind = BranchKind.LegalEntity) => new()
    {
        Name = name,
        Color = "#0F8A5F",
        Kind = kind,
        LegalName = "Plan Concept d.o.o.",
        Address = "Zmaja od Bosne 1",
        City = "Sarajevo",
        CountryCode = "ba",
        TaxId = "4200000000001",
        RegistrationNumber = "65-01-0000-00",
        VatNumber = "200000000001",
        OwnerName = "Ime Prezime",
        ContactPerson = "Kontakt Osoba",
        Phone = "+387 33 000 000",
        Email = "ured@example.com",
    };

    [Fact]
    public async Task A_super_admin_stores_a_units_details_including_its_tax_numbers()
    {
        var name = UniqueName("Details");

        var created = await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            return await scope.Send(WithDetails(name));
        });

        Assert.Equal("BA", created.CountryCode);
        Assert.Equal("Sarajevo", created.City);
        Assert.Equal("4200000000001", created.TaxId);
        Assert.Equal("200000000001", created.VatNumber);
        Assert.Equal("Kontakt Osoba", created.ContactPerson);
    }

    [Fact]
    public async Task An_admin_sees_the_details_but_not_the_tax_numbers_until_granted()
    {
        var name = UniqueName("Tax");

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);
            await scope.Send(WithDetails(name));
        });

        await InScope(async scope =>
        {
            await SignInWithTaxGrantAsync(scope, UserRole.Admin, granted: false);
            var unit = (await scope.Send(new GetBranchesQuery())).Single(b => b.Name == name);

            Assert.Equal("Sarajevo", unit.City);
            Assert.Equal("Ime Prezime", unit.OwnerName);
            Assert.Null(unit.TaxId);
            Assert.Null(unit.RegistrationNumber);
            Assert.Null(unit.VatNumber);
        });

        await InScope(async scope =>
        {
            await SignInWithTaxGrantAsync(scope, UserRole.Admin, granted: true);
            var unit = (await scope.Send(new GetBranchesQuery())).Single(b => b.Name == name);

            Assert.Equal("4200000000001", unit.TaxId);
        });
    }

    [Fact]
    public async Task Anyone_below_management_reads_a_unit_without_its_details()
    {
        var name = UniqueName("Basics");

        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);
            await scope.Send(WithDetails(name));
        });

        await InScope(async scope =>
        {
            // Even with the tax grant: a foreman reads a unit only to filter by it.
            await SignInWithTaxGrantAsync(scope, UserRole.Foreman, granted: true);
            var unit = (await scope.Send(new GetBranchesQuery())).Single(b => b.Name == name);

            Assert.Equal(name, unit.Name);
            Assert.Null(unit.Address);
            Assert.Null(unit.OwnerName);
            Assert.Null(unit.Phone);
            Assert.Null(unit.TaxId);
        });
    }

    [Fact]
    public async Task An_admin_edits_the_details_and_leaves_the_tax_numbers_as_they_were()
    {
        var name = UniqueName("Keep");

        var created = await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            return await scope.Send(WithDetails(name));
        });

        await InScope(async scope =>
        {
            await SignInWithTaxGrantAsync(scope, UserRole.Admin, granted: false);

            await scope.Send(new UpdateBranchCommand
            {
                Id = created.Id,
                Name = name,
                Color = "#0F8A5F",
                City = "Mostar",
                TaxId = "9999999999999",
            });
        });

        await InScope(async scope =>
        {
            var stored = scope.Db.Branches.Single(b => b.Id == created.Id);

            Assert.Equal("Mostar", stored.City);
            Assert.Equal("4200000000001", stored.TaxId);
        });
    }

    [Fact]
    public async Task A_representative_office_may_leave_its_numbers_blank_and_a_bad_country_or_email_is_refused()
    {
        await InScope(async scope =>
        {
            await SignInAsync(scope, UserRole.SuperAdmin);

            var office = await scope.Send(new CreateBranchCommand
            {
                Name = UniqueName("Office"),
                Color = "#7C3AED",
                Kind = BranchKind.RepresentativeOffice,
                City = "Berlin",
                CountryCode = "DE",
            });

            Assert.Equal(BranchKind.RepresentativeOffice, office.Kind);
            Assert.Null(office.TaxId);

            await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
                () => scope.Send(new CreateBranchCommand { Name = UniqueName("Bad"), Color = "#7C3AED", CountryCode = "DEU" }));
            await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
                () => scope.Send(new CreateBranchCommand { Name = UniqueName("Bad"), Color = "#7C3AED", Email = "not-an-email" }));
        });
    }
}
