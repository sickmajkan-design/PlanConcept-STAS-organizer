using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Planning;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>Copying a week, saved week templates, crews, planned against reported hours and the reminder to confirm.</summary>
[Collection(DatabaseCollection.Name)]
public class PlanningWeekToolsTests : IntegrationTestBase
{
    public PlanningWeekToolsTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static DateOnly MondayOf(DateOnly day) => day.AddDays(-(((int)day.DayOfWeek + 6) % 7));

    /// <summary>A Monday far from the weeks other tests use, and four weeks from the one the next call gives, so postings of other tests do not fall into the weeks copied here.</summary>
    private static int _weeks;

    private static DateOnly FarMonday() => MondayOf(Today).AddDays(7 * (150 + 4 * Interlocked.Increment(ref _weeks)));

    private Task PostAsync(Guid employeeId, Guid projectId, DateOnly from, DateOnly to) =>
        InScopeAs(UserRole.Admin, scope => scope.Send(new SetEmployeeScheduleCommand { EmployeeId = employeeId, ProjectId = projectId, From = from, To = to }));

    private async Task<List<(DateOnly From, DateOnly? To, Guid ProjectId)>> PostingsAsync(Guid employeeId) =>
        (await InScope(scope => scope.Db.EmployeeProjects.AsNoTracking().Where(p => p.EmployeeId == employeeId).OrderBy(p => p.StartDate).ToListAsync()))
            .Select(p => (p.StartDate, p.EndDate, p.ProjectId))
            .ToList();

    [Fact]
    public async Task Copying_a_week_fills_only_the_free_days_and_reports_what_it_left_alone()
    {
        var worker = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var a = await InScope(scope => TestData.SeedProjectAsync(scope));
        var b = await InScope(scope => TestData.SeedProjectAsync(scope));
        var source = FarMonday();
        var target = source.AddDays(7);

        await PostAsync(worker.Id, a.Id, source, source.AddDays(1));
        await PostAsync(worker.Id, b.Id, source.AddDays(2), source.AddDays(4));

        // The target week: Wednesday already taken, Thursday on leave.
        await PostAsync(worker.Id, b.Id, target.AddDays(2), target.AddDays(2));
        await InScope(async scope =>
        {
            scope.Db.Absences.Add(new Absence { EmployeeId = worker.Id, Type = AbsenceType.AnnualLeave, Status = AbsenceStatus.Approved, StartDate = target.AddDays(3), EndDate = target.AddDays(3) });
            await scope.Db.SaveChangesAsync();
            return 0;
        });

        var preview = await InScopeAs(UserRole.Admin, scope => scope.Send(new CopyScheduleWeekCommand { FromWeek = source.AddDays(3), ToWeek = target.AddDays(1), DryRun = true }));
        var mine = preview.Ranges.Where(r => r.EmployeeId == worker.Id).ToList();

        // Monday and Tuesday on A, Friday on B; Wednesday is taken and Thursday is leave.
        Assert.Equal(new[] { (a.Id, target, target.AddDays(1)), (b.Id, target.AddDays(4), target.AddDays(4)) }, mine.Select(r => (r.ProjectId, r.From, r.To)).ToArray());
        Assert.True(preview.SkippedTaken >= 1);
        Assert.True(preview.SkippedOnLeave >= 1);
        Assert.DoesNotContain(await PostingsAsync(worker.Id), p => p.From == target);

        var done = await InScopeAs(UserRole.Admin, scope => scope.Send(new CopyScheduleWeekCommand { FromWeek = source, ToWeek = target }));
        Assert.Equal(3, done.Ranges.Where(r => r.EmployeeId == worker.Id).Sum(r => r.To.DayNumber - r.From.DayNumber + 1));

        var after = await PostingsAsync(worker.Id);
        Assert.Contains(after, p => p.ProjectId == a.Id && p.From == target && p.To == target.AddDays(1));
        Assert.Contains(after, p => p.ProjectId == b.Id && p.From <= target.AddDays(4) && p.To >= target.AddDays(4));

        // Copying again changes nothing: everything that could be filled is filled now.
        var again = await InScopeAs(UserRole.Admin, scope => scope.Send(new CopyScheduleWeekCommand { FromWeek = source, ToWeek = target }));
        Assert.DoesNotContain(again.Ranges, r => r.EmployeeId == worker.Id);
    }

