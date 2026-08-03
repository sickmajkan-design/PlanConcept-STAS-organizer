using Construction.Application.Common.Exceptions;
using Construction.Application.Features.WorkItems.Commands.ChangeWorkItemStatus;
using Construction.Application.Features.WorkItems.Commands.CreateWorkItem;
using Construction.Application.Features.WorkItems.Commands.DeleteWorkItem;
using Construction.Application.Features.WorkItems.Commands.SendDueReminders;
using Construction.Application.Features.WorkItems.Commands.UpdateWorkItem;
using Construction.Application.Features.WorkItems.Models;
using Construction.Application.Features.WorkItems.Queries.GetWorkItems;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Work items run against PostgreSQL because the rules that matter are check
/// constraints — a defect must have a site, a position must be whole — and the
/// deadline sweep is a conditional UPDATE whose whole purpose is atomicity.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class WorkItemTests : IntegrationTestBase
{
    public WorkItemTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user, Guid? employeeId = null) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, employeeId, user.Email);

    private static Task<WorkItemDto> CreateAsync(
        TestScope scope,
        WorkItemKind kind = WorkItemKind.Task,
        Guid? projectId = null,
        Guid? assignedEmployeeId = null,
        DateOnly? dueDate = null,
        string title = "Popravi ogradu") =>
        scope.Send(new CreateWorkItemCommand
        {
            Kind = kind,
            Title = title,
            ProjectId = projectId,
            AssignedEmployeeId = assignedEmployeeId,
            DueDate = dueDate
        });

    // ---- raising ---------------------------------------------------------

    [Fact]
    public async Task A_new_item_starts_open()
    {
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        var item = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(scope);
        });

        Assert.Equal(WorkItemStatus.Open, item.Status);
        Assert.False(item.IsFinished);
        Assert.Null(item.ResolvedAt);
    }

    [Fact]
    public async Task A_defect_without_a_site_is_refused()
    {
        // "Crack in the wall" does not locate itself.
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => InScope(scope =>
            {
                ActAs(scope, foreman);
                return CreateAsync(scope, WorkItemKind.Defect);
            }));
    }

    [Fact]
    public async Task A_task_without_a_site_is_allowed()
    {
        // Ordering materials is real work that belongs to no site.
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        var item = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(scope, WorkItemKind.Task, title: "Naruči cement");
        });

        Assert.Null(item.ProjectId);
    }

    [Fact]
    public async Task A_worker_may_report_a_defect_but_not_raise_a_task()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        var defect = await InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return CreateAsync(scope, WorkItemKind.Defect, project.Id);
        });

        Assert.Equal(WorkItemKind.Defect, defect.Kind);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return CreateAsync(scope, WorkItemKind.Task);
        }));
    }

    [Fact]
    public async Task A_worker_cannot_hand_work_to_someone()
    {
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var other = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return CreateAsync(
                scope, WorkItemKind.Defect, project.Id, assignedEmployeeId: other.Id);
        }));
    }

    // ---- status ----------------------------------------------------------

    [Fact]
    public async Task An_assignee_can_start_and_resolve_their_own_work()
    {
        var (item, worker, employeeId) = await SeedAssignedAsync();

        var started = await InScope(scope =>
        {
            ActAs(scope, worker, employeeId);
            return scope.Send(new ChangeWorkItemStatusCommand
            {
                Id = item.Id,
                Status = WorkItemStatus.InProgress
            });
        });

        Assert.Equal(WorkItemStatus.InProgress, started.Status);

        var resolved = await InScope(scope =>
        {
            ActAs(scope, worker, employeeId);
            return scope.Send(new ChangeWorkItemStatusCommand
            {
                Id = item.Id,
                Status = WorkItemStatus.Resolved
            });
        });

        Assert.Equal(WorkItemStatus.Resolved, resolved.Status);
        Assert.NotNull(resolved.ResolvedAt);
    }

    [Fact]
    public async Task A_worker_cannot_sign_their_own_work_off_as_closed()
    {
        // Closing is the check that it was done, not part of doing it.
        var (item, worker, employeeId) = await SeedAssignedAsync();

        await InScope(scope =>
        {
            ActAs(scope, worker, employeeId);
            return scope.Send(new ChangeWorkItemStatusCommand
            {
                Id = item.Id,
                Status = WorkItemStatus.Resolved
            });
        });

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, worker, employeeId);
            return scope.Send(new ChangeWorkItemStatusCommand
            {
                Id = item.Id,
                Status = WorkItemStatus.Closed
            });
        }));
    }

    [Fact]
    public async Task A_worker_cannot_touch_somebody_elses_work()
    {
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));
        var theirs = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var mine = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, mine.Id));

        var item = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(scope, assignedEmployeeId: theirs.Id);
        });

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, worker, mine.Id);
            return scope.Send(new ChangeWorkItemStatusCommand
            {
                Id = item.Id,
                Status = WorkItemStatus.InProgress
            });
        }));
    }

    [Fact]
    public async Task A_closed_item_cannot_be_moved_again()
    {
        // Reopening something signed off is a new item with its own record.
        var (item, _, _) = await SeedAssignedAsync();
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        foreach (var status in new[] { WorkItemStatus.Resolved, WorkItemStatus.Closed })
        {
            await InScope(scope =>
            {
                ActAs(scope, foreman);
                return scope.Send(new ChangeWorkItemStatusCommand
                {
                    Id = item.Id,
                    Status = status
                });
            });
        }

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new ChangeWorkItemStatusCommand
            {
                Id = item.Id,
                Status = WorkItemStatus.Open
            });
        }));
    }

    [Fact]
    public async Task Reopening_a_resolved_item_clears_who_resolved_it()
    {
        // Otherwise the record credits someone for work that demonstrably is
        // not finished.
        var (item, _, _) = await SeedAssignedAsync();
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new ChangeWorkItemStatusCommand
            {
                Id = item.Id,
                Status = WorkItemStatus.Resolved
            });
        });

        var reopened = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new ChangeWorkItemStatusCommand
            {
                Id = item.Id,
                Status = WorkItemStatus.InProgress
            });
        });

        Assert.Null(reopened.ResolvedAt);
        Assert.Null(reopened.ResolvedByName);
    }

    [Fact]
    public async Task A_cancelled_item_stays_cancelled()
    {
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        var item = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(scope);
        });

        await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new ChangeWorkItemStatusCommand
            {
                Id = item.Id,
                Status = WorkItemStatus.Cancelled
            });
        });

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new ChangeWorkItemStatusCommand
            {
                Id = item.Id,
                Status = WorkItemStatus.Open
            });
        }));
    }

    // ---- editing ---------------------------------------------------------

    [Fact]
    public async Task A_finished_item_cannot_be_edited()
    {
        var (item, _, _) = await SeedAssignedAsync();
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new ChangeWorkItemStatusCommand
            {
                Id = item.Id,
                Status = WorkItemStatus.Cancelled
            });
        });

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new UpdateWorkItemCommand
            {
                Id = item.Id,
                Kind = WorkItemKind.Task,
                Title = "Drugi naslov"
            });
        }));
    }

    [Fact]
    public async Task Moving_the_deadline_makes_the_reminder_owed_again()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        var item = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(
                scope,
                assignedEmployeeId: employee.Id,
                dueDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));
        });

        await InScope(scope => scope.Send(new SendDueRemindersCommand()));

        var beforeMove = await InScope(scope => scope.Db.WorkItems
            .Where(w => w.Id == item.Id)
            .Select(w => w.DueReminderSentAt)
            .SingleAsync());

        Assert.NotNull(beforeMove);

        await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new UpdateWorkItemCommand
            {
                Id = item.Id,
                Kind = WorkItemKind.Task,
                Title = item.Title,
                AssignedEmployeeId = employee.Id,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10)
            });
        });

        var afterMove = await InScope(scope => scope.Db.WorkItems
            .Where(w => w.Id == item.Id)
            .Select(w => w.DueReminderSentAt)
            .SingleAsync());

        Assert.Null(afterMove);
    }

    // ---- who sees what ---------------------------------------------------

    [Fact]
    public async Task A_worker_sees_only_their_own_list()
    {
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));
        var mine = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var theirs = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, mine.Id));

        foreach (var employeeId in new[] { mine.Id, theirs.Id })
        {
            await InScope(scope =>
            {
                ActAs(scope, foreman);
                return CreateAsync(scope, assignedEmployeeId: employeeId);
            });
        }

        var visible = await InScope(scope =>
        {
            ActAs(scope, worker, mine.Id);
            return scope.Send(new GetWorkItemsQuery { PageSize = 50 });
        });

        Assert.NotEmpty(visible.Items);
        Assert.All(visible.Items, i => Assert.Equal(mine.Id, i.AssignedEmployeeId));
    }

    [Fact]
    public async Task A_worker_asking_for_someone_else_still_gets_only_their_own()
    {
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));
        var mine = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var theirs = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, mine.Id));

        await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(scope, assignedEmployeeId: theirs.Id);
        });

        var visible = await InScope(scope =>
        {
            ActAs(scope, worker, mine.Id);
            return scope.Send(new GetWorkItemsQuery
            {
                AssignedEmployeeId = theirs.Id,
                PageSize = 50
            });
        });

        Assert.All(visible.Items, i => Assert.Equal(mine.Id, i.AssignedEmployeeId));
    }

    [Fact]
    public async Task The_overdue_filter_leaves_out_finished_work()
    {
        // Something closed last month is not a problem waiting to be dealt with.
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        var stillOpen = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(scope, dueDate: yesterday, title: "Zaostalo");
        });

        var cancelled = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(scope, dueDate: yesterday, title: "Otkazano");
        });

        await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new ChangeWorkItemStatusCommand
            {
                Id = cancelled.Id,
                Status = WorkItemStatus.Cancelled
            });
        });

        var overdue = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new GetWorkItemsQuery { OverdueOnly = true, PageSize = 50 });
        });

        Assert.Contains(overdue.Items, i => i.Id == stillOpen.Id);
        Assert.DoesNotContain(overdue.Items, i => i.Id == cancelled.Id);
    }

    [Fact]
    public async Task Undated_work_sorts_after_work_with_a_deadline()
    {
        // PostgreSQL puts nulls first on an ascending sort, which would push
        // everything nobody has dated to the top of the board.
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(scope, projectId: project.Id, title: "Bez roka");
        });

        await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(
                scope,
                projectId: project.Id,
                dueDate: DateOnly.FromDateTime(DateTime.UtcNow),
                title: "Sa rokom");
        });

        var page = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new GetWorkItemsQuery
            {
                ProjectId = project.Id,
                PageSize = 50
            });
        });

        Assert.Equal("Sa rokom", page.Items.First().Title);
    }

    // ---- reminders -------------------------------------------------------

    [Fact]
    public async Task A_deadline_reminder_goes_out_once_and_not_again()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(
                scope,
                assignedEmployeeId: employee.Id,
                dueDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));
        });

        var first = await InScope(scope => scope.Send(new SendDueRemindersCommand()));
        var second = await InScope(scope => scope.Send(new SendDueRemindersCommand()));

        Assert.True(first >= 1);
        Assert.Equal(0, second);
    }

    [Fact]
    public async Task Unassigned_work_is_left_unclaimed_for_a_later_sweep()
    {
        // Nobody to remind now, but there will be once it is handed out.
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        var item = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(
                scope, dueDate: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));
        });

        await InScope(scope => scope.Send(new SendDueRemindersCommand()));

        var mark = await InScope(scope => scope.Db.WorkItems
            .Where(w => w.Id == item.Id)
            .Select(w => w.DueReminderSentAt)
            .SingleAsync());

        Assert.Null(mark);
    }

    // ---- deleting --------------------------------------------------------

    [Fact]
    public async Task Only_an_administrator_can_delete()
    {
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        var item = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(scope);
        });

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new DeleteWorkItemCommand(item.Id));
        }));

        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new DeleteWorkItemCommand(item.Id));
        });

        var remaining = await InScope(scope =>
            scope.Db.WorkItems.CountAsync(w => w.Id == item.Id));

        Assert.Equal(0, remaining);
    }

    private async Task<(WorkItemDto Item, User Worker, Guid EmployeeId)> SeedAssignedAsync()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        var item = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return CreateAsync(scope, assignedEmployeeId: employee.Id);
        });

        return (item, worker, employee.Id);
    }
}
