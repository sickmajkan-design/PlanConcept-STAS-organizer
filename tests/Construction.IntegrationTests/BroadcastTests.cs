using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// An announcement goes to the people several choices name, each once; a record of it is kept so the sender sees who has
/// read and confirmed it, can remind those who have not, and the record goes after twelve months.
/// </summary>
[Collection(ApiCollection.Name)]
public class BroadcastTests
{
    private readonly ApiFixture _api;

    public BroadcastTests(ApiFixture api)
    {
        _api = api;
    }

    private sealed record Crew(Project Project, Employee Anna, Employee Boris, Employee Cvijeta, User AnnaUser, User BorisUser, NotificationGroup Group);

    /// <summary>
    /// Anna and Boris are posted to the site and have accounts; Cvijeta is posted to it and has none. Anna and Cvijeta are
    /// also in a group, so a choice of the site plus the group names Anna twice.
    /// </summary>
    private async Task<Crew> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        return await _api.InScope(async db =>
        {
            var project = new Project { Name = $"Hala {suffix}", Status = ProjectStatus.Active };
            Employee Make(string first) => new()
            {
                EmployeeNumber = $"B-{Guid.NewGuid():N}"[..14],
                FirstName = first,
                LastName = $"Test{suffix}",
                Position = "Monter",
                EmploymentDate = new DateOnly(2020, 1, 1),
                Status = EmployeeStatus.Active,
            };

            var anna = Make("Anna");
            var boris = Make("Boris");
            var cvijeta = Make("Cvijeta");
            db.Projects.Add(project);
            db.Employees.AddRange(anna, boris, cvijeta);
            await db.SaveChangesAsync();

            foreach (var e in new[] { anna, boris, cvijeta })
            {
                db.EmployeeProjects.Add(new EmployeeProject { EmployeeId = e.Id, ProjectId = project.Id, StartDate = new DateOnly(2020, 1, 1) });
            }

            var annaUser = new User { Email = $"anna-{suffix}@b.test", PasswordHash = "x", Role = UserRole.Worker, IsActive = true, EmployeeId = anna.Id };
            var borisUser = new User { Email = $"boris-{suffix}@b.test", PasswordHash = "x", Role = UserRole.Worker, IsActive = true, EmployeeId = boris.Id };
            db.Users.AddRange(annaUser, borisUser);

            var group = new NotificationGroup { Name = $"Betoniranje {suffix}" };
            group.Members.Add(new NotificationGroupMember { EmployeeId = anna.Id });
            group.Members.Add(new NotificationGroupMember { EmployeeId = cvijeta.Id });
            db.NotificationGroups.Add(group);
            await db.SaveChangesAsync();

            return new Crew(project, anna, boris, cvijeta, annaUser, borisUser, group);
        });
    }

    private static object Targets(params object[] targets) => new { targets };

    private async Task<JsonElement> SendAsync(Crew c, bool requiresAck = false, string title = "Sutra u 6:30")
    {
        using var office = _api.ClientAs(UserRole.Admin);
        var response = await office.PostAsJsonAsync("/api/v1/notifications/broadcasts", new
        {
            title,
            body = "Ispred kontejnera.",
            requiresAcknowledgment = requiresAck,
            targets = new object[]
            {
                new { kind = 4, id = c.Project.Id },
                new { kind = 3, id = c.Group.Id },
            },
        });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Several_choices_name_each_person_once_and_those_without_an_account_are_shown_as_such()
    {
        var c = await SeedAsync();
        using var office = _api.ClientAs(UserRole.Admin);

        var response = await office.PostAsJsonAsync("/api/v1/notifications/audience-preview", new
        {
            targets = new object[] { new { kind = 4, id = c.Project.Id }, new { kind = 3, id = c.Group.Id }, new { kind = 2, id = c.Anna.Id } },
        });
        var preview = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Anna is reached three ways and counted once; Cvijeta has no account.
        Assert.Equal(3, preview.GetProperty("total").GetInt32());
        Assert.Equal(2, preview.GetProperty("reachable").GetInt32());
        Assert.Equal(1, preview.GetProperty("withoutAccount").GetInt32());

        var anna = preview.GetProperty("people").EnumerateArray().Single(p => p.GetProperty("employeeId").GetGuid() == c.Anna.Id);
        Assert.Equal(3, anna.GetProperty("via").GetArrayLength());
        var cvijeta = preview.GetProperty("people").EnumerateArray().Single(p => p.GetProperty("employeeId").GetGuid() == c.Cvijeta.Id);
        Assert.False(cvijeta.GetProperty("hasAccount").GetBoolean());
        Assert.Equal(3, preview.GetProperty("chips").GetArrayLength());
    }

    [Fact]
    public async Task Sending_reaches_those_with_an_account_once_and_keeps_a_record_with_how_each_was_reached()
    {
        var c = await SeedAsync();

        var result = await SendAsync(c);

        Assert.Equal(2, result.GetProperty("recipients").GetInt32());
        Assert.Equal(1, result.GetProperty("withoutAccount").GetInt32());
        var id = result.GetProperty("broadcastId").GetGuid();

        var rows = await _api.InScope(db => db.Notifications.Where(n => n.BroadcastId == id).ToListAsync());
        Assert.Equal(2, rows.Count);
        Assert.All(rows, n => Assert.False(n.IsReminder));
        var anna = rows.Single(n => n.UserId == c.AnnaUser.Id);
        Assert.Contains("Projekat", anna.Via);
        Assert.Contains("Grupa", anna.Via);
        Assert.Equal("Projekat: " + c.Project.Name, rows.Single(n => n.UserId == c.BorisUser.Id).Via);
    }

    [Fact]
    public async Task The_list_and_the_detail_say_who_has_read_and_confirmed()
    {
        var c = await SeedAsync();
        var id = (await SendAsync(c, requiresAck: true)).GetProperty("broadcastId").GetGuid();

        await _api.InScope(async db =>
        {
            var annaRow = await db.Notifications.SingleAsync(n => n.BroadcastId == id && n.UserId == c.AnnaUser.Id);
            annaRow.IsRead = true;
            annaRow.ReadAt = DateTime.UtcNow;
            annaRow.AcknowledgedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return 0;
        });

        using var office = _api.ClientAs(UserRole.Admin);
        var list = await office.GetFromJsonAsync<JsonElement>("/api/v1/notifications/broadcasts?pageSize=50");
        var item = list.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == id);
        Assert.Equal(2, item.GetProperty("recipients").GetInt32());
        Assert.Equal(1, item.GetProperty("read").GetInt32());
        Assert.Equal(1, item.GetProperty("acknowledged").GetInt32());
        Assert.True(item.GetProperty("requiresAcknowledgment").GetBoolean());
        Assert.Equal(2, item.GetProperty("audience").GetArrayLength());

        var detail = await office.GetFromJsonAsync<JsonElement>($"/api/v1/notifications/broadcasts/{id}");
        var people = detail.GetProperty("recipients").EnumerateArray().ToList();
        Assert.Equal(2, people.Count);
        var anna = people.Single(p => p.GetProperty("userId").GetGuid() == c.AnnaUser.Id);
        Assert.True(anna.GetProperty("isRead").GetBoolean());
        Assert.NotEqual(JsonValueKind.Null, anna.GetProperty("acknowledgedAt").ValueKind);
        var boris = people.Single(p => p.GetProperty("userId").GetGuid() == c.BorisUser.Id);
        Assert.False(boris.GetProperty("isRead").GetBoolean());
        Assert.False(boris.GetProperty("hasDevice").GetBoolean());
    }

    [Fact]
    public async Task A_reminder_goes_only_to_those_who_have_not_read_and_not_twice_in_a_day()
    {
        var c = await SeedAsync();
        var id = (await SendAsync(c)).GetProperty("broadcastId").GetGuid();

        await _api.InScope(async db =>
        {
            var annaRow = await db.Notifications.SingleAsync(n => n.BroadcastId == id && n.UserId == c.AnnaUser.Id);
            annaRow.IsRead = true;
            await db.SaveChangesAsync();
            return 0;
        });

        using var office = _api.ClientAs(UserRole.Admin);
        var first = await office.PostAsync($"/api/v1/notifications/broadcasts/{id}/remind", null);
        Assert.Equal(1, await first.Content.ReadFromJsonAsync<int>());

        var again = await office.PostAsync($"/api/v1/notifications/broadcasts/{id}/remind", null);
        Assert.Equal(0, await again.Content.ReadFromJsonAsync<int>());

        var reminders = await _api.InScope(db => db.Notifications.Where(n => n.BroadcastId == id && n.IsReminder).ToListAsync());
        var reminder = Assert.Single(reminders);
        Assert.Equal(c.BorisUser.Id, reminder.UserId);
        Assert.StartsWith("Podsjetnik", reminder.Title);

        // The first delivery is not counted twice: the reminder is not a recipient.
        var detail = await office.GetFromJsonAsync<JsonElement>($"/api/v1/notifications/broadcasts/{id}");
        Assert.Equal(2, detail.GetProperty("recipients").GetArrayLength());
        var boris = detail.GetProperty("recipients").EnumerateArray().Single(p => p.GetProperty("userId").GetGuid() == c.BorisUser.Id);
        Assert.Equal(1, boris.GetProperty("reminders").GetInt32());
    }

    [Fact]
    public async Task A_confirmation_still_owed_is_reminded_after_the_notice_was_read()
    {
        var c = await SeedAsync();
        var id = (await SendAsync(c, requiresAck: true)).GetProperty("broadcastId").GetGuid();

        await _api.InScope(async db =>
        {
            foreach (var n in await db.Notifications.Where(n => n.BroadcastId == id).ToListAsync())
            {
                n.IsRead = true;
                if (n.UserId == c.AnnaUser.Id)
                {
                    n.AcknowledgedAt = DateTime.UtcNow;
                }
            }

            await db.SaveChangesAsync();
            return 0;
        });

        using var office = _api.ClientAs(UserRole.Admin);
        var sent = await (await office.PostAsync($"/api/v1/notifications/broadcasts/{id}/remind", null)).Content.ReadFromJsonAsync<int>();

        Assert.Equal(1, sent);
    }

    [Fact]
    public async Task An_audience_nobody_in_can_be_reached_is_refused()
    {
        var c = await SeedAsync();
        using var office = _api.ClientAs(UserRole.Admin);

        var response = await office.PostAsJsonAsync("/api/v1/notifications/broadcasts", new
        {
            title = "x",
            body = "y",
            targets = new object[] { new { kind = 2, id = c.Cvijeta.Id } },
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Without_choosing_anyone_nothing_is_sent()
    {
        using var office = _api.ClientAs(UserRole.Admin);

        var response = await office.PostAsJsonAsync("/api/v1/notifications/broadcasts", new { title = "x", body = "y", targets = Array.Empty<object>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Foreman)]
    [InlineData(UserRole.Worker)]
    public async Task Only_admins_send_and_see_the_history(UserRole role)
    {
        using var client = _api.ClientAs(role);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/notifications/broadcasts")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("/api/v1/notifications/audience-preview", Targets())).StatusCode);
    }
}
