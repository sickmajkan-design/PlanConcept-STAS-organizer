using Construction.Application.Features.DocumentRequests.Commands.SendDocumentRequestReminders;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The deadline of a request for a document: the person hears the day before and on the day, the office
/// hears once that it passed, and nobody hears twice or about what no longer waits on the person.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class DocumentRequestReminderTests : IntegrationTestBase
{
    public DocumentRequestReminderTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record World(Employee Employee, User Worker, User Admin);

    private async Task<World> SeedAsync()
    {
        var employee = await InScope(scope => TestData.SeedEmployeeAsync(scope));
        var admin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Admin));
        var worker = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.Worker, employee.Id));

        return new World(employee, worker, admin);
    }

    private Task<DocumentRequest> AskAsync(World w, DateOnly? due, DocumentRequestStatus status = DocumentRequestStatus.Open) =>
        InScope(async scope =>
        {
            var request = new DocumentRequest
            {
                EmployeeId = w.Employee.Id,
                RequestedByUserId = w.Admin.Id,
                Category = AttachmentCategory.MedicalCheck,
                Title = "Ljekarski pregled",
                DueDate = due,
                Status = status
            };

            scope.Db.DocumentRequests.Add(request);
            await scope.Db.SaveChangesAsync();

            return request;
        });

    private Task Sweep() => InScope(scope => scope.Send(new SendDocumentRequestRemindersCommand()));

    private Task<List<Notification>> Heard(User user, NotificationType type, Guid requestId) =>
        InScope(async scope => (await scope.Db.Notifications
                .Where(n => n.UserId == user.Id && n.Type == type)
                .ToListAsync())
            .Where(n => n.DataJson != null && n.DataJson.Contains(requestId.ToString()))
            .ToList());

    [Fact]
    public async Task The_person_is_told_the_day_before_and_only_once()
    {
        var w = await SeedAsync();
        var request = await AskAsync(w, Today.AddDays(1));

        await Sweep();
        await Sweep();

        var notice = Assert.Single(await Heard(w.Worker, NotificationType.DocumentRequestDue, request.Id));
        Assert.Contains("tomorrow", notice.DataJson);
        // The office is not nagged before anything is late.
        Assert.Empty(await Heard(w.Admin, NotificationType.DocumentRequestOverdue, request.Id));
    }

    [Fact]
    public async Task The_person_is_told_again_on_the_day()
    {
        var w = await SeedAsync();
        var request = await AskAsync(w, Today);

        await Sweep();

        var notice = Assert.Single(await Heard(w.Worker, NotificationType.DocumentRequestDue, request.Id));
        Assert.Contains("today", notice.DataJson);
    }

    [Fact]
    public async Task A_deadline_further_off_or_none_says_nothing()
    {
        var w = await SeedAsync();
        var far = await AskAsync(w, Today.AddDays(5));
        var none = await AskAsync(w, null);

        await Sweep();

        Assert.Empty(await Heard(w.Worker, NotificationType.DocumentRequestDue, far.Id));
        Assert.Empty(await Heard(w.Worker, NotificationType.DocumentRequestDue, none.Id));
    }

    [Theory]
    [InlineData(DocumentRequestStatus.Submitted)]
    [InlineData(DocumentRequestStatus.Accepted)]
    [InlineData(DocumentRequestStatus.Cancelled)]
    public async Task A_request_that_no_longer_waits_on_the_person_is_left_alone(DocumentRequestStatus status)
    {
        var w = await SeedAsync();
        var request = await AskAsync(w, Today, status);

        await Sweep();

        Assert.Empty(await Heard(w.Worker, NotificationType.DocumentRequestDue, request.Id));
        Assert.Empty(await Heard(w.Admin, NotificationType.DocumentRequestOverdue, request.Id));
    }

    [Fact]
    public async Task A_request_sent_back_still_counts_as_waiting_on_the_person()
    {
        var w = await SeedAsync();
        var request = await AskAsync(w, Today, DocumentRequestStatus.Returned);

        await Sweep();

        Assert.Single(await Heard(w.Worker, NotificationType.DocumentRequestDue, request.Id));
    }

    [Fact]
    public async Task The_office_is_told_once_that_the_deadline_passed_and_the_person_is_not()
    {
        var w = await SeedAsync();
        var request = await AskAsync(w, Today.AddDays(-2));

        await Sweep();
        await Sweep();

        var notice = Assert.Single(await Heard(w.Admin, NotificationType.DocumentRequestOverdue, request.Id));
        Assert.Contains(w.Employee.LastName, notice.DataJson);
        Assert.Empty(await Heard(w.Worker, NotificationType.DocumentRequestDue, request.Id));
    }

    [Fact]
    public async Task A_new_deadline_earns_a_fresh_reminder()
    {
        var w = await SeedAsync();
        var request = await AskAsync(w, Today.AddDays(-1));

        await Sweep();
        Assert.Single(await Heard(w.Admin, NotificationType.DocumentRequestOverdue, request.Id));

        // The office gives more time, and that time too runs out: the earlier claim must not hush it.
        await InScope(async scope =>
        {
            var stored = await scope.Db.DocumentRequests.SingleAsync(r => r.Id == request.Id);
            stored.DueDate = Today.AddDays(-3);
            await scope.Db.SaveChangesAsync();
        });

        await Sweep();

        Assert.Equal(2, (await Heard(w.Admin, NotificationType.DocumentRequestOverdue, request.Id)).Count);
    }
}
