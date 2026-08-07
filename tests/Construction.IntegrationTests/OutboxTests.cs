using System.Text.Json;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Authentication.Commands.ForgotPassword;
using Construction.Application.Features.Outbox;
using Construction.Application.Features.Outbox.Commands.ProcessOutbox;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// The queue that took email and push out of the request path.
/// </summary>
/// <remarks>
/// Against PostgreSQL because the two properties that matter are properties of
/// the database, not of the C#: that a message commits in the same transaction
/// as the work that caused it, and that two workers running at the same moment
/// cannot both take the same message. Neither can be demonstrated in memory.
/// </remarks>
[Collection(DatabaseCollection.Name)]
public class OutboxTests : IntegrationTestBase
{
    public OutboxTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    /// <summary>
    /// A subject unique to one test, so a query for it finds that test's own
    /// message in a database every class shares.
    /// </summary>
    private static string Subject() => $"subject-{Guid.NewGuid():N}";

    /// <summary>
    /// Finds this test's message by looking inside the payload.
    /// </summary>
    /// <remarks>
    /// Filtered in memory rather than in SQL. The column is <c>jsonb</c>, and
    /// <c>LIKE</c> has no operator for it — a server-side <c>Contains</c>
    /// fails with "operator does not exist: jsonb ~~ jsonb". Reaching into a
    /// payload is a test's business anyway; the processor only ever selects on
    /// the columns beside it.
    /// </remarks>
    private async Task<OutboxMessage?> FindAsync(string subject)
    {
        var messages = await InScope(scope =>
            scope.Db.OutboxMessages.AsNoTracking().ToListAsync());

        return messages.SingleOrDefault(m => m.PayloadJson.Contains(subject));
    }

    private async Task<OutboxMessage> EnqueueEmailAsync(string subject, string to = "who@example.test")
    {
        await InScope(async scope =>
        {
            scope.Outbox.Enqueue(new EmailPayload(to, subject, "<p>body</p>"));
            await scope.Db.SaveChangesAsync();
        });

        return (await FindAsync(subject))!;
    }

    /// <summary>
    /// A sweep narrow enough to leave other classes' messages alone.
    /// </summary>
    /// <remarks>
    /// It cannot be narrowed by subject — the processor takes whatever is due
    /// — so the tests below assert on their own message rather than on the
    /// run's totals, except where the total is genuinely theirs.
    /// </remarks>
    private static ProcessOutboxCommand Sweep(int maxAttempts = 6) =>
        new() { BatchSize = 100, MaxAttempts = maxAttempts };

    // ---- enqueuing --------------------------------------------------------

    [Fact]
    public async Task Asking_for_a_password_reset_queues_the_email_instead_of_sending_it()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        await InScope(scope => scope.Send(new ForgotPasswordCommand { Email = user.Email }));

        // The request is finished and nothing has touched SMTP. This is the
        // whole point: it used to wait on a mail server whose default timeout
        // is two minutes, on an endpoint that anyone can call unauthenticated.
        var sentSoFar = await InScope(scope => Task.FromResult(scope.Emails.Sent.Count));

        Assert.Equal(0, sentSoFar);

        var emails = await InScope(scope => scope.Db.OutboxMessages
            .AsNoTracking()
            .Where(m => m.Type == OutboxMessageType.Email)
            .ToListAsync());

        var message = Assert.Single(
            emails.Where(m => m.PayloadJson.Contains(user.Email)));

