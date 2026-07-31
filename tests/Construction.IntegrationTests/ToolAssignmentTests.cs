using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Projects.Commands.DeleteProject;
using Construction.Application.Features.Tools.Commands.AssignTool;
using Construction.Application.Features.Tools.Commands.UnassignTool;
using Construction.Application.Features.Tools.Queries.GetToolByQrCode;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A tool can be held by an employee and placed on a project at the same
/// time, and the two assignments are managed independently.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class ToolAssignmentTests : IntegrationTestBase
{
    public ToolAssignmentTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task A_tool_can_be_held_by_an_employee_and_placed_on_a_project_at_once()
    {
        var (tool, employee, project) = await InScope(async scope => (
            await TestData.SeedToolAsync(scope),
            await TestData.SeedEmployeeAsync(scope),
            await TestData.SeedProjectAsync(scope)));

        await InScope(scope =>
            scope.Send(new AssignToolToEmployeeCommand(tool.Id, employee.Id)));

        var result = await InScope(scope =>
            scope.Send(new AssignToolToProjectCommand(tool.Id, project.Id)));

        Assert.Equal(employee.Id, result.AssignedEmployeeId);
        Assert.Equal(project.Id, result.AssignedProjectId);
    }

    [Fact]
    public async Task Releasing_the_employee_leaves_the_project_placement_alone()
    {
        var (tool, employee, project) = await InScope(async scope => (
            await TestData.SeedToolAsync(scope),
            await TestData.SeedEmployeeAsync(scope),
            await TestData.SeedProjectAsync(scope)));

        await InScope(scope => scope.Send(new AssignToolToEmployeeCommand(tool.Id, employee.Id)));
        await InScope(scope => scope.Send(new AssignToolToProjectCommand(tool.Id, project.Id)));

        var result = await InScope(scope =>
            scope.Send(new UnassignToolCommand(tool.Id, ToolAssignmentTarget.Employee)));

        Assert.Null(result.AssignedEmployeeId);
        Assert.Equal(project.Id, result.AssignedProjectId);
    }

    [Fact]
    public async Task Releasing_the_project_leaves_the_holder_alone()
    {
        var (tool, employee, project) = await InScope(async scope => (
            await TestData.SeedToolAsync(scope),
            await TestData.SeedEmployeeAsync(scope),
            await TestData.SeedProjectAsync(scope)));

        await InScope(scope => scope.Send(new AssignToolToEmployeeCommand(tool.Id, employee.Id)));
        await InScope(scope => scope.Send(new AssignToolToProjectCommand(tool.Id, project.Id)));

        var result = await InScope(scope =>
            scope.Send(new UnassignToolCommand(tool.Id, ToolAssignmentTarget.Project)));

        Assert.Equal(employee.Id, result.AssignedEmployeeId);
        Assert.Null(result.AssignedProjectId);
    }

    [Fact]
    public async Task Handing_a_tool_to_another_employee_replaces_the_previous_holder()
    {
        var (tool, first, second) = await InScope(async scope => (
            await TestData.SeedToolAsync(scope),
            await TestData.SeedEmployeeAsync(scope),
            await TestData.SeedEmployeeAsync(scope)));

        await InScope(scope => scope.Send(new AssignToolToEmployeeCommand(tool.Id, first.Id)));

        var result = await InScope(scope =>
            scope.Send(new AssignToolToEmployeeCommand(tool.Id, second.Id)));

        Assert.Equal(second.Id, result.AssignedEmployeeId);
    }

    [Fact]
    public async Task Assigning_to_someone_who_does_not_exist_reports_not_found()
    {
        var tool = await InScope(scope => TestData.SeedToolAsync(scope));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope =>
                scope.Send(new AssignToolToEmployeeCommand(tool.Id, Guid.NewGuid()))));
    }

    [Fact]
    public async Task Deleting_a_project_releases_the_tools_standing_on_it()
    {
        var (tool, project) = await InScope(async scope => (
            await TestData.SeedToolAsync(scope),
            await TestData.SeedProjectAsync(scope)));

        await InScope(scope => scope.Send(new AssignToolToProjectCommand(tool.Id, project.Id)));
        await InScope(scope => scope.Send(new DeleteProjectCommand(project.Id)));

        var stored = await InScope(scope =>
            scope.Db.Tools.SingleAsync(t => t.Id == tool.Id));

        // Otherwise the tool would point at a project nobody can open.
        Assert.Null(stored.AssignedProjectId);
    }

    [Fact]
    public async Task A_tool_can_be_found_by_its_qr_code()
    {
        var tool = await InScope(async scope =>
        {
            var tool = await TestData.SeedToolAsync(scope);
            tool.QrCode = $"QR-{Guid.NewGuid():N}"[..12];
            await scope.Db.SaveChangesAsync();
            return tool;
        });

        var found = await InScope(scope =>
            scope.Send(new GetToolByQrCodeQuery(tool.QrCode!)));

        Assert.Equal(tool.Id, found.Id);
    }

    [Fact]
    public async Task An_unknown_qr_code_reports_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            InScope(scope => scope.Send(new GetToolByQrCodeQuery("QR-DOES-NOT-EXIST"))));
    }
}
