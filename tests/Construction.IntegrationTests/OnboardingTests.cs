using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Employees.Commands.ImportEmployees;
using Construction.Application.Features.Invitations;
using Construction.Application.Features.Setup.Queries.GetSetupChecklist;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The first-day tools: bringing a workforce in from a spreadsheet, letting people
/// create their own accounts from a link, and the list of what is still missing.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class OnboardingTests : IntegrationTestBase
{
    public OnboardingTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

    private async Task<TestScope> AdminScopeAsync(UserRole role = UserRole.Admin)
    {
        var scope = Fixture.CreateScope();
        var admin = await TestData.SeedUserAsync(scope, role);
        scope.CurrentUser.SignInAs(admin.Id, role, null, admin.Email);
        return scope;
    }

    private static ImportEmployeeRow Row(int line, string first, string last, string? email = null) =>
        new() { Line = line, FirstName = first, LastName = last, Email = email };

    // ---- import ----------------------------------------------------------

    [Fact]
    public async Task A_dry_run_reports_what_would_happen_and_saves_nothing()
    {
        var last = $"Dry{Unique()}";

        using var scope = await AdminScopeAsync();
        var result = await scope.Send(new ImportEmployeesCommand
        {
            DryRun = true,
            Rows = [Row(2, "Ana", last)]
        });

        Assert.True(result.DryRun);
        Assert.Equal(1, result.Created);
        Assert.False(await scope.Db.Employees.AnyAsync(e => e.LastName == last));
    }

    [Fact]
    public async Task A_bad_row_is_reported_and_never_blocks_the_good_ones()
    {
        var last = $"Mix{Unique()}";

        using var scope = await AdminScopeAsync();
        var result = await scope.Send(new ImportEmployeesCommand
        {
            DryRun = false,
            Rows =
            [
                Row(2, "Ana", last),
                Row(3, "", "Bez imena"),
                Row(4, "Ivo", $"{last}b", email: "nije-mejl"),
            ]
        });

        Assert.Equal(1, result.Created);
        Assert.Equal(2, result.Errors);
        Assert.Equal(new[] { 3, 4 }, result.Rows.Where(r => r.Outcome == ImportRowOutcome.Error).Select(r => r.Line));
        Assert.True(await scope.Db.Employees.AnyAsync(e => e.LastName == last));
        Assert.False(await scope.Db.Employees.AnyAsync(e => e.LastName == $"{last}b"));
    }

    [Fact]
    public async Task Numbers_are_generated_and_never_collide_with_existing_ones()
    {
        using var scope = await AdminScopeAsync();
        var result = await scope.Send(new ImportEmployeesCommand
        {
            DryRun = false,
            Rows = [Row(2, "Prvi", $"Num{Unique()}"), Row(3, "Drugi", $"Num{Unique()}")]
        });

        var numbers = result.Rows.Select(r => r.EmployeeNumber).ToList();

        Assert.Equal(2, numbers.Distinct().Count());
        Assert.All(numbers, n => Assert.StartsWith("R-", n));
    }

    [Fact]
    public async Task Someone_who_already_exists_is_left_alone_by_default()
    {
        var last = $"Ima{Unique()}";
        var existing = await InScope(scope => TestData.SeedEmployeeAsync(scope, firstName: "Ana", lastName: last));

        using var scope = await AdminScopeAsync();
        var result = await scope.Send(new ImportEmployeesCommand
        {
            DryRun = false,
            Rows = [new ImportEmployeeRow { Line = 2, FirstName = "Ana", LastName = last, Phone = "061 111" }]
        });

        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Created);

        var reloaded = await scope.Db.Employees.AsNoTracking().SingleAsync(e => e.Id == existing.Id);
        Assert.Null(reloaded.Phone);
    }

    [Fact]
    public async Task Update_mode_fills_gaps_but_never_overwrites()
    {
        var last = $"Dop{Unique()}";
        var existing = await InScope(async scope =>
        {
            var e = await TestData.SeedEmployeeAsync(scope, firstName: "Ana", lastName: last);
            e.Phone = "061 000";
            await scope.Db.SaveChangesAsync();
            return e;
        });

        using var scope = await AdminScopeAsync();
        var result = await scope.Send(new ImportEmployeesCommand
        {
            DryRun = false,
            OnDuplicate = ImportDuplicateHandling.Update,
            Rows = [new ImportEmployeeRow { Line = 2, FirstName = "Ana", LastName = last, Phone = "062 999", Email = "ana@example.com" }]
        });

        Assert.Equal(1, result.Updated);

        var reloaded = await scope.Db.Employees.AsNoTracking().SingleAsync(e => e.Id == existing.Id);
        Assert.Equal("061 000", reloaded.Phone);
        Assert.Equal("ana@example.com", reloaded.Email);
    }

    [Fact]
    public async Task The_same_person_twice_in_one_file_is_reported()
    {
        var last = $"Dup{Unique()}";

        using var scope = await AdminScopeAsync();
        var result = await scope.Send(new ImportEmployeesCommand
        {
            DryRun = false,
            Rows = [Row(2, "Ana", last), Row(3, "Ana", last)]
        });

        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(1, await scope.Db.Employees.CountAsync(e => e.LastName == last));
    }

    // ---- invitations -----------------------------------------------------

    private async Task<(TestScope Scope, Domain.Entities.Employee Employee)> InviterAsync(
        UserRole role = UserRole.Admin)
    {
        var scope = await AdminScopeAsync(role);
        var employee = await TestData.SeedEmployeeAsync(scope, lastName: $"Inv{Unique()}");
        return (scope, employee);
    }

    [Fact]
    public async Task Accepting_an_invitation_creates_the_account_for_that_employee()
    {
        var (scope, employee) = await InviterAsync();
        using var _ = scope;

        var invitation = await scope.Send(new CreateEmployeeInvitationCommand { EmployeeId = employee.Id });
        var email = $"pozvan-{Unique()}@example.com";

        var created = await scope.Send(new AcceptInvitationCommand
        {
            Token = invitation.Token,
            Email = email,
            Password = "Sigurna-2026x"
        });

        Assert.Equal(email, created);

        var user = await scope.Db.Users.AsNoTracking().SingleAsync(u => u.EmployeeId == employee.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal(UserRole.Worker, user.Role);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task A_link_works_once()
    {
        var (scope, employee) = await InviterAsync();
        using var _ = scope;

        var invitation = await scope.Send(new CreateEmployeeInvitationCommand { EmployeeId = employee.Id });

        await scope.Send(new AcceptInvitationCommand
        {
            Token = invitation.Token,
            Email = $"jednom-{Unique()}@example.com",
            Password = "Sigurna-2026x"
        });

        await Assert.ThrowsAsync<NotFoundException>(() => scope.Send(new GetInvitationQuery(invitation.Token)));
        await Assert.ThrowsAsync<NotFoundException>(() => scope.Send(new AcceptInvitationCommand
        {
            Token = invitation.Token,
            Email = $"opet-{Unique()}@example.com",
            Password = "Sigurna-2026x"
        }));
    }

    [Fact]
    public async Task An_expired_link_is_refused_the_same_way_as_an_unknown_one()
    {
        var (scope, employee) = await InviterAsync();
        using var _ = scope;

        var invitation = await scope.Send(new CreateEmployeeInvitationCommand { EmployeeId = employee.Id });

        scope.Clock.Advance(InvitationTokens.Lifetime + TimeSpan.FromMinutes(1));

        await Assert.ThrowsAsync<NotFoundException>(() => scope.Send(new GetInvitationQuery(invitation.Token)));
        await Assert.ThrowsAsync<NotFoundException>(() => scope.Send(new GetInvitationQuery("nepoznat-token")));
    }

    [Fact]
    public async Task A_newer_invitation_cancels_the_older_one()
    {
        var (scope, employee) = await InviterAsync();
        using var _ = scope;

        var first = await scope.Send(new CreateEmployeeInvitationCommand { EmployeeId = employee.Id });
        var second = await scope.Send(new CreateEmployeeInvitationCommand { EmployeeId = employee.Id });

        await Assert.ThrowsAsync<NotFoundException>(() => scope.Send(new GetInvitationQuery(first.Token)));
        Assert.Equal(employee.FirstName, (await scope.Send(new GetInvitationQuery(second.Token))).FirstName);
    }

    [Fact]
    public async Task Only_a_hash_of_the_token_is_stored()
    {
        var (scope, employee) = await InviterAsync();
        using var _ = scope;

        var invitation = await scope.Send(new CreateEmployeeInvitationCommand { EmployeeId = employee.Id });
        var stored = await scope.Db.EmployeeInvitations.AsNoTracking().SingleAsync(i => i.EmployeeId == employee.Id);

        Assert.NotEqual(invitation.Token, stored.TokenHash);
        Assert.Equal(InvitationTokens.Hash(invitation.Token), stored.TokenHash);
    }

    [Fact]
    public async Task Someone_who_already_has_an_account_cannot_be_invited()
    {
        var (scope, employee) = await InviterAsync();
        using var _ = scope;

        await TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id);

        await Assert.ThrowsAsync<ConflictException>(() =>
            scope.Send(new CreateEmployeeInvitationCommand { EmployeeId = employee.Id }));
    }

    [Fact]
    public async Task An_admin_cannot_invite_someone_into_a_role_above_their_own()
    {
        var (scope, employee) = await InviterAsync(UserRole.Admin);
        using var _ = scope;

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => scope.Send(new CreateEmployeeInvitationCommand
        {
            EmployeeId = employee.Id,
            Role = UserRole.SuperAdmin
        }));
    }

    [Fact]
    public async Task A_taken_email_is_a_conflict_and_leaves_the_link_usable()
    {
        var (scope, employee) = await InviterAsync();
        using var _ = scope;

        var taken = await TestData.SeedUserAsync(scope, UserRole.Worker);
        var invitation = await scope.Send(new CreateEmployeeInvitationCommand { EmployeeId = employee.Id });

        await Assert.ThrowsAsync<ConflictException>(() => scope.Send(new AcceptInvitationCommand
        {
            Token = invitation.Token,
            Email = taken.Email,
            Password = "Sigurna-2026x"
        }));

        // The person can try again with another address.
        Assert.NotNull(await scope.Send(new GetInvitationQuery(invitation.Token)));
    }

    // ---- checklist -------------------------------------------------------

    [Fact]
    public async Task An_employee_without_an_account_or_project_shows_up_and_leaves_when_fixed()
    {
        var (scope, employee) = await InviterAsync();
        using var _ = scope;

        var before = await scope.Send(new GetSetupChecklistQuery());
        var withoutAccount = before.Items.Single(i => i.Key == "employeesWithoutAccount").Count;
        var withoutProject = before.Items.Single(i => i.Key == "employeesWithoutProject").Count;

        await TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id);
        var project = await TestData.SeedProjectAsync(scope);
        await scope.Send(new Construction.Application.Features.Employees.Commands.AssignEmployeeToProject
            .AssignEmployeeToProjectCommand(employee.Id, project.Id));

        var after = await scope.Send(new GetSetupChecklistQuery());

        Assert.Equal(withoutAccount - 1, after.Items.SingleOrDefault(i => i.Key == "employeesWithoutAccount")?.Count ?? 0);
        Assert.Equal(withoutProject - 1, after.Items.SingleOrDefault(i => i.Key == "employeesWithoutProject")?.Count ?? 0);
    }

    // ---- the health part of the list --------------------------------------

    [Fact]
    public async Task A_super_admin_is_told_that_mail_and_push_are_not_set_up()
    {
        // The test host leaves SMTP and Firebase unconfigured.
        using var scope = await AdminScopeAsync(UserRole.SuperAdmin);

        var list = await scope.Send(new GetSetupChecklistQuery());

        Assert.Contains(list.Items, i => i.Key == "emailNotConfigured");
        Assert.Contains(list.Items, i => i.Key == "pushNotConfigured");
    }

    [Fact]
    public async Task An_admin_is_not_told_because_they_cannot_set_them_up()
    {
        using var scope = await AdminScopeAsync(UserRole.Admin);

        var list = await scope.Send(new GetSetupChecklistQuery());

        Assert.DoesNotContain(list.Items, i => i.Key == "emailNotConfigured");
        Assert.DoesNotContain(list.Items, i => i.Key == "pushNotConfigured");
    }
}
