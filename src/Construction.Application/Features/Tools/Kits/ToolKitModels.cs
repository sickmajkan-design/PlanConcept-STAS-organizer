using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Tools.Kits;

public record ToolKitItemDto(
    Guid Id,
    string Name,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string State,
    int SortOrder);

public record ToolKitEventDto(
    Guid Id,
    DateTime At,
    Guid? ItemId,
    string ItemName,
    string Type,
    decimal? Amount,
    Guid? EmployeeId,
    string? EmployeeName,
    string? Note,
    DateTime? SettledAt,
    string? PayoutMethod);

/// <summary>What a case is made of right now, what is wrong with it, and what is owed because of it.</summary>
public record ToolKitDto(
    Guid ToolId,
    string Name,
    IReadOnlyList<ToolKitItemDto> Items,
    IReadOnlyList<ToolKitEventDto> Events,
    decimal TotalValue,
    /// <summary>Worth of what is absent: missing, charged or written off.</summary>
    decimal MissingValue,
    /// <summary>Worth of what is broken, with the worker or with the office.</summary>
    decimal BrokenValue,
    /// <summary>Charged to workers and not yet collected.</summary>
    decimal OpenCharges,
    /// <summary>Owed to workers for replacements they bought and not yet paid out.</summary>
    decimal OpenRefunds);

internal static class ToolKitReader
{
    /// <summary>The kit with its history. The tool has to exist and has to be a kit.</summary>
    public static async Task<ToolKitDto> LoadAsync(
        IApplicationDbContext context,
        Guid toolId,
        CancellationToken cancellationToken)
    {
        var tool = await LoadKitAsync(context, toolId, cancellationToken);

        var items = await context.ToolKitItems
            .AsNoTracking()
            .Where(i => i.ToolId == toolId)
            .OrderBy(i => i.SortOrder).ThenBy(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        var events = await context.ToolKitEvents
            .AsNoTracking()
            .Where(e => e.ToolId == toolId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new ToolKitEventDto(
                e.Id,
                e.CreatedAt,
                e.ItemId,
                e.ItemName,
                e.Type.ToString(),
                e.Amount,
                e.EmployeeId,
                e.Employee != null ? e.Employee.FirstName + " " + e.Employee.LastName : null,
                e.Note,
                e.SettledAt,
                e.PayoutMethod != null ? e.PayoutMethod.ToString() : null))
            .ToListAsync(cancellationToken);

        decimal Worth(Func<ToolKitItem, bool> pick) =>
            items.Where(pick).Sum(i => i.Quantity * i.UnitPrice);

        return new ToolKitDto(
            toolId,
            tool.Name,
            items.Select(i => new ToolKitItemDto(
                i.Id, i.Name, i.Quantity, i.UnitPrice, i.Quantity * i.UnitPrice, i.State.ToString(), i.SortOrder)).ToList(),
            events,
            Worth(_ => true),
            Worth(i => i.State is ToolKitItemState.Missing or ToolKitItemState.Charged or ToolKitItemState.WrittenOff),
            Worth(i => i.State is ToolKitItemState.Broken or ToolKitItemState.BrokenHandedIn),
            events.Where(e => e.Type == nameof(ToolKitEventType.ChargedToWorker) && e.SettledAt == null).Sum(e => e.Amount ?? 0),
            events.Where(e => e.Type == nameof(ToolKitEventType.ReplacementBought) && e.SettledAt == null).Sum(e => e.Amount ?? 0));
    }

    /// <summary>The tool, refused when it is not a kit.</summary>
    public static async Task<Tool> LoadKitAsync(
        IApplicationDbContext context,
        Guid toolId,
        CancellationToken cancellationToken)
    {
        var tool = await context.Tools.FirstOrDefaultAsync(t => t.Id == toolId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tool), toolId);

        if (!tool.IsKit)
        {
            throw new ConflictException("This tool is not a kit.");
        }

        return tool;
    }
}
