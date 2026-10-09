using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Branches;
using Construction.Application.Features.Projects.Commands.CreateProject;
using Construction.Application.Features.Projects.Queries.GetProjects;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// Business units nested under each other (a region, its branches, their offices): where a unit may
/// be placed, and that asking for a unit asks for everything under it.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class BranchTreeTests : IntegrationTestBase
{
    public BranchTreeTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}"[..28];

    private static async Task SignInAsync(TestScope scope)
    {
        var user = await TestData.SeedUserAsync(scope, UserRole.SuperAdmin);
        scope.CurrentUser.SignInAs(user.Id, UserRole.SuperAdmin, null, user.Email);
    }

    private async Task<BranchDto> CreateAsync(Guid? parent = null)
    {
        return await InScope(async scope =>
        {
            await SignInAsync(scope);

            return await scope.Send(new CreateBranchCommand
            {
                Name = UniqueName("Unit"),
                Color = "#3457D5",
                ParentBranchId = parent,
            });
        });
    }

    [Fact]
    public async Task A_unit_is_placed_under_another_and_nesting_stops_at_three_levels()
    {
        var region = await CreateAsync();
        var branch = await CreateAsync(region.Id);
        var office = await CreateAsync(branch.Id);

        Assert.Equal(region.Id, branch.ParentBranchId);
        Assert.Equal(branch.Id, office.ParentBranchId);

        await InScope(async scope =>
        {
            await SignInAsync(scope);

            await Assert.ThrowsAsync<ConflictException>(() => scope.Send(new CreateBranchCommand
            {
                Name = UniqueName("TooDeep"),
                Color = "#3457D5",
                ParentBranchId = office.Id,
            }));
        });
    }

    [Fact]
    public async Task A_unit_cannot_be_moved_under_itself_or_under_one_of_its_own_sub_units()
    {
        var region = await CreateAsync();
        var branch = await CreateAsync(region.Id);

        await InScope(async scope =>
        {
            await SignInAsync(scope);

            var underItself = await Assert.ThrowsAsync<ConflictException>(() => scope.Send(new UpdateBranchCommand
            {
                Id = region.Id,
                Name = region.Name,
                Color = region.Color,
                ParentBranchId = region.Id,
            }));
            Assert.Contains("itself", underItself.Message);

            await Assert.ThrowsAsync<ConflictException>(() => scope.Send(new UpdateBranchCommand
            {
                Id = region.Id,
                Name = region.Name,
                Color = region.Color,
                ParentBranchId = branch.Id,
            }));
        });
    }

    [Fact]
    public async Task A_unit_with_sub_units_cannot_be_deleted()
    {
        var region = await CreateAsync();
        await CreateAsync(region.Id);

        await InScope(async scope =>
        {
            await SignInAsync(scope);

            await Assert.ThrowsAsync<ConflictException>(() => scope.Send(new DeleteBranchCommand(region.Id)));
        });
    }

    [Fact]
    public async Task Asking_for_a_unit_includes_its_sub_units_but_not_its_siblings()
    {
        var region = await CreateAsync();
        var branch = await CreateAsync(region.Id);
        var sibling = await CreateAsync(region.Id);
        var elsewhere = await CreateAsync();

        await InScope(async scope =>
        {
            await SignInAsync(scope);

            var inBranch = await scope.Send(new CreateProjectCommand { Name = UniqueName("Site"), BranchId = branch.Id });
            var inSibling = await scope.Send(new CreateProjectCommand { Name = UniqueName("Site"), BranchId = sibling.Id });
            var inElsewhere = await scope.Send(new CreateProjectCommand { Name = UniqueName("Site"), BranchId = elsewhere.Id });

            var ids = async (Guid unit) =>
                (await scope.Send(new GetProjectsQuery { BranchId = unit, PageSize = 100 })).Items.Select(p => p.Id).ToHashSet();

            var forRegion = await ids(region.Id);
            Assert.Contains(inBranch.Id, forRegion);
            Assert.Contains(inSibling.Id, forRegion);
            Assert.DoesNotContain(inElsewhere.Id, forRegion);

            var forBranch = await ids(branch.Id);
            Assert.Equal([inBranch.Id], forBranch.ToArray());
        });
    }
}