    [Fact]
    public async Task Copying_a_week_onto_itself_is_refused_and_a_foreman_without_the_right_may_not_copy()
    {
        var monday = FarMonday();

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(() =>
            InScopeAs(UserRole.Admin, scope => scope.Send(new CopyScheduleWeekCommand { FromWeek = monday, ToWeek = monday.AddDays(2) })));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            InScopeAs(UserRole.Foreman, scope => scope.Send(new CopyScheduleWeekCommand { FromWeek = monday, ToWeek = monday.AddDays(7) })));
    }

    [Fact]
    public async Task A_saved_template_can_be_put_on_another_week_and_deleted()
    {
        var worker = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));
        var source = FarMonday();
        var target = source.AddDays(14);
        var name = $"Standard {Guid.NewGuid():N}";

        await PostAsync(worker.Id, site.Id, source, source.AddDays(4));

        var id = await InScopeAs(UserRole.Admin, scope => scope.Send(new SaveScheduleTemplateCommand { Name = name, Week = source.AddDays(2) }));

        var listed = await InScopeAs(UserRole.Admin, scope => scope.Send(new GetScheduleTemplatesQuery()));
        var template = Assert.Single(listed, t => t.Id == id);
        Assert.True(template.People >= 1);
        Assert.True(template.Days >= 5);

        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(() =>
            InScopeAs(UserRole.Admin, scope => scope.Send(new SaveScheduleTemplateCommand { Name = name, Week = source })));

        var preview = await InScopeAs(UserRole.Admin, scope => scope.Send(new ApplyScheduleTemplateCommand { TemplateId = id, Week = target, DryRun = true }));
        Assert.Contains(preview.Ranges, r => r.EmployeeId == worker.Id && r.From == target && r.To == target.AddDays(4));
        Assert.DoesNotContain(await PostingsAsync(worker.Id), p => p.From == target);

        await InScopeAs(UserRole.Admin, scope => scope.Send(new ApplyScheduleTemplateCommand { TemplateId = id, Week = target }));
        Assert.Contains(await PostingsAsync(worker.Id), p => p.ProjectId == site.Id && p.From <= target && (p.To == null || p.To >= target.AddDays(4)));

        await InScopeAs(UserRole.Admin, scope => scope.Send(new DeleteScheduleTemplateCommand(id)));
        Assert.DoesNotContain(await InScopeAs(UserRole.Admin, scope => scope.Send(new GetScheduleTemplatesQuery())), t => t.Id == id);
    }

    [Fact]
    public async Task A_crew_moves_all_its_members_and_a_worker_belongs_to_one_crew_only()
    {
        var one = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var two = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));
        var from = FarMonday();

        var crewId = await InScopeAs(UserRole.Admin, scope => scope.Send(new SaveCrewCommand { Name = $"Ekipa {Guid.NewGuid():N}", LeaderEmployeeId = one.Id, EmployeeIds = [one.Id, two.Id] }));

        var crews = await InScopeAs(UserRole.Admin, scope => scope.Send(new GetCrewsQuery()));
        var crew = Assert.Single(crews, c => c.Id == crewId);
        Assert.Equal(one.Id, crew.LeaderEmployeeId);
        Assert.Equal(2, crew.Members.Count);

        // The second crew may not take somebody who is already in one.
        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(() =>
            InScopeAs(UserRole.Admin, scope => scope.Send(new SaveCrewCommand { Name = $"Druga {Guid.NewGuid():N}", EmployeeIds = [two.Id] })));

        var moved = await InScopeAs(UserRole.Admin, scope => scope.Send(new AssignCrewCommand { CrewId = crewId, ProjectId = site.Id, From = from, To = from.AddDays(4) }));
        Assert.Equal(2, moved);

        foreach (var member in new[] { one, two })
        {
            Assert.Contains(await PostingsAsync(member.Id), p => p.ProjectId == site.Id && p.From == from && p.To == from.AddDays(4));
        }

        // Taking a member out of the crew frees the place for another crew.
        await InScopeAs(UserRole.Admin, scope => scope.Send(new SaveCrewCommand { Id = crewId, Name = crew.Name, LeaderEmployeeId = one.Id, EmployeeIds = [one.Id] }));
        await InScopeAs(UserRole.Admin, scope => scope.Send(new SaveCrewCommand { Name = $"Treca {Guid.NewGuid():N}", EmployeeIds = [two.Id] }));

        await InScopeAs(UserRole.Admin, scope => scope.Send(new DeleteCrewCommand(crewId)));
        Assert.DoesNotContain(await InScopeAs(UserRole.Admin, scope => scope.Send(new GetCrewsQuery())), c => c.Id == crewId);
    }

    [Fact]
    public async Task A_foreman_may_not_keep_crews()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => InScopeAs(UserRole.Foreman, scope => scope.Send(new GetCrewsQuery())));
    }

    [Fact]
    public async Task Planned_hours_are_set_against_the_hours_people_reported()
    {
        var worker = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));
        var monday = MondayOf(Today).AddDays(-14);

        await PostAsync(worker.Id, site.Id, monday, monday.AddDays(4));

        await InScope(async scope =>
        {
            var start = monday.AddDays(1).ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc);
            scope.Db.TimeEntries.Add(new TimeEntry { EmployeeId = worker.Id, ProjectId = site.Id, StartedAt = start, EndedAt = start.AddHours(9), BreakMinutes = 60, Status = TimeEntryStatus.Approved });
            await scope.Db.SaveChangesAsync();
            return 0;
        });

        var result = await InScopeAs(UserRole.Admin, scope => scope.Send(new GetPlannedHoursQuery { From = monday, To = monday.AddDays(4) }));
        var row = Assert.Single(result.Projects, p => p.ProjectId == site.Id);

        Assert.Equal(5, row.PlannedDays);
        Assert.Equal(40m, row.PlannedHours);
        Assert.Equal(40m, row.PlannedHoursSoFar);
        Assert.Equal(8m, row.ReportedHours);
    }

    [Fact]
    public async Task Publishing_a_day_tells_each_posted_worker_where_they_go_and_is_recorded()
    {
        var posted = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var onLeave = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var free = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope, "Hala Brod"));
        var day = Today.AddDays(20);
        var postedUser = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, posted.Id));
        await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, onLeave.Id));
        await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, free.Id));

        await PostAsync(posted.Id, site.Id, day, day);
        await PostAsync(onLeave.Id, site.Id, day, day);
        await InScope(async scope =>
        {
            scope.Db.Absences.Add(new Absence { EmployeeId = onLeave.Id, Type = AbsenceType.AnnualLeave, Status = AbsenceStatus.Approved, StartDate = day, EndDate = day });
            await scope.Db.SaveChangesAsync();
            return 0;
        });

        var result = await InScopeAs(UserRole.Admin, scope => scope.Send(new PublishScheduleDayCommand { Date = day }));
        Assert.True(result.People >= 1);

        var notices = await InScope(scope => scope.Db.Notifications.AsNoTracking().Where(n => n.UserId == postedUser.Id && n.Title.StartsWith("Your schedule for")).ToListAsync());
        var notice = Assert.Single(notices);
        Assert.Contains("Hala Brod", notice.Body);
        Assert.Contains("\"published\"", notice.DataJson);

        // The one on leave and the one posted nowhere hear nothing.
        Assert.Empty(await InScope(scope => scope.Db.Notifications.AsNoTracking().Where(n => n.UserId != postedUser.Id && n.Title.StartsWith("Your schedule for") && (n.Body.Contains("Hala Brod") && n.Body.Contains(day.ToString("dd.MM.yyyy")))).Join(scope.Db.Users, n => n.UserId, u => u.Id, (n, u) => u).Where(u => u.EmployeeId == onLeave.Id || u.EmployeeId == free.Id).ToListAsync()));

        var listed = await InScopeAs(UserRole.Admin, scope => scope.Send(new GetSchedulePublicationsQuery { From = day, To = day }));
        Assert.Equal(result.People, Assert.Single(listed).People);
    }

    [Fact]
    public async Task A_day_that_has_passed_cannot_be_published_and_a_foreman_without_the_right_may_not()
    {
        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(() =>
            InScopeAs(UserRole.Admin, scope => scope.Send(new PublishScheduleDayCommand { Date = Today.AddDays(-1) })));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            InScopeAs(UserRole.Foreman, scope => scope.Send(new PublishScheduleDayCommand { Date = Today.AddDays(1) })));
    }

    [Fact]
    public async Task Everybody_who_has_not_confirmed_is_reminded_once()
    {
        var worker = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var site = await InScope(scope => TestData.SeedProjectAsync(scope));
        await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, worker.Id));
        var day = FarMonday();

        await PostAsync(worker.Id, site.Id, day, day.AddDays(4));

        // Everybody posted that far ahead without confirming is reminded, this worker among them.
        var reminded = await InScopeAs(UserRole.Admin, scope => scope.Send(new RemindUnconfirmedPostingsCommand { Date = day }));
        Assert.True(reminded >= 1);

        await InScope(async scope =>
        {
            await scope.Db.EmployeeProjects.Where(p => p.EmployeeId == worker.Id).ExecuteUpdateAsync(p => p.SetProperty(x => x.AcknowledgedAt, DateTime.UtcNow));
            return 0;
        });

        var afterwards = await InScopeAs(UserRole.Admin, scope => scope.Send(new RemindUnconfirmedPostingsCommand { Date = day }));
        Assert.Equal(reminded - 1, afterwards);
    }
}
