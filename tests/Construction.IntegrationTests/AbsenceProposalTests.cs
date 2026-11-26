using System.Net;
using System.Net.Http.Json;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Management offers other dates for annual leave that is still waiting; the worker answers.
/// </summary>
[Collection(ApiCollection.Name)]
public class AbsenceProposalTests
{
    private readonly ApiFixture _api;

    public AbsenceProposalTests(ApiFixture api)
    {
        _api = api;
    }

    private async Task<Guid> SeedPendingLeaveAsync()
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(40);

        return await _api.InScope(async context =>
        {
            var employeeId = await context.Users
                .Where(u => u.Id == _api.UserIds[UserRole.Worker])
                .Select(u => u.EmployeeId!.Value)
                .FirstAsync();

            var absence = new Absence
            {
                EmployeeId = employeeId,
                Type = AbsenceType.AnnualLeave,
                Status = AbsenceStatus.Requested,
                StartDate = start,
                EndDate = start.AddDays(4),
            };

            context.Absences.Add(absence);
            await context.SaveChangesAsync();

            return absence.Id;
        });
    }

    [Fact]
    public async Task Another_date_can_be_offered_for_a_waiting_request_and_accepting_moves_the_dates_only()
    {
        var id = await SeedPendingLeaveAsync();
        var newStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60);
        using var admin = _api.ClientAs(UserRole.Admin);
        using var worker = _api.ClientAs(UserRole.Worker);

        var proposed = await admin.PostAsJsonAsync($"/api/v1/absences/{id}/propose-edit", new
        {
            startDate = newStart,
            endDate = newStart.AddDays(4),
            reason = "Plan clash",
        });
        Assert.Equal(HttpStatusCode.OK, proposed.StatusCode);

        var accepted = await worker.PostAsJsonAsync($"/api/v1/absences/{id}/confirm-edit", new { approve = true });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        var absence = await _api.InScope(c => c.Absences.AsNoTracking().FirstAsync(a => a.Id == id));
        Assert.Equal(newStart, absence.StartDate);
        // Granting stays the office's own decision.
        Assert.Equal(AbsenceStatus.Requested, absence.Status);
        // The text sent along was a message to the worker, so the worker's own reason is not replaced.
        Assert.Null(absence.Reason);
        Assert.False(absence.HasPendingEdit);
    }

    [Fact]
    public async Task Deciding_the_request_drops_an_offer_that_was_still_open()
    {
        var id = await SeedPendingLeaveAsync();
        var newStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60);
        using var admin = _api.ClientAs(UserRole.Admin);

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/v1/absences/{id}/propose-edit", new
        {
            startDate = newStart,
            endDate = newStart.AddDays(4),
        })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync(
            $"/api/v1/absences/{id}/review", new { approve = false, note = "No" })).StatusCode);

        var absence = await _api.InScope(c => c.Absences.AsNoTracking().FirstAsync(a => a.Id == id));
        Assert.False(absence.HasPendingEdit);
    }

    [Theory]
    [InlineData(UserRole.Foreman)]
    [InlineData(UserRole.ProjectManager)]
    public async Task Someone_who_cannot_review_leave_cannot_offer_dates_on_somebody_elses_request(UserRole role)
    {
        var id = await SeedPendingLeaveAsync();
        var newStart = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(60);
        using var client = _api.ClientAs(role);

        var response = await client.PostAsJsonAsync($"/api/v1/absences/{id}/propose-edit", new
        {
            startDate = newStart,
            endDate = newStart.AddDays(4),
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
