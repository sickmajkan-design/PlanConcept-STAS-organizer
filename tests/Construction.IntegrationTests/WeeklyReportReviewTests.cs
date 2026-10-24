using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// A foreman sends the week's report; the office is told, looks at it, and either approves it or sends it back
/// with a reason, and the foreman answers by sending it again.
/// </summary>
[Collection(ApiCollection.Name)]
public class WeeklyReportReviewTests
{
    private readonly ApiFixture _api;

    public WeeklyReportReviewTests(ApiFixture api)
    {
        _api = api;
    }

    private async Task<(Guid ProjectId, string ProjectName)> SiteOfTheForemanAsync()
    {
        var foremanId = _api.UserIds[UserRole.Foreman];

        return await _api.InScope(async db =>
        {
            var employeeId = await db.Users.Where(u => u.Id == foremanId).Select(u => u.EmployeeId!.Value).SingleAsync();
            var project = new Project { Name = $"Weekly {Guid.NewGuid():N}"[..16], Status = ProjectStatus.Active };

            db.Projects.Add(project);
            await db.SaveChangesAsync();

            db.EmployeeProjects.Add(new EmployeeProject
            {
                EmployeeId = employeeId,
                ProjectId = project.Id,
                StartDate = new DateOnly(2020, 1, 1),
            });
            await db.SaveChangesAsync();

            return (project.Id, project.Name);
        });
    }

