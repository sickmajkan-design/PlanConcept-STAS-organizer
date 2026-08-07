using System.Text.Json;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Construction.Application.Features.Outbox.Commands.ProcessOutbox;

/// <summary>What one pass over the queue did.</summary>
public record OutboxRunResult(int Sent, int Retrying, int Abandoned)
{
    public int Handled => Sent + Retrying + Abandoned;
}

/// <summary>Sends the messages that are due.</summary>
public record ProcessOutboxCommand : IRequest<OutboxRunResult>
{
    public int BatchSize { get; init; } = 50;

    /// <summary>
    /// Attempts before a message is given up on.
    /// </summary>
    /// <remarks>
    /// With the backoff below, six attempts spread over roughly half an hour.
    /// Long enough to outlast a mail server restart; short enough that a
    /// permanently wrong address is not retried until the end of time.
    /// </remarks>
    public int MaxAttempts { get; init; } = 6;

    /// <summary>
    /// How long a claimed message stays claimed.
    /// </summary>
    /// <remarks>
    /// The lease is what makes a crashed worker harmless: it claimed some
    /// messages, died, and the messages become due again on their own rather
    /// than needing anyone to notice. It has to be longer than a send could
    /// plausibly take, or a slow SMTP server would let a second worker pick up
    /// a message the first is still sending.
    /// </remarks>
    public TimeSpan ClaimLease { get; init; } = TimeSpan.FromMinutes(5);
}

public class ProcessOutboxCommandValidator : AbstractValidator<ProcessOutboxCommand>
{
    public ProcessOutboxCommandValidator()
    {
        RuleFor(x => x.BatchSize).InclusiveBetween(1, 1_000);

        RuleFor(x => x.MaxAttempts).InclusiveBetween(1, 100);

        RuleFor(x => x.ClaimLease)
            .Must(lease => lease >= TimeSpan.FromSeconds(30))
            .WithMessage("The claim lease must be long enough for a send to finish.");
    }
}

