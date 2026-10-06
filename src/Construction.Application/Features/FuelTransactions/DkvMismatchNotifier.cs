using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.FuelTransactions;

/// <summary>How many rows of a new upload did not line up, by kind.</summary>
internal record DkvMismatchCounts(int NeedsReview, int NoDriverEntry, int UnknownCard)
{
    public int Total => NeedsReview + NoDriverEntry + UnknownCard;

    public static DkvMismatchCounts Of(IEnumerable<DkvPlanItem> newItems)
    {
        var items = newItems.ToList();

        return new DkvMismatchCounts(
            items.Count(i => i.Match.Status == FuelTransactionStatus.NeedsReview),
            items.Count(i => i.Match.Status == FuelTransactionStatus.NoDriverEntry),
            items.Count(i => i.Match.Status == FuelTransactionStatus.UnknownCard));
    }
}

/// <summary>
/// Tells the office that an uploaded statement has rows which do not agree with
/// what drivers recorded. One notice per upload, not one per row.
/// </summary>
/// <remarks>
/// Everyone who may settle rows, minus whoever uploaded it: they are looking at
/// the result already. Nothing is sent for a clean statement.
/// </remarks>
internal static class DkvMismatchNotifier
{
    public static async Task NotifyAsync(
        IApplicationDbContext context,
        INotificationService notifications,
        Guid? actingUserId,
        Guid batchId,
        string fileName,
        DkvMismatchCounts counts,
        CancellationToken cancellationToken)
    {
        if (counts.Total == 0)
        {
            return;
        }

        var recipientIds = await context.Users
            .Where(u => u.IsActive
                && u.Id != actingUserId
                && (u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        await notifications.NotifyUsersAsync(
            recipientIds,
            NotificationType.DkvStatementMismatch,
            "DKV statement does not match",
            $"{counts.Total} rows of {fileName} need a look.",
            new Dictionary<string, string>
            {
                ["batchId"] = batchId.ToString(),
                ["fileName"] = fileName,
                ["needsReview"] = counts.NeedsReview.ToString(),
                ["noDriverEntry"] = counts.NoDriverEntry.ToString(),
                ["unknownCard"] = counts.UnknownCard.ToString(),
                ["total"] = counts.Total.ToString()
            },
            cancellationToken: cancellationToken);
    }
}