    private static MultipartFormDataContent Report(Guid projectId, int week, string note = "ok")
    {
        var sheet = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0xFF, 0xD9]);
        sheet.Headers.ContentType = new("image/jpeg");

        return new MultipartFormDataContent
        {
            { new StringContent(projectId.ToString()), "projectId" },
            { new StringContent("2026"), "isoYear" },
            { new StringContent(week.ToString()), "isoWeek" },
            { new StringContent("SignedHours"), "type" },
            { new StringContent("212.5"), "quantity" },
            { new StringContent(note), "note" },
            { sheet, "file", "satnica.jpg" },
        };
    }

    private async Task<JsonElement> SendAsync(HttpClient foreman, Guid projectId, int week = 41)
    {
        var response = await foreman.PostAsync("/api/v1/weeklysitereports", Report(projectId, week));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private Task<int> NoticesAsync(Guid userId, NotificationType type, Guid projectId) =>
        _api.InScope(async db => (await db.Notifications
                .Where(n => n.UserId == userId && n.Type == type)
                .ToListAsync())
            .Count(n => n.DataJson != null && n.DataJson.Contains(projectId.ToString())));

    [Fact]
    public async Task The_office_is_told_when_a_report_arrives_and_the_foreman_is_not()
    {
        var (projectId, _) = await SiteOfTheForemanAsync();
        using var foreman = _api.ClientAs(UserRole.Foreman);

        await SendAsync(foreman, projectId);

        Assert.Equal(1, await NoticesAsync(_api.UserIds[UserRole.Admin], NotificationType.WeeklyReportSubmitted, projectId));
        Assert.Equal(1, await NoticesAsync(_api.UserIds[UserRole.SuperAdmin], NotificationType.WeeklyReportSubmitted, projectId));
        Assert.Equal(0, await NoticesAsync(_api.UserIds[UserRole.Foreman], NotificationType.WeeklyReportSubmitted, projectId));
    }

    [Fact]
    public async Task Approving_marks_the_report_and_tells_the_foreman()
    {
        var (projectId, _) = await SiteOfTheForemanAsync();
        using var foreman = _api.ClientAs(UserRole.Foreman);
        using var office = _api.ClientAs(UserRole.Admin);
        var report = await SendAsync(foreman, projectId);
        var id = report.GetProperty("id").GetGuid();

        var approved = await office.PostAsync($"/api/v1/weeklysitereports/{id}/approve", null);

        Assert.Equal(HttpStatusCode.NoContent, approved.StatusCode);
        var stored = await _api.InScope(db => db.WeeklySiteReports.SingleAsync(r => r.Id == id));
        Assert.Equal(WeeklyReportStatus.Processed, stored.Status);
        Assert.Equal(1, await NoticesAsync(_api.UserIds[UserRole.Foreman], NotificationType.WeeklyReportApproved, projectId));
    }

    [Fact]
    public async Task Sending_back_needs_a_reason_and_tells_the_foreman_what_it_is()
    {
        var (projectId, _) = await SiteOfTheForemanAsync();
        using var foreman = _api.ClientAs(UserRole.Foreman);
        using var office = _api.ClientAs(UserRole.Admin);
        var id = (await SendAsync(foreman, projectId)).GetProperty("id").GetGuid();

        var withoutReason = await office.PostAsJsonAsync($"/api/v1/weeklysitereports/{id}/request-correction", new { note = "  " });
        Assert.Equal(HttpStatusCode.BadRequest, withoutReason.StatusCode);

        var sentBack = await office.PostAsJsonAsync(
            $"/api/v1/weeklysitereports/{id}/request-correction", new { note = "Nedostaje potpis kupca" });
        Assert.Equal(HttpStatusCode.NoContent, sentBack.StatusCode);

        var stored = await _api.InScope(db => db.WeeklySiteReports.SingleAsync(r => r.Id == id));
        Assert.Equal(WeeklyReportStatus.CorrectionRequested, stored.Status);
        Assert.Equal("Nedostaje potpis kupca", stored.ReviewNote);

        var notice = await _api.InScope(async db => (await db.Notifications
                .Where(n => n.UserId == _api.UserIds[UserRole.Foreman] && n.Type == NotificationType.WeeklyReportCorrection)
                .ToListAsync())
            .Single(n => n.DataJson!.Contains(projectId.ToString())));
        Assert.Contains("Nedostaje potpis kupca", notice.DataJson);
    }

    [Fact]
    public async Task Sending_the_report_again_replaces_the_one_sent_back()
    {
        var (projectId, _) = await SiteOfTheForemanAsync();
        using var foreman = _api.ClientAs(UserRole.Foreman);
        using var office = _api.ClientAs(UserRole.Admin);
        var id = (await SendAsync(foreman, projectId)).GetProperty("id").GetGuid();

        (await office.PostAsJsonAsync($"/api/v1/weeklysitereports/{id}/request-correction", new { note = "Pogrešni sati" }))
            .EnsureSuccessStatusCode();

        var again = await SendAsync(foreman, projectId);

        Assert.Equal(id, again.GetProperty("id").GetGuid());
        var rows = await _api.InScope(db => db.WeeklySiteReports.Where(r => r.ProjectId == projectId).ToListAsync());
        var only = Assert.Single(rows);
        Assert.Equal(WeeklyReportStatus.Submitted, only.Status);
        Assert.Null(only.ReviewNote);
        // The office hears about the new one as well as the first.
        Assert.Equal(2, await NoticesAsync(_api.UserIds[UserRole.Admin], NotificationType.WeeklyReportSubmitted, projectId));
    }

    [Fact]
    public async Task A_foreman_cannot_approve_send_back_or_read_the_summary()
    {
        var (projectId, _) = await SiteOfTheForemanAsync();
        using var foreman = _api.ClientAs(UserRole.Foreman);
        var id = (await SendAsync(foreman, projectId)).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await foreman.PostAsync($"/api/v1/weeklysitereports/{id}/approve", null)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await foreman.PostAsJsonAsync($"/api/v1/weeklysitereports/{id}/request-correction", new { note = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await foreman.GetAsync("/api/v1/weeklysitereports/summary")).StatusCode);
    }

    [Fact]
    public async Task The_summary_counts_what_waits_for_the_office()
    {
        var (projectId, _) = await SiteOfTheForemanAsync();
        using var foreman = _api.ClientAs(UserRole.Foreman);
        using var office = _api.ClientAs(UserRole.Admin);
        await SendAsync(foreman, projectId);

        var summary = await office.GetFromJsonAsync<JsonElement>("/api/v1/weeklysitereports/summary");

        Assert.True(summary.GetProperty("awaitingReview").GetInt32() >= 1);
    }
}
