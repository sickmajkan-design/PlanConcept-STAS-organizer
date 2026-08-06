using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Notifications.Commands.MarkAllNotificationsRead;
using Construction.Application.Features.Notifications.Commands.MarkNotificationRead;
using Construction.Application.Features.Notifications.Commands.SendAnnouncement;
using Construction.Application.Features.Notifications.Queries.GetMyNotifications;
using Construction.Application.Features.Notifications.Queries.GetUnreadCount;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The inbox and the announcement audience, against PostgreSQL.
///
/// The audience filters are the part worth testing: an announcement cannot be
/// recalled, and a filter that quietly matches everybody sends somebody's
/// site-specific instruction to the whole company. The inbox itself is tested
/// for the same reason every read path is — that it returns the caller's own
/// rows and nobody else's.
/// </summary>
/// <remarks>
/// Every test class in this collection shares one database, so users seeded by
/// other tests are present too and a total recipient count is not a number
/// this file can predict. Assertions are therefore about specific people —
/// "this user was reached, that one was not" — except where a filter narrows
/// to records seeded by the test itself, which is deterministic.
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class NotificationTests : IntegrationTestBase
{
    public NotificationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user, Guid? employeeId = null) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, employeeId ?? user.EmployeeId, user.Email);

    private Task<int> InboxCountAsync(Guid userId, string title) =>
        InScope(scope => scope.Db.Notifications
            .CountAsync(n => n.UserId == userId && n.Title == title));

    /// <summary>A subject unique to one test, so the count is that test's own.</summary>
    private static string Subject() => $"Obaveštenje {Guid.NewGuid():N}";

    private static async Task<User> SeedCrewMemberAsync(
        TestScope scope,
        Project project,
        UserRole role = UserRole.Worker)
    {
        var employee = await TestData.SeedEmployeeAsync(scope);

        scope.Db.EmployeeProjects.Add(new EmployeeProject
        {
            EmployeeId = employee.Id,
            ProjectId = project.Id,
            StartDate = new DateOnly(2026, 1, 1),
            AssignedAt = DateTime.UtcNow
        });

        await scope.Db.SaveChangesAsync();

        return await TestData.SeedUserAsync(scope, role, employee.Id);
    }

    // ---- who an announcement reaches ------------------------------------

    [Fact]
    public async Task Announcement_without_filters_reaches_every_active_user()
    {
        var subject = Subject();

        var (admin, worker, suspended) = await InScope(async scope => (
            await TestData.SeedUserAsync(scope, UserRole.Admin),
            await TestData.SeedUserAsync(scope, UserRole.Worker),
            await TestData.SeedUserAsync(scope, UserRole.Worker, isActive: false)));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new SendAnnouncementCommand
            {
                Title = subject,
                Body = "Sutra se radi do 14h."
            });
        });

        Assert.Equal(1, await InboxCountAsync(admin.Id, subject));
        Assert.Equal(1, await InboxCountAsync(worker.Id, subject));

        // A disabled account is somebody who has left. Sending to them costs
        // nothing visible, which is exactly why it would go unnoticed.
        Assert.Equal(0, await InboxCountAsync(suspended.Id, subject));
    }

    [Fact]
    public async Task Role_filter_leaves_out_every_other_role()
    {
        var subject = Subject();

        var (admin, foreman, worker) = await InScope(async scope => (
            await TestData.SeedUserAsync(scope, UserRole.Admin),
            await TestData.SeedUserAsync(scope, UserRole.Foreman),
            await TestData.SeedUserAsync(scope, UserRole.Worker)));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new SendAnnouncementCommand
            {
                Title = subject,
                Body = "Sastanak poslovođa u 7h.",
                Role = UserRole.Foreman
            });
        });

        Assert.Equal(1, await InboxCountAsync(foreman.Id, subject));
        Assert.Equal(0, await InboxCountAsync(worker.Id, subject));

        // Including the sender, who is an Admin and not a Foreman.
        Assert.Equal(0, await InboxCountAsync(admin.Id, subject));
    }

    [Fact]
    public async Task Project_filter_reaches_only_that_site_s_crew()
    {
        var subject = Subject();

        var (admin, onSite, elsewhere, deskBound, recipients) = await InScope(async scope =>
        {
            var project = await TestData.SeedProjectAsync(scope);
            var otherProject = await TestData.SeedProjectAsync(scope);

            var admin = await TestData.SeedUserAsync(scope, UserRole.Admin);
            var onSite = await SeedCrewMemberAsync(scope, project);
            var elsewhere = await SeedCrewMemberAsync(scope, otherProject);

            // An account with no employee behind it cannot be on a crew, and
            // the filter has to leave it out rather than treat "no employee"
            // as "not excluded".
            var deskBound = await TestData.SeedUserAsync(scope, UserRole.Admin);

            ActAs(scope, admin);

            var recipients = await scope.Send(new SendAnnouncementCommand
            {
                Title = subject,
                Body = "Struja se gasi u 10h.",
                ProjectId = project.Id
            });

            return (admin, onSite, elsewhere, deskBound, recipients);
        });

        // Deterministic: nobody else in the shared database is on this site.
        Assert.Equal(1, recipients);

        Assert.Equal(1, await InboxCountAsync(onSite.Id, subject));
        Assert.Equal(0, await InboxCountAsync(elsewhere.Id, subject));
        Assert.Equal(0, await InboxCountAsync(deskBound.Id, subject));
        Assert.Equal(0, await InboxCountAsync(admin.Id, subject));
    }

    [Fact]
    public async Task Role_and_project_filters_narrow_together()
    {
        var subject = Subject();

        var (foremanOnSite, workerOnSite, recipients) = await InScope(async scope =>
        {
            var project = await TestData.SeedProjectAsync(scope);

            var admin = await TestData.SeedUserAsync(scope, UserRole.Admin);
            var foremanOnSite = await SeedCrewMemberAsync(scope, project, UserRole.Foreman);
            var workerOnSite = await SeedCrewMemberAsync(scope, project);

            ActAs(scope, admin);

            // "The foremen on the Danube job" — the case the panel offers two
            // pickers for. If these were alternatives rather than a
            // conjunction, every worker on the site would get it too.
            var recipients = await scope.Send(new SendAnnouncementCommand
            {
                Title = subject,
                Body = "Primopredaja u kancelariji.",
                Role = UserRole.Foreman,
                ProjectId = project.Id
            });

            return (foremanOnSite, workerOnSite, recipients);
        });

        Assert.Equal(1, recipients);
        Assert.Equal(1, await InboxCountAsync(foremanOnSite.Id, subject));
        Assert.Equal(0, await InboxCountAsync(workerOnSite.Id, subject));
    }

    [Fact]
    public async Task Announcement_is_filed_as_a_general_announcement()
    {
        var subject = Subject();

        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new SendAnnouncementCommand
            {
                Title = $"  {subject}  ",
                Body = "  Zimska oprema je obavezna.  "
            });
        });

        var notification = await InScope(scope => scope.Db.Notifications
            .AsNoTracking()
            .FirstAsync(n => n.UserId == admin.Id && n.Title == subject));

        Assert.Equal(NotificationType.GeneralAnnouncement, notification.Type);

        // Trimmed on the way in: a title with trailing spaces looks identical
        // and sorts differently.
        Assert.Equal("Zimska oprema je obavezna.", notification.Body);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task An_announcement_with_no_audience_reaches_nobody_and_says_so()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        var recipients = await InScope(async scope =>
        {
            // A site with no crew on it: a plausible mistake, and the only
            // thing that distinguishes it from a successful send is the count.
            var emptyProject = await TestData.SeedProjectAsync(scope);

            ActAs(scope, admin);

            return await scope.Send(new SendAnnouncementCommand
            {
                Title = Subject(),
                Body = "Niko ovo neće videti.",
                ProjectId = emptyProject.Id
            });
        });

        Assert.Equal(0, recipients);
    }

    // ---- the inbox -------------------------------------------------------

    [Fact]
    public async Task The_inbox_holds_only_the_caller_s_own_notifications()
    {
        var subject = Subject();

        var (foreman, worker) = await InScope(async scope => (
            await TestData.SeedUserAsync(scope, UserRole.Foreman),
            await TestData.SeedUserAsync(scope, UserRole.Worker)));

        await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new SendAnnouncementCommand
            {
                Title = subject,
                Body = "Samo poslovođama.",
                Role = UserRole.Foreman
            });
        });

        var foremansInbox = await InScope(scope =>
        {
            ActAs(scope, foreman);
            return scope.Send(new GetMyNotificationsQuery { PageSize = 50 });
        });

        Assert.Contains(foremansInbox.Items, item => item.Title == subject);

        // Not "the foreman's list is longer" but "every row in it is his":
        // the ids the query returned, checked against the owner column the
        // query is supposed to have filtered on.
        var returnedIds = foremansInbox.Items.Select(item => item.Id).ToList();

        var ownerIds = await InScope(scope => scope.Db.Notifications
            .AsNoTracking()
            .Where(n => returnedIds.Contains(n.Id))
            .Select(n => n.UserId)
            .Distinct()
            .ToListAsync());

        Assert.Equal([foreman.Id], ownerIds);

        var workersInbox = await InScope(scope =>
        {
            ActAs(scope, worker);
            return scope.Send(new GetMyNotificationsQuery { PageSize = 50 });
        });

        Assert.DoesNotContain(workersInbox.Items, item => item.Title == subject);
    }

    [Fact]
    public async Task Unread_only_hides_what_has_been_read()
    {
        var subject = Subject();

        var user = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker));

        await InScope(scope =>
        {
            ActAs(scope, user);
            return scope.Send(new SendAnnouncementCommand { Title = subject, Body = "Prva." });
        });

        var notificationId = await InScope(scope => scope.Db.Notifications
            .Where(n => n.UserId == user.Id && n.Title == subject)
            .Select(n => n.Id)
            .FirstAsync());

        await InScope(scope =>
        {
            ActAs(scope, user);
            return scope.Send(new MarkNotificationReadCommand(notificationId));
        });

        var unread = await InScope(scope =>
        {
            ActAs(scope, user);
            return scope.Send(new GetMyNotificationsQuery { PageSize = 50, UnreadOnly = true });
        });

        Assert.DoesNotContain(unread.Items, item => item.Id == notificationId);

        var all = await InScope(scope =>
        {
            ActAs(scope, user);
            return scope.Send(new GetMyNotificationsQuery { PageSize = 50 });
        });

        var read = Assert.Single(all.Items, item => item.Id == notificationId);
        Assert.True(read.IsRead);
        Assert.NotNull(read.ReadAt);
    }

    [Fact]
    public async Task Reading_somebody_else_s_notification_is_a_404()
    {
        var subject = Subject();

        var (owner, stranger) = await InScope(async scope => (
            await TestData.SeedUserAsync(scope, UserRole.Worker),
            await TestData.SeedUserAsync(scope, UserRole.Worker)));

        await InScope(scope =>
        {
            ActAs(scope, owner);
            return scope.Send(new SendAnnouncementCommand { Title = subject, Body = "Svima." });
        });

        var ownersNotificationId = await InScope(scope => scope.Db.Notifications
            .Where(n => n.UserId == owner.Id && n.Title == subject)
            .Select(n => n.Id)
            .FirstAsync());

        // 404 rather than 403: a refusal would confirm that the id is real,
        // which is the same reasoning every other read path here uses.
        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, stranger);
            return scope.Send(new MarkNotificationReadCommand(ownersNotificationId));
        }));

        var stillUnread = await InScope(scope => scope.Db.Notifications
            .AsNoTracking()
            .FirstAsync(n => n.Id == ownersNotificationId));

        Assert.False(stillUnread.IsRead);
    }

    [Fact]
    public async Task Marking_all_read_clears_the_badge_and_counts_what_it_changed()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker));

        for (var i = 0; i < 3; i++)
        {
            await InScope(scope =>
            {
                ActAs(scope, user);
                return scope.Send(new SendAnnouncementCommand
                {
                    Title = Subject(),
                    Body = "Poruka."
                });
            });
        }

        var changed = await InScope(scope =>
        {
            ActAs(scope, user);
            return scope.Send(new MarkAllNotificationsReadCommand());
        });

        Assert.Equal(3, changed);

        var badge = await InScope(scope =>
        {
            ActAs(scope, user);
            return scope.Send(new GetUnreadCountQuery());
        });

        Assert.Equal(0, badge);

        // Nothing left to change, so the second call reports none — the panel
        // shows this count, and repeating it would claim work that was not done.
        var again = await InScope(scope =>
        {
            ActAs(scope, user);
            return scope.Send(new MarkAllNotificationsReadCommand());
        });

        Assert.Equal(0, again);
    }

    [Fact]
    public async Task Marking_read_twice_is_harmless()
    {
        var subject = Subject();

        var user = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker));

        await InScope(scope =>
        {
            ActAs(scope, user);
            return scope.Send(new SendAnnouncementCommand { Title = subject, Body = "Poruka." });
        });

        var notificationId = await InScope(scope => scope.Db.Notifications
            .Where(n => n.UserId == user.Id && n.Title == subject)
            .Select(n => n.Id)
            .FirstAsync());

        await InScope(scope =>
        {
            ActAs(scope, user);
            return scope.Send(new MarkNotificationReadCommand(notificationId));
        });

        var firstReadAt = await InScope(scope => scope.Db.Notifications
            .AsNoTracking()
            .Where(n => n.Id == notificationId)
            .Select(n => n.ReadAt)
            .FirstAsync());

        // The phone retries a tap on a flaky connection, so this arrives twice
        // in practice. The second one must not move the timestamp.
        await InScope(scope =>
        {
            ActAs(scope, user);
            return scope.Send(new MarkNotificationReadCommand(notificationId));
        });

        var secondReadAt = await InScope(scope => scope.Db.Notifications
            .AsNoTracking()
            .Where(n => n.Id == notificationId)
            .Select(n => n.ReadAt)
            .FirstAsync());

        Assert.Equal(firstReadAt, secondReadAt);
    }

    // ---- what the API refuses -------------------------------------------

    [Fact]
    public async Task An_announcement_needs_a_subject_and_a_body()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await Assert.ThrowsAsync<ValidationException>(() => InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new SendAnnouncementCommand { Title = "   ", Body = "Telo." });
        }));

        await Assert.ThrowsAsync<ValidationException>(() => InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new SendAnnouncementCommand { Title = Subject(), Body = "" });
        }));
    }

    [Fact]
    public async Task An_audience_role_outside_the_enum_is_refused()
    {
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));

        await Assert.ThrowsAsync<ValidationException>(() => InScope(scope =>
        {
            ActAs(scope, admin);
            return scope.Send(new SendAnnouncementCommand
            {
                Title = Subject(),
                Body = "Telo.",
                Role = (UserRole)42
            });
        }));
    }
}
