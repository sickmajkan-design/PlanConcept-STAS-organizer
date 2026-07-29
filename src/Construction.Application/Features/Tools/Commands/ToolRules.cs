using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Commands;

/// <summary>
/// Shared tool business rules: uniqueness of serial number / QR code and the
/// status transitions driven by assignments.
/// </summary>
internal static class ToolRules
{
    public static async Task EnsureUniqueAsync(
        IApplicationDbContext context,
        string? serialNumber,
        string? qrCode,
        Guid? excludeToolId,
        CancellationToken cancellationToken)
    {
        if (serialNumber is not null)
        {
            var serialTaken = await context.Tools.AnyAsync(
                t => t.SerialNumber == serialNumber &&
                     (excludeToolId == null || t.Id != excludeToolId),
                cancellationToken);

            if (serialTaken)
            {
                throw new ConflictException($"Serial number '{serialNumber}' is already in use.");
            }
        }

        if (qrCode is not null)
        {
            var qrTaken = await context.Tools.AnyAsync(
                t => t.QrCode == qrCode && (excludeToolId == null || t.Id != excludeToolId),
                cancellationToken);

            if (qrTaken)
            {
                throw new ConflictException($"QR code '{qrCode}' is already in use.");
            }
        }
    }

    /// <summary>
    /// A tool may only be handed out while it is in circulation; tools under
    /// repair, lost or retired stay where they are until their status changes.
    /// </summary>
    public static void EnsureAssignable(Tool tool)
    {
        if (tool.Status is ToolStatus.UnderRepair or ToolStatus.Lost or ToolStatus.Retired)
        {
            throw new ConflictException(
                $"The tool cannot be assigned while its status is '{tool.Status}'.");
        }
    }

    /// <summary>
    /// Keeps Status in sync with the assignments: Assigned while anyone or any
    /// project holds the tool, back to Available when fully released.
    /// </summary>
    public static void RecomputeStatus(Tool tool)
    {
        var hasAssignment = tool.AssignedEmployeeId is not null || tool.AssignedProjectId is not null;

        if (hasAssignment)
        {
            tool.Status = ToolStatus.Assigned;
        }
        else if (tool.Status == ToolStatus.Assigned)
        {
            tool.Status = ToolStatus.Available;
        }
    }
}
