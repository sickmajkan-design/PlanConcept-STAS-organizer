using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.DocumentRequests;

/// <summary>Whether a person has a driving licence on their record that is still valid.</summary>
public static class DriverLicence
{
    public const string Missing = "Missing";

    public const string Expired = "Expired";

    /// <summary>
    /// Null when the person has a licence that is valid (or one with no end date); otherwise why the
    /// office should look at it. A reminder, never a block: a licence that simply has not been filed
    /// yet must not stop a van leaving the yard.
    /// </summary>
    public static async Task<string?> WarningAsync(
        IApplicationDbContext context,
        Guid employeeId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        // What was sent and has not been accepted is not a licence on the record yet.
        var pending = await context.DocumentRequestEvents
            .AsNoTracking()
            .Where(e => e.Type == DocumentRequestEventType.Submitted
                && e.DocumentRequest.EmployeeId == employeeId
                && (e.DocumentRequest.Status == DocumentRequestStatus.Submitted
                    || e.DocumentRequest.Status == DocumentRequestStatus.Returned))
            .Select(e => e.AttachmentIds)
            .ToListAsync(cancellationToken);

        var pendingIds = pending.SelectMany(p => p).ToList();

        var expiries = await context.Attachments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId
                && a.Category == AttachmentCategory.Licence
                && !pendingIds.Contains(a.Id))
            .Select(a => a.ExpiresAt)
            .ToListAsync(cancellationToken);

        if (expiries.Count == 0)
        {
            return Missing;
        }

        return expiries.Any(e => e is null || e >= today) ? null : Expired;
    }
}