public class ProcessOutboxCommandHandler
    : IRequestHandler<ProcessOutboxCommand, OutboxRunResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IPushSender _pushSender;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ProcessOutboxCommandHandler> _logger;

    public ProcessOutboxCommandHandler(
        IApplicationDbContext context,
        IEmailSender emailSender,
        IPushSender pushSender,
        IDateTimeProvider dateTimeProvider,
        ILogger<ProcessOutboxCommandHandler> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _pushSender = pushSender;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<OutboxRunResult> Handle(
        ProcessOutboxCommand request,
        CancellationToken cancellationToken)
    {
        var messages = await ClaimAsync(request, cancellationToken);

        var sent = 0;
        var retrying = 0;
        var abandoned = 0;

        foreach (var message in messages)
        {
            var outcome = await DeliverAsync(message, request, cancellationToken);

            switch (outcome)
            {
                case Outcome.Sent: sent++; break;
                case Outcome.Retrying: retrying++; break;
                default: abandoned++; break;
            }
        }

        return new OutboxRunResult(sent, retrying, abandoned);
    }

    /// <summary>
    /// Takes up to <see cref="ProcessOutboxCommand.BatchSize"/> due messages
    /// for this worker alone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One <c>UPDATE</c> stamps a claim token and pushes the next attempt
    /// beyond the lease; the rows are then read back by that token. Two
    /// workers running at the same moment cannot both take a message:
    /// PostgreSQL takes a row lock for the update, and the second worker
    /// re-evaluates its <c>WHERE</c> against the committed row afterwards — by
    /// which point <c>NextAttemptAt</c> has moved and the row no longer
    /// qualifies. The loser claims fewer rows, or none; it never claims the
    /// same one.
    /// </para>
    /// <para>
    /// Attempts is incremented here rather than after a failure, so a send
    /// that kills the process still counts. Otherwise a message that crashes
    /// the worker every time would be retried until somebody noticed.
    /// </para>
    /// </remarks>
    private async Task<IReadOnlyList<OutboxMessage>> ClaimAsync(
        ProcessOutboxCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var claimId = Guid.NewGuid();
        var leaseUntil = utcNow + request.ClaimLease;

        var claimed = await _context.OutboxMessages
            .Where(m => m.SentAt == null
                && m.AbandonedAt == null
                && m.NextAttemptAt <= utcNow)
            .OrderBy(m => m.NextAttemptAt)
            .Take(request.BatchSize)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.ClaimId, claimId)
                    .SetProperty(m => m.Attempts, m => m.Attempts + 1)
                    .SetProperty(m => m.NextAttemptAt, leaseUntil),
                cancellationToken);

        if (claimed == 0)
        {
            return [];
        }

        return await _context.OutboxMessages
            .Where(m => m.ClaimId == claimId)
            .ToListAsync(cancellationToken);
    }

    private enum Outcome { Sent, Retrying, Abandoned }

    private async Task<Outcome> DeliverAsync(
        OutboxMessage message,
        ProcessOutboxCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(message, cancellationToken);

            message.SentAt = _dateTimeProvider.UtcNow;
            message.LastError = null;

            await _context.SaveChangesAsync(cancellationToken);

            return Outcome.Sent;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown. The lease expires and the message comes back on its
            // own, so it must not be marked failed on the way out.
            throw;
        }
        catch (Exception exception)
        {
            return await RecordFailureAsync(message, request, exception, cancellationToken);
        }
    }

    private async Task SendAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        switch (message.Type)
        {
            case OutboxMessageType.Email:
                var email = Deserialize<EmailPayload>(message);

                await _emailSender.SendAsync(
                    email.To, email.Subject, email.HtmlBody, cancellationToken);
                break;

            case OutboxMessageType.Push:
                await SendPushAsync(Deserialize<PushPayload>(message), cancellationToken);
                break;

            default:
                // Exhaustive on purpose. A type this switch has not been
                // taught must not be silently marked as sent, which is what a
                // `default: break` would do.
                throw new ArgumentOutOfRangeException(
                    nameof(message),
                    message.Type,
                    "Unknown outbox message type.");
        }
    }

    private async Task SendPushAsync(PushPayload push, CancellationToken cancellationToken)
    {
        // Resolved now rather than at enqueue time: on a retry an hour later,
        // a frozen token list could be devices that no longer exist.
        var tokens = await _context.DeviceTokens
            .Where(t => push.UserIds.Contains(t.UserId))
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            // Nobody has a device registered. Delivered as far as this system
            // is concerned — the inbox row was written when the notification
            // was raised, and there is nothing to retry into.
            return;
        }

        var data = new Dictionary<string, string>(push.Data)
        {
            ["notificationType"] = push.Type.ToString(),
        };

        var result = await _pushSender.SendAsync(
            tokens, push.Title, push.Body, data, cancellationToken);

        if (result.InvalidTokens.Count > 0)
        {
            await _context.DeviceTokens
                .Where(t => result.InvalidTokens.Contains(t.Token))
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation(
                "Pruned {Count} invalid device token(s) after push.",
                result.InvalidTokens.Count);
        }
    }

    private async Task<Outcome> RecordFailureAsync(
        OutboxMessage message,
        ProcessOutboxCommand request,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow;

        // Truncated to fit the column. A stack trace is in the log; what
        // belongs on the row is enough to tell one failure from another.
        message.LastError = exception.Message.Length > 2_000
            ? exception.Message[..2_000]
            : exception.Message;

        if (message.Attempts >= request.MaxAttempts)
        {
            message.AbandonedAt = utcNow;

            _logger.LogError(
                exception,
                "Giving up on outbox message {MessageId} ({Type}) after {Attempts} attempts.",
                message.Id,
                message.Type,
                message.Attempts);
        }
        else
        {
            message.NextAttemptAt = utcNow + BackoffFor(message.Attempts);

            _logger.LogWarning(
                exception,
                "Outbox message {MessageId} ({Type}) failed on attempt {Attempts}; "
                + "next attempt at {NextAttemptAt:o}.",
                message.Id,
                message.Type,
                message.Attempts,
                message.NextAttemptAt);
        }

        // CancellationToken.None: the row has to record what happened even if
        // the host is shutting down, or the failure is invisible and the
        // message is retried with no record of why it needed to be.
        await _context.SaveChangesAsync(CancellationToken.None);

        return message.AbandonedAt is null ? Outcome.Retrying : Outcome.Abandoned;
    }

    /// <summary>
    /// Doubling delay: half a minute, one, two, four, eight.
    /// </summary>
    /// <remarks>
    /// Backing off rather than retrying immediately, because the failures this
    /// sees are almost always a service that is down — and hammering it while
    /// it restarts is how a queue turns a brief outage into a longer one.
    /// </remarks>
    private static TimeSpan BackoffFor(int attempts) =>
        TimeSpan.FromSeconds(30 * Math.Pow(2, Math.Max(0, attempts - 1)));

    private static T Deserialize<T>(OutboxMessage message) =>
        JsonSerializer.Deserialize<T>(message.PayloadJson, OutboxWriter.SerializerOptions)
        ?? throw new InvalidOperationException(
            $"Outbox message {message.Id} has an empty {typeof(T).Name} payload.");
}
