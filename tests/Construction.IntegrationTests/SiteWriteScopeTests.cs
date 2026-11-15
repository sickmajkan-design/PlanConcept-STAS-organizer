using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Attachments;
using Construction.Application.Features.Attachments.Commands.UploadAttachment;
using Construction.Application.Features.Attachments.Queries.GetAttachmentContent;
using Construction.Application.Features.Attachments.Queries.GetAttachments;
using Construction.Application.Features.Materials.Commands.AdjustMaterialQuantity;
using Construction.Application.Features.TimeEntries.Commands.DeleteTimeEntry;
using Construction.Application.Features.TimeEntries.Queries.GetTimeEntryById;
using Construction.Application.Features.Tools.Commands.AssignTool;
using Construction.Application.Features.WorkItems.Commands.ChangeWorkItemStatus;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.IntegrationTests;

/// <summary>
/// A foreman may change what belongs to the site they are posted to, and is told "not found" about everything on
/// another site, the same answer a missing record gets.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class SiteWriteScopeTests : IntegrationTestBase
{
    public SiteWriteScopeTests(DatabaseFixture fixture) : base(fixture)
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

    private Task<Guid> TimeEntryAsync(Setup s, Guid projectId) =>
        InScope(async scope =>
        {
            var entry = new TimeEntry
            {
                EmployeeId = s.Worker.Id,
                ProjectId = projectId,
                StartedAt = DateTime.UtcNow.AddDays(-2).AddHours(-8),
                EndedAt = DateTime.UtcNow.AddDays(-2),
                Status = TimeEntryStatus.Submitted,
            };
            scope.Db.TimeEntries.Add(entry);
            await scope.Db.SaveChangesAsync();
            return entry.Id;
        });

    [Fact]
    public async Task A_foreman_cannot_touch_or_read_hours_booked_to_another_site()
    {
        var s = await SeedAsync();
        var foreign = await TimeEntryAsync(s, s.Other.Id);
        var own = await TimeEntryAsync(s, s.Mine.Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsForeman(s, async scope => { await scope.Send(new DeleteTimeEntryCommand(foreign)); return 0; }));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsForeman(s, scope => scope.Send(new GetTimeEntryByIdQuery(foreign))));

        Assert.Equal(own, (await AsForeman(s, scope => scope.Send(new GetTimeEntryByIdQuery(own)))).Id);
    }

    [Fact]
    public async Task A_foreman_cannot_adjust_stock_or_assign_a_tool_that_belongs_to_another_site()
    {
        var s = await SeedAsync();

        var (material, tool, freeTool) = await InScope(async scope =>
        {
            var m = await TestData.SeedMaterialAsync(scope, 100);
            m.ProjectId = s.Other.Id;
            var t = await TestData.SeedToolAsync(scope);
            t.AssignedProjectId = s.Other.Id;
            var f = await TestData.SeedToolAsync(scope);
            await scope.Db.SaveChangesAsync();
            return (m, t, f);
        });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsForeman(s, scope => scope.Send(new AdjustMaterialQuantityCommand { Id = material.Id, Change = -5, Reason = "x" })));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsForeman(s, scope => scope.Send(new AssignToolToProjectCommand(tool.Id, s.Mine.Id))));
        // Free equipment is nobody's site: a foreman may post it to their own project, but not to another one.
        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsForeman(s, scope => scope.Send(new AssignToolToProjectCommand(freeTool.Id, s.Other.Id))));

        var posted = await AsForeman(s, scope => scope.Send(new AssignToolToProjectCommand(freeTool.Id, s.Mine.Id)));
        Assert.Equal(s.Mine.Id, posted.AssignedProjectId);
    }

    [Fact]
    public async Task A_foreman_cannot_change_the_status_of_work_on_another_site()
    {
        var s = await SeedAsync();

        var (foreign, own) = await InScope(async scope =>
        {
            var a = new WorkItem { Title = "Other site", ProjectId = s.Other.Id };
            var b = new WorkItem { Title = "My site", ProjectId = s.Mine.Id };
            scope.Db.WorkItems.AddRange(a, b);
            await scope.Db.SaveChangesAsync();
            return (a.Id, b.Id);
        });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsForeman(s, scope => scope.Send(new ChangeWorkItemStatusCommand { Id = foreign, Status = WorkItemStatus.Resolved })));

        var changed = await AsForeman(s, scope => scope.Send(new ChangeWorkItemStatusCommand { Id = own, Status = WorkItemStatus.Resolved }));
        Assert.Equal(WorkItemStatus.Resolved, changed.Status);
    }

    [Fact]
    public async Task A_foreman_reads_the_documents_of_their_own_site_and_not_of_another()
    {
        var s = await SeedAsync();

        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        async Task<Guid> UploadTo(Guid projectId) => await InScope(async scope =>
        {
            scope.CurrentUser.SignInAs(admin.Id, UserRole.Admin, null, admin.Email);
            var bytes = System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 plan");
            return (await scope.Send(new UploadAttachmentCommand
            {
                OwnerType = AttachmentOwnerType.Project,
                OwnerId = projectId,
                Category = AttachmentCategory.Contract,
                FileName = "plan.pdf",
                SizeBytes = bytes.Length,
                Content = new MemoryStream(bytes),
            })).Id;
        });

        var mine = await UploadTo(s.Mine.Id);
        var other = await UploadTo(s.Other.Id);

        Assert.Single(await AsForeman(s, scope => scope.Send(new GetAttachmentsQuery { OwnerType = AttachmentOwnerType.Project, OwnerId = s.Mine.Id })));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsForeman(s, scope => scope.Send(new GetAttachmentsQuery { OwnerType = AttachmentOwnerType.Project, OwnerId = s.Other.Id })));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            AsForeman(s, scope => scope.Send(new GetAttachmentContentQuery(other))));
        Assert.NotNull(await AsForeman(s, scope => scope.Send(new GetAttachmentContentQuery(mine))));
    }
}
