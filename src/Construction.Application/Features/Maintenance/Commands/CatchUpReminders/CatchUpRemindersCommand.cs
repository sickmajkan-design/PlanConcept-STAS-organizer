using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Accommodations.Reminders;
using Construction.Application.Features.Attachments.Commands.SendExpiryReminders;
using MediatR;

namespace Construction.Application.Features.Maintenance.Commands.CatchUpReminders;

/// <summary>
/// Sends the reminders that are due but not yet sent — documents about to lapse
/// and housing contracts about to end — without waiting for the daily sweep.
/// </summary>
/// <remarks>
/// <para>
/// A document entered in the morning that lapses in ten days would otherwise
/// reach the bell only at the next daily sweep, up to a day later. Signing in is
/// a good moment to look: it is when someone is about to read the bell.
/// </para>
/// <para>
/// It only ever sends what was never sent. Both sweeps record each reminder as
/// sent (a claim row, or the date already announced), so a notification someone
/// deliberately deleted is not brought back, and running this beside the daily
/// sweep, or on two instances, tells nobody twice. Low stock is not part of it:
/// that reminder is the moment a material crosses its minimum, keeps no record
/// of having been sent, and so cannot be re-derived without repeating itself.
/// </para>
/// <para>
/// Throttled, because the queries run for every administrator's sign-in and a
/// burst of sign-ins has nothing new to find.
/// </para>
/// </remarks>
public record CatchUpRemindersCommand : IRequest<int>
{
    /// <summary>How long a run keeps the next one away.</summary>
    public static readonly TimeSpan MinimumGap = TimeSpan.FromMinutes(10);

    /// <summary>Runs whatever the last run was; for code that wants the answer now, such as a test.</summary>
    public bool IgnoreGap { get; init; }
}

public class CatchUpRemindersCommandHandler : IRequestHandler<CatchUpRemindersCommand, int>
{
    private static readonly object Gate = new();
    private static DateTime lastRun = DateTime.MinValue;

    private readonly ISender _sender;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CatchUpRemindersCommandHandler(ISender sender, IDateTimeProvider dateTimeProvider)
    {
        _sender = sender;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> Handle(CatchUpRemindersCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        lock (Gate)
        {
            if (!request.IgnoreGap && now - lastRun < CatchUpRemindersCommand.MinimumGap && now >= lastRun)
            {
                return 0;
            }

            lastRun = now;
        }

        var documents = await _sender.Send(new SendExpiryRemindersCommand(), cancellationToken);
        var contracts = await _sender.Send(new SendContractExpiryRemindersCommand(), cancellationToken);

        return documents + contracts;
    }
}
