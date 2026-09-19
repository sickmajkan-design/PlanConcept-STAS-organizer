using System.Globalization;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Materials;

/// <summary>
/// Tells the people who reorder stock when a material falls below its minimum.
/// </summary>
/// <remarks>
/// Only the moment of crossing counts: a material that was already under its
/// minimum and goes lower is not announced again, so the bell does not ring on
/// every issue from a heap that has been low for a week. Restocking above the
/// minimum and falling through it again is a new event.
/// </remarks>
public static class LowStockNotifier
{
    public static async Task NotifyIfCrossedAsync(
        IApplicationDbContext context,
        INotificationService notifications,
        Guid materialId,
        decimal delta,
        CancellationToken cancellationToken)
    {
        if (delta >= 0)
        {
            return;
        }

        var material = await context.Materials
            .AsNoTracking()
            .Where(m => m.Id == materialId)
            .Select(m => new { m.Name, m.Unit, m.Quantity, m.MinimumQuantity })
            .FirstOrDefaultAsync(cancellationToken);

        if (material?.MinimumQuantity is not { } minimum)
        {
            return;
        }

        var before = material.Quantity - delta;

        if (before < minimum || material.Quantity >= minimum)
        {
            return;
        }

        var recipientIds = await context.Users
            .Where(u => u.IsActive &&
                        (u.Role == UserRole.SuperAdmin ||
                         u.Role == UserRole.Admin ||
                         u.Role == UserRole.ProjectManager))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        string Format(decimal value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        await notifications.NotifyUsersAsync(
            recipientIds,
            NotificationType.MaterialLowStock,
            "Low stock",
            $"{material.Name}: {Format(material.Quantity)} {material.Unit} left, below the minimum of {Format(minimum)}.",
            new Dictionary<string, string>
            {
                ["materialId"] = materialId.ToString(),
                ["materialName"] = material.Name,
                ["quantity"] = Format(material.Quantity),
                ["unit"] = material.Unit,
                ["minimum"] = Format(minimum)
            },
            cancellationToken: cancellationToken);
    }
}
