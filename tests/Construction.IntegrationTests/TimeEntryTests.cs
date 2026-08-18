using Construction.Application.Common.Exceptions;
using Construction.Application.Features.TimeEntries.Commands.ClockIn;
using Construction.Application.Features.TimeEntries.Commands.ClockOut;
using Construction.Application.Features.TimeEntries.Commands.CreateTimeEntry;
using Construction.Application.Features.TimeEntries.Commands.DeleteTimeEntry;
using Construction.Application.Features.TimeEntries.Commands.ReviewTimeEntry;
using Construction.Application.Features.TimeEntries.Commands.UpdateTimeEntry;
using Construction.Application.Features.TimeEntries.Queries.GetCurrentTimeEntry;
using Construction.Application.Features.TimeEntries.Queries.GetTimeEntries;
using Construction.Application.Features.TimeEntries.Queries.GetTimeEntrySummary;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Work time is the one module where a wrong number is money, so these run
/// against PostgreSQL: the "one open shift" rule is a partial unique index,
/// and the summary is a GROUP BY over an interval subtraction. Neither exists
/// outside a real database.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class TimeEntryTests : IntegrationTestBase
{
    public TimeEntryTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    /// <summary>An employee with an account, which is what clocking in needs.</summary>
    private async Task<(Employee Employee, User User)> SeedWorkerAsync(TestScope scope)
    {
        var employee = await TestData.SeedEmployeeAsync(scope);
        var user = await TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id);

        return (employee, user);
    }

    private static void ActAs(TestScope scope, User user, Guid? employeeId) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, employeeId, user.Email);

    // ---- clocking in and out -------------------------------------------

    [Fact]
    public async Task Clocking_in_starts_a_shift_that_is_still_running()
    {
        var (employee, user) = await InScope(SeedWorkerAsync);

        var entry = await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new ClockInCommand());
        });

        Assert.Equal(TimeEntryStatus.InProgress, entry.Status);
        Assert.Null(entry.EndedAt);
        Assert.Null(entry.WorkedMinutes);
        Assert.Equal(employee.Id, entry.EmployeeId);
    }

    [Fact]
    public async Task Clocking_in_twice_is_refused()
    {
        var (employee, user) = await InScope(SeedWorkerAsync);

        await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new ClockInCommand());
        });

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new ClockInCommand());
        }));
    }

    [Fact]
    public async Task Clocking_out_records_the_worked_time_less_the_break()
    {
        var (employee, user) = await InScope(SeedWorkerAsync);
        var start = DateTime.UtcNow.AddHours(-8);

        await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            scope.Clock.FreezeAt(start);
            return scope.Send(new ClockInCommand());
        });

        var entry = await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            scope.Clock.FreezeAt(start.AddHours(8));
            return scope.Send(new ClockOutCommand { BreakMinutes = 30 });
        });

        Assert.Equal(TimeEntryStatus.Submitted, entry.Status);
        Assert.Equal(450, entry.WorkedMinutes);
    }

    [Fact]
    public async Task Clocking_out_without_being_clocked_in_is_refused()
    {
        var (employee, user) = await InScope(SeedWorkerAsync);

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new ClockOutCommand());
        }));
    }

    [Fact]
    public async Task A_shift_left_running_overnight_cannot_be_closed_by_the_worker()
    {
        // The app cannot know when they actually stopped, and a guess would be
        // indistinguishable from a real shift afterwards.
        var (employee, user) = await InScope(SeedWorkerAsync);
        var start = DateTime.UtcNow.AddHours(-30);

        await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            scope.Clock.FreezeAt(start);
            return scope.Send(new ClockInCommand());
        });

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            scope.Clock.FreezeAt(start.AddHours(30));
            return scope.Send(new ClockOutCommand());
        }));
    }

    [Fact]
    public async Task A_break_as_long_as_the_shift_is_refused()
    {
        var (employee, user) = await InScope(SeedWorkerAsync);
        var start = DateTime.UtcNow.AddHours(-2);

        await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            scope.Clock.FreezeAt(start);
            return scope.Send(new ClockInCommand());
        });

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            scope.Clock.FreezeAt(start.AddHours(2));
            return scope.Send(new ClockOutCommand { BreakMinutes = 120 });
        }));
    }

    // ---- clocking in and out with no signal ----------------------------
    //
    // A worker starts at seven in a basement, or finishes in a lift shaft with
    // one bar. The moment they started or stopped is the one thing this system
    // cannot work out afterwards, so the handset stamps it and sends it when
    // the signal comes back. These assert that the moment survives the journey
    // — the whole point of the feature — and that it is bounded on arrival.

    [Fact]
    public async Task A_shift_started_with_no_signal_keeps_the_moment_it_started()
    {
        var (employee, user) = await InScope(SeedWorkerAsync);
        var startedInTheBasement = DateTime.UtcNow.AddHours(-2);

        var entry = await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);

            // The request arrives two hours late, when the phone found signal.
            scope.Clock.FreezeAt(DateTime.UtcNow);

            return scope.Send(new ClockInCommand
            {
                OccurredAt = startedInTheBasement,
            });
        });

        // Not "now". If this recorded the arrival time instead, every shift
        // begun out of signal would be short by however long the outage was.
        Assert.Equal(
            startedInTheBasement,
            entry.StartedAt,
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task A_whole_shift_recorded_offline_is_measured_by_the_handset()
    {
        var (employee, user) = await InScope(SeedWorkerAsync);
        var start = DateTime.UtcNow.AddHours(-9);
        var end = DateTime.UtcNow.AddHours(-1);

        await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            scope.Clock.FreezeAt(DateTime.UtcNow);
            return scope.Send(new ClockInCommand { OccurredAt = start });
        });

        var entry = await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            scope.Clock.FreezeAt(DateTime.UtcNow);
            return scope.Send(new ClockOutCommand
            {
                OccurredAt = end,
                BreakMinutes = 30,
            });
        });

        // Eight hours between the handset's two stamps, less the break — and
        // nothing to do with when either request reached the server.
        Assert.Equal(450, entry.WorkedMinutes);
    }

    [Fact]
    public async Task A_shift_cannot_be_ended_before_it_began()
    {
        var (employee, user) = await InScope(SeedWorkerAsync);

        await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new ClockInCommand());
        });

        // A handset whose clock is behind the server's, or a queued clock-out
        // replayed against a shift somebody else opened later. Storing it
        // would give the entry a negative length that every hours total
        // downstream would then carry.
        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new ClockOutCommand
            {
                OccurredAt = DateTime.UtcNow.AddHours(-3),
            });
        }));
    }

    [Fact]
    public async Task A_shift_stamped_last_week_is_refused()
    {
        var (employee, user) = await InScope(SeedWorkerAsync);

        // Past a day, the shift is over and everyone has gone home. What the
        // office needs is a correction somebody signs off, not a stale
        // timestamp arriving from a phone that was in a drawer.
        await Assert.ThrowsAsync<ValidationException>(() => InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new ClockInCommand
            {
                OccurredAt = DateTime.UtcNow.AddDays(-7),
            });
        }));
    }

    [Fact]
    public async Task A_shift_stamped_in_the_future_is_refused()
    {
        var (employee, user) = await InScope(SeedWorkerAsync);

        await Assert.ThrowsAsync<ValidationException>(() => InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new ClockInCommand
            {
                OccurredAt = DateTime.UtcNow.AddHours(2),
            });
        }));
    }

    [Fact]
    public async Task An_offline_shift_still_cannot_overlap_one_already_recorded()
    {
        var (employee, user) = await InScope(SeedWorkerAsync);
        var start = DateTime.UtcNow.AddHours(-6);

        await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            scope.Clock.FreezeAt(start);
            return scope.Send(new ClockInCommand());
        });

        await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            scope.Clock.FreezeAt(start.AddHours(4));
            return scope.Send(new ClockOutCommand());
        });

        // Backdating past a shift that is already on record is exactly the
        // case the overlap rule exists for, and being offline is not a reason
        // to be let through it.
        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new ClockInCommand
            {
                OccurredAt = start.AddHours(1),
            });
        }));
    }

    [Fact]
    public async Task An_account_with_no_employee_record_cannot_record_time()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, user, null);
            return scope.Send(new ClockInCommand());
        }));
    }

    [Fact]
    public async Task The_current_shift_is_null_when_off_shift_and_set_when_on_one()
    {
        var (employee, user) = await InScope(SeedWorkerAsync);

        var before = await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new GetCurrentTimeEntryQuery());
        });

        Assert.Null(before);

        await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new ClockInCommand());
        });

        var during = await InScope(scope =>
        {
            ActAs(scope, user, employee.Id);
            return scope.Send(new GetCurrentTimeEntryQuery());
        });

        Assert.NotNull(during);
        Assert.Null(during!.EndedAt);
    }

    // ---- overlap --------------------------------------------------------

    [Fact]
    public async Task A_manual_entry_overlapping_an_existing_shift_is_refused()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var start = DateTime.UtcNow.AddHours(-10);

        await InScope(scope => scope.Send(new CreateTimeEntryCommand
        {
            EmployeeId = employee.Id,
            StartedAt = start,
            EndedAt = start.AddHours(8)
        }));

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
            scope.Send(new CreateTimeEntryCommand
            {
                EmployeeId = employee.Id,
                StartedAt = start.AddHours(4),
                EndedAt = start.AddHours(6)
            })));
    }

    [Fact]
    public async Task Back_to_back_shifts_are_allowed()
    {
        // A shift ending at 14:00 and one starting at 14:00 do not overlap;
        // that is how a split day is actually recorded.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var start = DateTime.UtcNow.AddHours(-10);

        await InScope(scope => scope.Send(new CreateTimeEntryCommand
        {
            EmployeeId = employee.Id,
            StartedAt = start,
            EndedAt = start.AddHours(4)
        }));

        var second = await InScope(scope => scope.Send(new CreateTimeEntryCommand
        {
            EmployeeId = employee.Id,
            StartedAt = start.AddHours(4),
            EndedAt = start.AddHours(8)
        }));

        Assert.Equal(240, second.WorkedMinutes);
    }

    [Fact]
    public async Task Two_employees_may_work_the_same_hours()
    {
        var first = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var second = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var start = DateTime.UtcNow.AddHours(-8);

        await InScope(scope => scope.Send(new CreateTimeEntryCommand
        {
            EmployeeId = first.Id,
            StartedAt = start,
            EndedAt = start.AddHours(8)
        }));

        var entry = await InScope(scope => scope.Send(new CreateTimeEntryCommand
        {
            EmployeeId = second.Id,
            StartedAt = start,
            EndedAt = start.AddHours(8)
        }));

        Assert.Equal(second.Id, entry.EmployeeId);
    }

    // ---- review ---------------------------------------------------------

    [Fact]
    public async Task Nobody_can_sign_off_their_own_hours()
    {
        // The one control that makes an approval mean anything.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var manager = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.ProjectManager, employee.Id));

        var start = DateTime.UtcNow.AddHours(-8);
        var entry = await InScope(scope => scope.Send(new CreateTimeEntryCommand
        {
            EmployeeId = employee.Id,
            StartedAt = start,
            EndedAt = start.AddHours(8)
        }));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, manager, employee.Id);
            return scope.Send(new ReviewTimeEntryCommand { Id = entry.Id, Approve = true });
        }));
    }

    [Fact]
    public async Task Approving_records_who_signed_it_off()
    {
        var (entry, reviewer) = await SeedSubmittedEntryAsync();

        var approved = await InScope(scope =>
        {
            ActAs(scope, reviewer, null);
            return scope.Send(new ReviewTimeEntryCommand { Id = entry.Id, Approve = true });
        });

        Assert.Equal(TimeEntryStatus.Approved, approved.Status);
        Assert.Equal(reviewer.Email, approved.ReviewedByName);
        Assert.NotNull(approved.ReviewedAt);
        Assert.Null(approved.ReviewNote);
    }

    [Fact]
    public async Task A_running_shift_cannot_be_reviewed()
    {
        var (employee, worker) = await InScope(SeedWorkerAsync);
        var reviewer = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        var entry = await InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new ClockInCommand());
        });

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, reviewer, null);
            return scope.Send(new ReviewTimeEntryCommand { Id = entry.Id, Approve = true });
        }));
    }

    [Fact]
    public async Task Rejecting_keeps_the_reason_and_leaves_the_entry_editable()
    {
        var (entry, reviewer) = await SeedSubmittedEntryAsync();

        var rejected = await InScope(scope =>
        {
            ActAs(scope, reviewer, null);
            return scope.Send(new ReviewTimeEntryCommand
            {
                Id = entry.Id,
                Approve = false,
                Note = "Break not recorded"
            });
        });

        Assert.Equal(TimeEntryStatus.Rejected, rejected.Status);
        Assert.Equal("Break not recorded", rejected.ReviewNote);

        // Editable again, and the correction re-submits it.
        var corrected = await InScope(scope => scope.Send(new UpdateTimeEntryCommand
        {
            Id = entry.Id,
            EmployeeId = entry.EmployeeId,
            StartedAt = entry.StartedAt,
            EndedAt = entry.EndedAt,
            BreakMinutes = 30
        }));

        Assert.Equal(TimeEntryStatus.Submitted, corrected.Status);
        Assert.Null(corrected.ReviewNote);
    }

    // ---- locking --------------------------------------------------------

    [Fact]
    public async Task An_approved_entry_cannot_be_edited()
    {
        var (entry, reviewer) = await SeedSubmittedEntryAsync();

        await InScope(scope =>
        {
            ActAs(scope, reviewer, null);
            return scope.Send(new ReviewTimeEntryCommand { Id = entry.Id, Approve = true });
        });

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
            scope.Send(new UpdateTimeEntryCommand
            {
                Id = entry.Id,
                EmployeeId = entry.EmployeeId,
                StartedAt = entry.StartedAt,
                EndedAt = entry.EndedAt,
                BreakMinutes = 0
            })));
    }

    [Fact]
    public async Task An_approved_entry_cannot_be_deleted()
    {
        // Hours that quietly vanish from a timesheet are the one thing a
        // worker cannot argue with.
        var (entry, reviewer) = await SeedSubmittedEntryAsync();

        await InScope(scope =>
        {
            ActAs(scope, reviewer, null);
            return scope.Send(new ReviewTimeEntryCommand { Id = entry.Id, Approve = true });
        });

        await Assert.ThrowsAsync<ConflictException>(() =>
            InScope(scope => scope.Send(new DeleteTimeEntryCommand(entry.Id))));
    }

    [Fact]
    public async Task A_submitted_entry_can_be_deleted()
    {
        var (entry, _) = await SeedSubmittedEntryAsync();

        await InScope(scope => scope.Send(new DeleteTimeEntryCommand(entry.Id)));

        var remaining = await InScope(scope =>
            scope.Db.TimeEntries.CountAsync(t => t.Id == entry.Id));

        Assert.Equal(0, remaining);
    }

    // ---- who sees what --------------------------------------------------

    [Fact]
    public async Task A_worker_sees_only_their_own_hours()
    {
        var (mine, worker) = await InScope(SeedWorkerAsync);
        var theirs = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var start = DateTime.UtcNow.AddHours(-8);

        foreach (var employeeId in new[] { mine.Id, theirs.Id })
        {
            await InScope(scope => scope.Send(new CreateTimeEntryCommand
            {
                EmployeeId = employeeId,
                StartedAt = start,
                EndedAt = start.AddHours(8)
            }));
        }

        var visible = await InScope(scope =>
        {
            ActAs(scope, worker, mine.Id);
            return scope.Send(new GetTimeEntriesQuery { PageSize = 50 });
        });

        Assert.NotEmpty(visible.Items);
        Assert.All(visible.Items, item => Assert.Equal(mine.Id, item.EmployeeId));
    }

    [Fact]
    public async Task A_worker_asking_for_someone_else_still_gets_only_their_own()
    {
        // Filtering rather than refusing: a 403 would confirm the id exists.
        var (mine, worker) = await InScope(SeedWorkerAsync);
        var theirs = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var start = DateTime.UtcNow.AddHours(-8);

        await InScope(scope => scope.Send(new CreateTimeEntryCommand
        {
            EmployeeId = theirs.Id,
            StartedAt = start,
            EndedAt = start.AddHours(8)
        }));

        var visible = await InScope(scope =>
        {
            ActAs(scope, worker, mine.Id);
            return scope.Send(new GetTimeEntriesQuery
            {
                EmployeeId = theirs.Id,
                PageSize = 50
            });
        });

        Assert.All(visible.Items, item => Assert.Equal(mine.Id, item.EmployeeId));
    }

    [Fact]
    public async Task A_foreman_sees_everyone()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));
        var start = DateTime.UtcNow.AddHours(-8);

        await InScope(scope => scope.Send(new CreateTimeEntryCommand
        {
            EmployeeId = employee.Id,
            StartedAt = start,
            EndedAt = start.AddHours(8)
        }));

        var visible = await InScope(scope =>
        {
            ActAs(scope, foreman, null);
            return scope.Send(new GetTimeEntriesQuery
            {
                EmployeeId = employee.Id,
                PageSize = 50
            });
        });

        Assert.Contains(visible.Items, item => item.EmployeeId == employee.Id);
    }

    // ---- summary --------------------------------------------------------

    [Fact]
    public async Task The_summary_totals_worked_minutes_and_separates_the_approved_part()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var reviewer = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        var day = DateTime.UtcNow.Date.AddDays(-3);

        // 8h less a 30m break, then 4h with no break.
        var first = await InScope(scope => scope.Send(new CreateTimeEntryCommand
        {
            EmployeeId = employee.Id,
            StartedAt = day.AddHours(7),
            EndedAt = day.AddHours(15),
            BreakMinutes = 30
        }));

        await InScope(scope => scope.Send(new CreateTimeEntryCommand
        {
            EmployeeId = employee.Id,
            StartedAt = day.AddHours(16),
            EndedAt = day.AddHours(20)
        }));

        await InScope(scope =>
        {
            ActAs(scope, reviewer, null);
            return scope.Send(new ReviewTimeEntryCommand { Id = first.Id, Approve = true });
        });

        var summary = await InScope(scope =>
        {
            ActAs(scope, reviewer, null);
            return scope.Send(new GetTimeEntrySummaryQuery
            {
                From = day,
                To = day.AddDays(1),
                EmployeeId = employee.Id
            });
        });

        var row = Assert.Single(summary.Rows);

        Assert.Equal(2, row.EntryCount);
        Assert.Equal(450 + 240, row.TotalMinutes);
        Assert.Equal(450, row.ApprovedMinutes);
        Assert.Equal(1, row.PendingCount);
    }

    [Fact]
    public async Task The_summary_ignores_a_shift_still_running()
    {
        var (employee, worker) = await InScope(SeedWorkerAsync);

        await InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new ClockInCommand());
        });

        var summary = await InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new GetTimeEntrySummaryQuery
            {
                From = DateTime.UtcNow.Date,
                To = DateTime.UtcNow.Date.AddDays(1)
            });
        });

        Assert.Equal(0, summary.TotalMinutes);
    }

    private async Task<(Construction.Application.Features.TimeEntries.Models.TimeEntryDto Entry, User Reviewer)>
        SeedSubmittedEntryAsync()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var reviewer = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.ProjectManager));

        var start = DateTime.UtcNow.AddHours(-8);

        var entry = await InScope(scope => scope.Send(new CreateTimeEntryCommand
        {
            EmployeeId = employee.Id,
            StartedAt = start,
            EndedAt = start.AddHours(8)
        }));

        return (entry, reviewer);
    }
}
