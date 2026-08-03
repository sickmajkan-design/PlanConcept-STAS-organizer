using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Absences.Commands.DeleteAbsence;
using Construction.Application.Features.Absences.Commands.RequestAbsence;
using Construction.Application.Features.Absences.Commands.ReviewAbsence;
using Construction.Application.Features.Absences.Models;
using Construction.Application.Features.Absences.Queries.GetAbsences;
using Construction.Application.Features.Absences.Queries.GetSchedule;
using Construction.Application.Features.Employees.Commands.AssignEmployeeToProject;
using Construction.Application.Features.Employees.Commands.RemoveEmployeeFromProject;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The schedule runs against PostgreSQL because its two central rules are
/// exclusion constraints over date ranges — something no in-memory provider
/// has — and because the board is one query whose whole point is not being
/// fifty.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class ScheduleTests : IntegrationTestBase
{
    public ScheduleTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static readonly DateOnly Monday = new(2026, 8, 3);

    private static void ActAs(TestScope scope, User user, Guid? employeeId = null) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, employeeId, user.Email);

    // ---- postings --------------------------------------------------------

    [Fact]
    public async Task An_assignment_without_dates_starts_today_and_runs_on()
    {
        // What the model meant before it had dates; existing callers keep working.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await InScope(scope => scope.Send(
            new AssignEmployeeToProjectCommand(employee.Id, project.Id)));

        var posting = await InScope(scope => scope.Db.EmployeeProjects
            .Where(ep => ep.EmployeeId == employee.Id)
            .SingleAsync());

        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), posting.StartDate);
        Assert.Null(posting.EndDate);
    }

    [Fact]
    public async Task The_same_person_can_move_between_sites_through_the_week()
    {
        // The whole reason assignments grew dates.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var first = await InScope(scope => TestData.SeedProjectAsync(scope));
        var second = await InScope(scope => TestData.SeedProjectAsync(scope));

        await InScope(scope => scope.Send(
            new AssignEmployeeToProjectCommand(employee.Id, first.Id)
            {
                StartDate = Monday,
                EndDate = Monday.AddDays(2)
            }));

        await InScope(scope => scope.Send(
            new AssignEmployeeToProjectCommand(employee.Id, second.Id)
            {
                StartDate = Monday.AddDays(3),
                EndDate = Monday.AddDays(4)
            }));

        var count = await InScope(scope =>
            scope.Db.EmployeeProjects.CountAsync(ep => ep.EmployeeId == employee.Id));

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task The_same_person_may_cover_two_sites_at_once()
    {
        // A supervisor doing exactly this is real; forbidding it would make
        // the board lie about where people are.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var first = await InScope(scope => TestData.SeedProjectAsync(scope));
        var second = await InScope(scope => TestData.SeedProjectAsync(scope));

        foreach (var project in new[] { first, second })
        {
            await InScope(scope => scope.Send(
                new AssignEmployeeToProjectCommand(employee.Id, project.Id)
                {
                    StartDate = Monday,
                    EndDate = Monday.AddDays(4)
                }));
        }

        var count = await InScope(scope =>
            scope.Db.EmployeeProjects.CountAsync(ep => ep.EmployeeId == employee.Id));

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task The_same_person_twice_on_one_site_over_the_same_days_is_refused()
    {
        // Always a data-entry mistake.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await InScope(scope => scope.Send(
            new AssignEmployeeToProjectCommand(employee.Id, project.Id)
            {
                StartDate = Monday,
                EndDate = Monday.AddDays(4)
            }));

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope => scope.Send(
            new AssignEmployeeToProjectCommand(employee.Id, project.Id)
            {
                StartDate = Monday.AddDays(2),
                EndDate = Monday.AddDays(6)
            })));
    }

    [Fact]
    public async Task Returning_to_a_site_later_is_a_second_posting()
    {
        // The old composite key made this unrepresentable.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await InScope(scope => scope.Send(
            new AssignEmployeeToProjectCommand(employee.Id, project.Id)
            {
                StartDate = Monday,
                EndDate = Monday.AddDays(4)
            }));

        await InScope(scope => scope.Send(
            new AssignEmployeeToProjectCommand(employee.Id, project.Id)
            {
                StartDate = Monday.AddDays(30),
                EndDate = Monday.AddDays(34)
            }));

        var count = await InScope(scope => scope.Db.EmployeeProjects
            .CountAsync(ep => ep.EmployeeId == employee.Id && ep.ProjectId == project.Id));

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Removing_someone_from_a_site_closes_the_posting_rather_than_erasing_it()
    {
        // They were there; deleting it would make the board disagree with the
        // timesheets sitting next to it.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await InScope(scope => scope.Send(
            new AssignEmployeeToProjectCommand(employee.Id, project.Id)
            {
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10)
            }));

        await InScope(scope => scope.Send(
            new RemoveEmployeeFromProjectCommand(employee.Id, project.Id)));

        var posting = await InScope(scope => scope.Db.EmployeeProjects
            .Where(ep => ep.EmployeeId == employee.Id)
            .SingleAsync());

        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), posting.EndDate);
    }

    [Fact]
    public async Task Removing_a_posting_that_has_not_started_deletes_it()
    {
        // It never happened, so there is nothing to preserve.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));

        await InScope(scope => scope.Send(
            new AssignEmployeeToProjectCommand(employee.Id, project.Id)
            {
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7)
            }));

        await InScope(scope => scope.Send(
            new RemoveEmployeeFromProjectCommand(employee.Id, project.Id)));

        var remaining = await InScope(scope =>
            scope.Db.EmployeeProjects.CountAsync(ep => ep.EmployeeId == employee.Id));

        Assert.Equal(0, remaining);
    }

    // ---- absences --------------------------------------------------------

    [Fact]
    public async Task A_worker_books_their_own_leave_as_a_request()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        var absence = await InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new RequestAbsenceCommand
            {
                StartDate = Monday,
                EndDate = Monday.AddDays(4)
            });
        });

        Assert.Equal(AbsenceStatus.Requested, absence.Status);
        Assert.Equal(5, absence.DayCount);
    }

    [Fact]
    public async Task A_worker_cannot_book_leave_for_somebody_else()
    {
        var mine = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var theirs = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, mine.Id));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, worker, mine.Id);
            return scope.Send(new RequestAbsenceCommand
            {
                EmployeeId = theirs.Id,
                StartDate = Monday,
                EndDate = Monday
            });
        }));
    }

    [Fact]
    public async Task A_worker_cannot_grant_their_own_leave()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new RequestAbsenceCommand
            {
                StartDate = Monday,
                EndDate = Monday,
                Approve = true
            });
        }));
    }

    [Fact]
    public async Task Nobody_grants_their_own_leave_whatever_their_role()
    {
        // The same control the timesheets carry: without it, a foreman's own
        // holiday approves itself.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var foreman = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Foreman, employee.Id));

        var absence = await InScope(scope =>
        {
            ActAs(scope, foreman, employee.Id);
            return scope.Send(new RequestAbsenceCommand
            {
                StartDate = Monday,
                EndDate = Monday.AddDays(2)
            });
        });

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScope(scope =>
        {
            ActAs(scope, foreman, employee.Id);
            return scope.Send(new ReviewAbsenceCommand
            {
                Id = absence.Id,
                Approve = true
            });
        }));
    }

    [Fact]
    public async Task Two_granted_absences_cannot_cover_the_same_day()
    {
        var (employee, reviewer) = await SeedReviewablePairAsync();

        await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new RequestAbsenceCommand
            {
                EmployeeId = employee.Id,
                StartDate = Monday,
                EndDate = Monday.AddDays(6),
                Approve = true
            });
        });

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new RequestAbsenceCommand
            {
                EmployeeId = employee.Id,
                Type = AbsenceType.SickLeave,
                StartDate = Monday.AddDays(3),
                EndDate = Monday.AddDays(9),
                Approve = true
            });
        }));
    }

    [Fact]
    public async Task Two_unanswered_requests_may_overlap()
    {
        // A request is a question, not a fact: somebody asked twice.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        foreach (var _ in Enumerable.Range(0, 2))
        {
            await InScope(scope =>
            {
                ActAs(scope, worker, employee.Id);
                return scope.Send(new RequestAbsenceCommand
                {
                    StartDate = Monday,
                    EndDate = Monday.AddDays(4)
                });
            });
        }

        var count = await InScope(scope =>
            scope.Db.Absences.CountAsync(a => a.EmployeeId == employee.Id));

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Refusing_leave_keeps_the_reason()
    {
        var (employee, reviewer) = await SeedReviewablePairAsync();

        var absence = await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new RequestAbsenceCommand
            {
                EmployeeId = employee.Id,
                StartDate = Monday,
                EndDate = Monday.AddDays(2)
            });
        });

        var refused = await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new ReviewAbsenceCommand
            {
                Id = absence.Id,
                Approve = false,
                Note = "Rok na gradilištu te nedelje"
            });
        });

        Assert.Equal(AbsenceStatus.Rejected, refused.Status);
        Assert.Equal("Rok na gradilištu te nedelje", refused.ReviewNote);
    }

    [Fact]
    public async Task A_worker_withdraws_their_own_unanswered_request()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        var absence = await InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new RequestAbsenceCommand
            {
                StartDate = Monday,
                EndDate = Monday.AddDays(2)
            });
        });

        await InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new DeleteAbsenceCommand(absence.Id));
        });

        // Withdrawn, not erased: a supervisor who half-planned around it can
        // still see it was asked for and taken back.
        var status = await InScope(scope => scope.Db.Absences
            .Where(a => a.Id == absence.Id)
            .Select(a => a.Status)
            .SingleAsync());

        Assert.Equal(AbsenceStatus.Cancelled, status);
    }

    [Fact]
    public async Task A_worker_cannot_withdraw_leave_already_granted()
    {
        var (employee, reviewer) = await SeedReviewablePairAsync();
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        var absence = await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new RequestAbsenceCommand
            {
                EmployeeId = employee.Id,
                StartDate = Monday,
                EndDate = Monday.AddDays(2),
                Approve = true
            });
        });

        await Assert.ThrowsAsync<ConflictException>(() => InScope(scope =>
        {
            ActAs(scope, worker, employee.Id);
            return scope.Send(new DeleteAbsenceCommand(absence.Id));
        }));
    }

    [Fact]
    public async Task A_worker_sees_only_their_own_leave()
    {
        var mine = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var theirs = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var worker = await InScope(scope =>
            TestData.SeedUserAsync(scope, UserRole.Worker, mine.Id));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        foreach (var employeeId in new[] { mine.Id, theirs.Id })
        {
            await InScope(scope =>
            {
                ActAs(scope, admin);
                return scope.Send(new RequestAbsenceCommand
                {
                    EmployeeId = employeeId,
                    StartDate = Monday,
                    EndDate = Monday.AddDays(2)
                });
            });
        }

        var visible = await InScope(scope =>
        {
            ActAs(scope, worker, mine.Id);
            return scope.Send(new GetAbsencesQuery { EmployeeId = theirs.Id, PageSize = 50 });
        });

        Assert.All(visible.Items, a => Assert.Equal(mine.Id, a.EmployeeId));
    }

    // ---- the board -------------------------------------------------------

    [Fact]
    public async Task The_board_clips_a_posting_to_the_window()
    {
        // An open-ended posting begun last year has no visible start inside
        // this week; sending the real dates would make every client repeat the
        // same arithmetic.
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var project = await InScope(scope => TestData.SeedProjectAsync(scope));
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        await InScope(scope => scope.Send(
            new AssignEmployeeToProjectCommand(employee.Id, project.Id)
            {
                StartDate = Monday.AddDays(-90)
            }));

        var schedule = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new GetScheduleQuery
            {
                From = Monday,
                To = Monday.AddDays(6),
                AssignedOnly = true
            });
        });

        var row = Assert.Single(schedule.Rows, r => r.EmployeeId == employee.Id);
        var posting = Assert.Single(row.Assignments);

        Assert.Equal(Monday, posting.From);
        Assert.Equal(Monday.AddDays(6), posting.To);
        Assert.True(posting.ContinuesAfter);
    }

    [Fact]
    public async Task The_board_shows_granted_leave_and_not_a_pending_request()
    {
        // A board that greys somebody out on the strength of an unanswered
        // request would have work planned around leave nobody granted.
        var (employee, reviewer) = await SeedReviewablePairAsync();
        var pendingEmployee = await InScope(scope => TestData.SeedEmployeeAsync(scope));

        await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new RequestAbsenceCommand
            {
                EmployeeId = employee.Id,
                StartDate = Monday,
                EndDate = Monday.AddDays(2),
                Approve = true
            });
        });

        await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new RequestAbsenceCommand
            {
                EmployeeId = pendingEmployee.Id,
                StartDate = Monday,
                EndDate = Monday.AddDays(2)
            });
        });

        var schedule = await InScope(scope =>
        {
            ActAs(scope, reviewer);
            return scope.Send(new GetScheduleQuery
            {
                From = Monday,
                To = Monday.AddDays(6)
            });
        });

        var granted = Assert.Single(schedule.Rows, r => r.EmployeeId == employee.Id);
        Assert.Single(granted.Absences);

        var pending = Assert.Single(schedule.Rows, r => r.EmployeeId == pendingEmployee.Id);
        Assert.Empty(pending.Absences);
    }

    [Fact]
    public async Task The_board_includes_people_with_nothing_on_them()
    {
        // Exactly who a supervisor is looking for when filling a gap.
        var free = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        var schedule = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new GetScheduleQuery
            {
                From = Monday,
                To = Monday.AddDays(6)
            });
        });

        var row = Assert.Single(schedule.Rows, r => r.EmployeeId == free.Id);

        Assert.Empty(row.Assignments);
        Assert.Empty(row.Absences);
    }

    [Fact]
    public async Task A_window_wider_than_the_limit_is_refused()
    {
        var foreman = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Foreman));

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => InScope(scope =>
            {
                ActAs(scope, foreman);
                return scope.Send(new GetScheduleQuery
                {
                    From = Monday,
                    To = Monday.AddDays(GetScheduleQuery.MaxDays + 10)
                });
            }));
    }

    private async Task<(Employee Employee, User Reviewer)> SeedReviewablePairAsync()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var reviewer = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        return (employee, reviewer);
    }
}
