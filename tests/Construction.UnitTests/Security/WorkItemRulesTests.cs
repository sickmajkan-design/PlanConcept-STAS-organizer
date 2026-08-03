using Construction.Application.Features.WorkItems;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.UnitTests.Security;

public class WorkItemRulesTests
{
    // ---- the lifecycle ---------------------------------------------------

    [Theory]
    [InlineData(WorkItemStatus.Open, WorkItemStatus.InProgress)]
    [InlineData(WorkItemStatus.Open, WorkItemStatus.Resolved)]
    [InlineData(WorkItemStatus.Open, WorkItemStatus.Cancelled)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Resolved)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Open)]
    [InlineData(WorkItemStatus.Resolved, WorkItemStatus.Closed)]
    public void Allows_the_ordinary_moves(WorkItemStatus from, WorkItemStatus to)
    {
        Assert.True(WorkItemRules.CanTransition(from, to));
    }

    [Theory]
    [InlineData(WorkItemStatus.Open, WorkItemStatus.Closed)]
    [InlineData(WorkItemStatus.InProgress, WorkItemStatus.Closed)]
    public void Refuses_closing_something_nobody_has_resolved(
        WorkItemStatus from,
        WorkItemStatus to)
    {
        // Closed means "checked and finished"; there is nothing to check yet.
        Assert.False(WorkItemRules.CanTransition(from, to));
    }

    [Fact]
    public void Allows_sending_resolved_work_back()
    {
        // "You have not actually fixed this" is an everyday correction.
        Assert.True(WorkItemRules.CanTransition(
            WorkItemStatus.Resolved, WorkItemStatus.InProgress));
        Assert.True(WorkItemRules.CanTransition(
            WorkItemStatus.Resolved, WorkItemStatus.Open));
    }

    [Theory]
    [InlineData(WorkItemStatus.Closed)]
    [InlineData(WorkItemStatus.Cancelled)]
    public void A_finished_item_goes_nowhere(WorkItemStatus terminal)
    {
        foreach (var target in Enum.GetValues<WorkItemStatus>())
        {
            Assert.False(WorkItemRules.CanTransition(terminal, target));
        }
    }

    [Fact]
    public void A_move_to_the_same_state_is_not_a_move()
    {
        foreach (var status in Enum.GetValues<WorkItemStatus>())
        {
            Assert.False(WorkItemRules.CanTransition(status, status));
        }
    }

    [Fact]
    public void Every_state_is_reachable_from_open()
    {
        // A state nothing can reach is dead code in the lifecycle, and the
        // table above is where that would hide.
        var reachable = new HashSet<WorkItemStatus> { WorkItemStatus.Open };
        var queue = new Queue<WorkItemStatus>([WorkItemStatus.Open]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var next in Enum.GetValues<WorkItemStatus>())
            {
                if (WorkItemRules.CanTransition(current, next) && reachable.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        Assert.Equal(Enum.GetValues<WorkItemStatus>().Length, reachable.Count);
    }

    // ---- who may do what -------------------------------------------------

    [Theory]
    [InlineData(UserRole.Foreman, WorkItemKind.Task, true)]
    [InlineData(UserRole.Foreman, WorkItemKind.Defect, true)]
    [InlineData(UserRole.Worker, WorkItemKind.Defect, true)]
    [InlineData(UserRole.Worker, WorkItemKind.Task, false)]
    public void A_worker_reports_defects_only(
        UserRole role,
        WorkItemKind kind,
        bool expected)
    {
        // Reporting a crack in front of you is exactly the right thing for
        // the person on site; handing out tasks is not.
        Assert.Equal(expected, WorkItemRules.CanCreate(role, kind));
    }

    [Fact]
    public void An_absent_role_creates_nothing()
    {
        Assert.False(WorkItemRules.CanCreate(null, WorkItemKind.Defect));
        Assert.False(WorkItemRules.CanCreate(null, WorkItemKind.Task));
    }

    [Fact]
    public void A_worker_may_move_their_own_item_and_no_other()
    {
        var mine = Guid.NewGuid();
        var theirs = Guid.NewGuid();

        Assert.True(WorkItemRules.CanModify(
            UserRole.Worker, mine, new WorkItem { AssignedEmployeeId = mine }));

        Assert.False(WorkItemRules.CanModify(
            UserRole.Worker, mine, new WorkItem { AssignedEmployeeId = theirs }));

        // Unassigned work is nobody's to move: a null caller must not match a
        // null assignee.
        Assert.False(WorkItemRules.CanModify(
            UserRole.Worker, null, new WorkItem { AssignedEmployeeId = null }));
    }

    [Fact]
    public void A_foreman_may_move_anything()
    {
        Assert.True(WorkItemRules.CanModify(
            UserRole.Foreman, null, new WorkItem { AssignedEmployeeId = Guid.NewGuid() }));
    }

    [Theory]
    [InlineData(UserRole.Foreman, true)]
    [InlineData(UserRole.ProjectManager, true)]
    [InlineData(UserRole.Worker, false)]
    public void Closing_and_assigning_stop_at_the_foreman(UserRole role, bool expected)
    {
        Assert.Equal(expected, WorkItemRules.CanClose(role));
        Assert.Equal(expected, WorkItemRules.CanAssign(role));
    }

    [Fact]
    public void Restriction_defaults_to_the_narrowest_reading()
    {
        Assert.True(WorkItemRules.IsRestrictedToOwnItems(null));
        Assert.True(WorkItemRules.IsRestrictedToOwnItems(UserRole.Worker));
        Assert.False(WorkItemRules.IsRestrictedToOwnItems(UserRole.Foreman));
    }

    // ---- overdue ---------------------------------------------------------

    [Fact]
    public void Overdue_means_past_the_date_and_still_to_do()
    {
        var today = new DateOnly(2026, 8, 3);
        var yesterday = new DateOnly(2026, 8, 2);

        Assert.True(new WorkItem
        {
            DueDate = yesterday,
            Status = WorkItemStatus.Open
        }.IsOverdueOn(today));

        // Something closed last month is not a problem waiting to be dealt with.
        Assert.False(new WorkItem
        {
            DueDate = yesterday,
            Status = WorkItemStatus.Closed
        }.IsOverdueOn(today));

        // The deadline day itself is not yet missed.
        Assert.False(new WorkItem
        {
            DueDate = today,
            Status = WorkItemStatus.Open
        }.IsOverdueOn(today));

        Assert.False(new WorkItem { Status = WorkItemStatus.Open }.IsOverdueOn(today));
    }
}