        Assert.True(message.IsPending);
        Assert.Equal(0, message.Attempts);
    }

    [Fact]
    public async Task The_reset_token_and_its_email_are_one_transaction()
    {
        // Not "both are written" but "both are written together". The token is
        // committed in the same SaveChanges as the message, so there is no
        // window in which somebody holds a valid token that nobody will ever
        // email them.
        var user = await InScope(scope => TestData.SeedUserAsync(scope));

        await InScope(scope => scope.Send(new ForgotPasswordCommand { Email = user.Email }));

        var tokens = await InScope(scope =>
            scope.Db.PasswordResetTokens.CountAsync(t => t.UserId == user.Id));

        var all = await InScope(scope =>
            scope.Db.OutboxMessages.AsNoTracking().ToListAsync());

        Assert.Equal(1, tokens);
        Assert.Single(all.Where(m => m.PayloadJson.Contains(user.Email)));
    }

    [Fact]
    public async Task An_unknown_address_queues_nothing()
    {
        var before = await InScope(scope => scope.Db.OutboxMessages.CountAsync());

        await InScope(scope => scope.Send(
            new ForgotPasswordCommand { Email = "nobody-here@example.test" }));

        var after = await InScope(scope => scope.Db.OutboxMessages.CountAsync());

        // The endpoint must not reveal whether an address is registered, and a
        // queue that grew would be a way of asking.
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task A_handler_that_fails_before_saving_queues_nothing()
    {
        var subject = Subject();

        await InScope(scope =>
        {
            scope.Outbox.Enqueue(new EmailPayload("who@example.test", subject, "<p>body</p>"));

            // Never saved. The message belongs to the caller's unit of work,
            // so an operation that rolls back takes its messages with it —
            // which is right, because the thing they were about did not happen.
            return Task.CompletedTask;
        });

        Assert.Null(await FindAsync(subject));
    }

    // ---- sending ----------------------------------------------------------

    [Fact]
    public async Task A_queued_email_is_sent_and_marked_sent()
    {
        var subject = Subject();

        await EnqueueEmailAsync(subject, to: "recipient@example.test");

        await InScope(scope => scope.Send(Sweep()));

        var sent = await InScope(scope => Task.FromResult(scope.Emails.Sent));

        Assert.Contains(sent, email => email.Subject == subject
            && email.To == "recipient@example.test");

        var message = await FindAsync(subject);

        Assert.NotNull(message!.SentAt);
        Assert.Null(message.AbandonedAt);
        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public async Task A_sent_message_is_not_sent_again()
    {
        var subject = Subject();

        await EnqueueEmailAsync(subject);

        await InScope(scope => scope.Send(Sweep()));
        await InScope(scope => scope.Send(Sweep()));

        var sent = await InScope(scope => Task.FromResult(scope.Emails.Sent));

        Assert.Single(sent.Where(email => email.Subject == subject));
    }

    [Fact]
    public async Task A_push_resolves_its_recipients_devices_when_it_is_sent()
    {
        var (user, token) = await InScope(async scope =>
        {
            var seeded = await TestData.SeedUserAsync(scope);
            var deviceToken = $"device-{Guid.NewGuid():N}";

            scope.Db.DeviceTokens.Add(new DeviceToken
            {
                UserId = seeded.Id,
                Token = deviceToken,
                Platform = DevicePlatform.Android,
                LastUsedAt = DateTime.UtcNow,
            });

            await scope.Db.SaveChangesAsync();

            return (User: seeded, Token: deviceToken);
        });

        var title = Subject();

        await InScope(async scope =>
        {
            scope.Outbox.Enqueue(new PushPayload(
                [user.Id],
                NotificationType.GeneralAnnouncement,
                title,
                "body",
                new Dictionary<string, string>()));

            await scope.Db.SaveChangesAsync();
        });

        await InScope(scope => scope.Send(Sweep()));

        var pushes = await InScope(scope => Task.FromResult(scope.Pushes.Sent));
        var push = Assert.Single(pushes.Where(p => p.Title == title));

        // Resolved at send time, not frozen at enqueue time: on a retry an
        // hour later a stored token list could be devices that no longer exist.
        Assert.Contains(token, push.Tokens);
    }

    [Fact]
    public async Task A_push_to_somebody_with_no_device_counts_as_delivered()
    {
        var user = await InScope(scope => TestData.SeedUserAsync(scope));
        var title = Subject();

        await InScope(async scope =>
        {
            scope.Outbox.Enqueue(new PushPayload(
                [user.Id],
                NotificationType.GeneralAnnouncement,
                title,
                "body",
                new Dictionary<string, string>()));

            await scope.Db.SaveChangesAsync();
        });

        await InScope(scope => scope.Send(Sweep()));

        var message = await FindAsync(title);

        // There is nothing to retry into. The inbox row was written when the
        // notification was raised, so the person still sees it when they open
        // the app; retrying a push at nobody would just fill the log.
        Assert.NotNull(message!.SentAt);
    }

    // ---- failure ----------------------------------------------------------

    [Fact]
    public async Task A_failed_send_is_retried_later_rather_than_lost()
    {
        var subject = Subject();

        await EnqueueEmailAsync(subject);

        await InScope(async scope =>
        {
            scope.Emails.FailWith = new InvalidOperationException("smtp is down");
            await scope.Send(Sweep());
        });

        var message = await FindAsync(subject);

        Assert.Null(message!.SentAt);
        Assert.Null(message.AbandonedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Equal("smtp is down", message.LastError);

        // Backed off rather than retried at once: the failure is almost always
        // a service that is down, and hammering it while it restarts turns a
        // brief outage into a longer one.
        Assert.True(message.NextAttemptAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task A_message_backing_off_is_not_picked_up_by_the_next_pass()
    {
        var subject = Subject();

        await EnqueueEmailAsync(subject);

        await InScope(async scope =>
        {
            scope.Emails.FailWith = new InvalidOperationException("smtp is down");
            await scope.Send(Sweep());
        });

        // Sender working again, but the message is not due yet.
        await InScope(scope => scope.Send(Sweep()));

        var message = await FindAsync(subject);

        Assert.Equal(1, message!.Attempts);
        Assert.Null(message.SentAt);
    }

    [Fact]
    public async Task It_recovers_once_the_service_comes_back()
    {
        var subject = Subject();

        var queued = await EnqueueEmailAsync(subject);

        await InScope(async scope =>
        {
            scope.Emails.FailWith = new InvalidOperationException("smtp is down");
            await scope.Send(Sweep());
        });

        // Bring the backoff forward rather than waiting half a minute for it.
        await InScope(async scope =>
        {
            await scope.Db.OutboxMessages
                .Where(m => m.Id == queued.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.NextAttemptAt, DateTime.UtcNow));
        });

        await InScope(async scope =>
        {
            // The sender is a singleton on the fixture, so the failure set
            // above outlives the scope that set it. Clearing it here is the
            // service coming back up.
            scope.Emails.FailWith = null;
            await scope.Send(Sweep());
        });

        var message = await FindAsync(subject);

        Assert.NotNull(message!.SentAt);
        Assert.Equal(2, message.Attempts);

        // The error from the attempt that failed is cleared, so a row with a
        // message on it always means the latest attempt failed.
        Assert.Null(message.LastError);
    }

    [Fact]
    public async Task It_gives_up_after_the_attempt_limit()
    {
        var subject = Subject();

        var queued = await EnqueueEmailAsync(subject);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            await InScope(async scope =>
            {
                scope.Emails.FailWith = new InvalidOperationException("no such mailbox");
                await scope.Send(Sweep(maxAttempts: 2));
            });

            await InScope(scope => scope.Db.OutboxMessages
                .Where(m => m.Id == queued.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.NextAttemptAt, DateTime.UtcNow)));
        }

        var message = await FindAsync(subject);

        // A permanently wrong address must stop somewhere, or it is retried
        // until the end of time and the log fills with it.
        Assert.NotNull(message!.AbandonedAt);
        Assert.Null(message.SentAt);
        Assert.Equal(2, message.Attempts);
    }

    [Fact]
    public async Task An_abandoned_message_is_never_picked_up_again()
    {
        var subject = Subject();

        var queued = await EnqueueEmailAsync(subject);

        await InScope(scope => scope.Db.OutboxMessages
            .Where(m => m.Id == queued.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.AbandonedAt, DateTime.UtcNow)
                .SetProperty(m => m.NextAttemptAt, DateTime.UtcNow.AddDays(-1))));

        await InScope(scope => scope.Send(Sweep()));

        var sent = await InScope(scope => Task.FromResult(scope.Emails.Sent));

        Assert.DoesNotContain(sent, email => email.Subject == subject);
    }

    // ---- concurrency ------------------------------------------------------

    [Fact]
    public async Task A_second_worker_starting_mid_send_does_not_take_the_same_message()
    {
        // The interleaving the claim lease exists for, produced deliberately
        // rather than hoped for: the second sweep runs from inside the first
        // one's send, which is exactly the window in which a message has been
        // claimed but not yet marked sent.
        //
        // Claiming pushed NextAttemptAt five minutes out, so the message is no
        // longer due and the second worker finds nothing. Without that — if
        // claiming only stamped a token — the second worker would claim the
        // same row and send a second copy of somebody's password-reset email.
        var subject = Subject();

        await EnqueueEmailAsync(subject);

        await InScope(async scope =>
        {
            scope.Emails.OnSend = () => InScope(inner => inner.Send(Sweep()));

            await scope.Send(Sweep());
        });

        var sent = await InScope(scope => Task.FromResult(scope.Emails.Sent));

        Assert.Single(sent.Where(email => email.Subject == subject));
    }

    [Fact]
    public async Task Two_workers_running_at_once_never_send_the_same_message_twice()
    {
        // The same property under ordinary contention rather than a contrived
        // interleaving. Weaker than the test above — two sweeps started
        // together may not overlap at all — but it is the shape production
        // actually has, with two replicas on the same timer.
        var subjects = Enumerable.Range(0, 20).Select(_ => Subject()).ToList();

        foreach (var subject in subjects)
        {
            await EnqueueEmailAsync(subject);
        }

        var first = InScope(scope => scope.Send(Sweep()));
        var second = InScope(scope => scope.Send(Sweep()));

        await Task.WhenAll(first, second);

        var sent = await InScope(scope => Task.FromResult(scope.Emails.Sent));

        foreach (var subject in subjects)
        {
            // Exactly once. Twice would be two password-reset emails for one
            // request, or the same announcement on somebody's phone twice.
            Assert.Single(sent.Where(email => email.Subject == subject));
        }
    }

    [Fact]
    public async Task A_claimed_message_comes_back_when_its_lease_expires()
    {
        // A worker that claims messages and then dies must not strand them.
        // Nothing has to notice: the claim moved NextAttemptAt forward, so the
        // message becomes due again by itself.
        var subject = Subject();

        var queued = await EnqueueEmailAsync(subject);

        await InScope(scope => scope.Db.OutboxMessages
            .Where(m => m.Id == queued.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.ClaimId, Guid.NewGuid())
                .SetProperty(m => m.Attempts, 1)
                .SetProperty(m => m.NextAttemptAt, DateTime.UtcNow.AddMinutes(5))));

        await InScope(scope => scope.Send(Sweep()));

        var stillWaiting = await FindAsync(subject);
        Assert.Null(stillWaiting!.SentAt);

        // Lease expired.
        await InScope(scope => scope.Db.OutboxMessages
            .Where(m => m.Id == queued.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.NextAttemptAt, DateTime.UtcNow)));

        await InScope(scope => scope.Send(Sweep()));

        var recovered = await FindAsync(subject);
        Assert.NotNull(recovered!.SentAt);
    }

    // ---- what it refuses --------------------------------------------------

    [Fact]
    public async Task A_lease_shorter_than_a_send_is_refused()
    {
        await Assert.ThrowsAsync<Construction.Application.Common.Exceptions.ValidationException>(
            () => InScope(scope => scope.Send(new ProcessOutboxCommand
            {
                ClaimLease = TimeSpan.FromSeconds(1),
            })));
    }

    [Fact]
    public async Task The_payload_survives_a_round_trip_through_the_column()
    {
        var subject = Subject();

        await EnqueueEmailAsync(subject, to: "Ivan.Horvat+test@example.test");

        var message = await FindAsync(subject);

        var payload = JsonSerializer.Deserialize<EmailPayload>(message!.PayloadJson);

        Assert.NotNull(payload);
        Assert.Equal("Ivan.Horvat+test@example.test", payload.To);
        Assert.Equal(subject, payload.Subject);
    }
}
