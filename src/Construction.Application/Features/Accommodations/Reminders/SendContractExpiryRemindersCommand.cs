using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Accommodations.Reminders;

/// <summary>
/// Warns the office that a housing contract is about to end (or already has),
/// early enough to renew it or move people out.
/// </summary>
/// <remarks>
/// Each contract end date is announced once. Moving the end date on the
/// accommodation counts as a new date and warns again when it comes near.
/// </remarks>
public record SendContractExpiryRemindersCommand : IRequest<int>
{
    public const int LeadDays = 30;
}

public class SendContractExpiryRemindersCommandHandler
    : IRequestHandler<SendContractExpiryRemindersCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly INotificationService _notifications;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SendContractExpiryRemindersCommandHandler(
        IApplicationDbContext context,
        INotificationService notifications,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _notifications = notifications;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<int> Handle(
        SendContractExpiryRemindersCommand request,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow);
        var horizon = today.AddDays(SendContractExpiryRemindersCommand.LeadDays);

        var due = await _context.Accommodations
            .Where(a => a.IsActive
                && a.ContractEnd != null
                && a.ContractEnd <= horizon
                && a.ContractExpiryNotifiedFor != a.ContractEnd)
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
        {
            return 0;
        }

        var recipientIds = await _context.Users
            .Where(u => u.IsActive && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var accommodation in due)
        {
            var end = accommodation.ContractEnd!.Value;
            accommodation.ContractExpiryNotifiedFor = end;
            await _context.SaveChangesAsync(cancellationToken);

            var name = string.IsNullOrWhiteSpace(accommodation.Name) ? accommodation.Address : accommodation.Name;

            await _notifications.NotifyUsersAsync(
                recipientIds,
                NotificationType.AccommodationContractExpiring,
                "Housing contract ending",
                end < today
                    ? $"The contract for {name} ended on {end:dd.MM.yyyy}."
                    : $"The contract for {name} ends on {end:dd.MM.yyyy}.",
                new Dictionary<string, string>
                {
                    ["accommodationId"] = accommodation.Id.ToString(),
                    ["accommodationName"] = name,
                    ["contractEnd"] = end.ToString("yyyy-MM-dd")
                },
                cancellationToken: cancellationToken);
        }

        return due.Count;
    }
}
