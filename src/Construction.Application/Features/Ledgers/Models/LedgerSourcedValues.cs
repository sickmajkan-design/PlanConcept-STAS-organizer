using System.Globalization;
using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Costs.Queries.GetToolCosts;
using Construction.Application.Features.Costs.Queries.GetVehicleCosts;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Models;

/// <summary>
/// Computes the value of every "sourced" column's cells for one section's
/// rows, at read time. A sourced <see cref="Construction.Domain.Entities.LedgerColumn"/>'s
/// cells are never stored in <see cref="Construction.Domain.Entities.LedgerCell"/> — this
/// is the only place their value comes from.
/// </summary>
public static class LedgerSourcedValues
{
    public static async Task<LedgerSectionDto> OverlayAsync(
        IApplicationDbContext context,
        IMediator mediator,
        LedgerSectionDto section,
        IEnumerable<(Guid ColumnId, LedgerColumnSourceMetric Metric)> sourcedColumns,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var columns = sourcedColumns.ToList();
        if (columns.Count == 0 || section.Rows.Count == 0)
        {
            return section;
        }

        var vehicleColumnId = columns
            .Where(c => c.Metric == LedgerColumnSourceMetric.VehicleTotalCost)
            .Select(c => (Guid?)c.ColumnId)
            .FirstOrDefault();

        var toolColumnId = columns
            .Where(c => c.Metric == LedgerColumnSourceMetric.ToolTotalCost)
            .Select(c => (Guid?)c.ColumnId)
            .FirstOrDefault();

        var materialColumnId = columns
            .Where(c => c.Metric == LedgerColumnSourceMetric.MaterialCost)
            .Select(c => (Guid?)c.ColumnId)
            .FirstOrDefault();

        // One report call per linked entity, not per cell — a section is a
        // handful to dozens of rows, so this stays cheap.
        var vehicleTotals = new Dictionary<Guid, decimal>();
        var toolTotals = new Dictionary<Guid, decimal>();
        var materialTotals = new Dictionary<Guid, decimal>();

        if (vehicleColumnId is not null)
        {
            foreach (var vehicleId in section.Rows
                .Where(r => r.VehicleId.HasValue)
                .Select(r => r.VehicleId!.Value)
                .Distinct())
            {
                var report = await mediator.Send(
                    new GetVehicleCostsQuery { From = from, To = to, VehicleId = vehicleId },
                    cancellationToken);
                vehicleTotals[vehicleId] = report.Total;
            }
        }

        if (toolColumnId is not null)
        {
            foreach (var toolId in section.Rows
                .Where(r => r.ToolId.HasValue)
                .Select(r => r.ToolId!.Value)
                .Distinct())
            {
                var report = await mediator.Send(
                    new GetToolCostsQuery { From = from, To = to, ToolId = toolId },
                    cancellationToken);
                toolTotals[toolId] = report.Total;
            }
        }

        if (materialColumnId is not null)
        {
            var materialIds = section.Rows
                .Where(r => r.MaterialId.HasValue)
                .Select(r => r.MaterialId!.Value)
                .Distinct()
                .ToList();

            if (materialIds.Count > 0)
            {
                var rows = await context.MaterialMovements
                    .AsNoTracking()
                    .Where(m => materialIds.Contains(m.MaterialId)
                        && m.Kind == MaterialMovementKind.Out
                        && m.UnitPrice != null
                        && m.OccurredOn >= from && m.OccurredOn <= to)
                    .Where(m => section.ProjectId == null || m.ProjectId == section.ProjectId)
                    .GroupBy(m => m.MaterialId)
                    .Select(g => new { MaterialId = g.Key, Cost = g.Sum(m => m.UnitPrice!.Value * m.Quantity) })
                    .ToListAsync(cancellationToken);

                materialTotals = rows.ToDictionary(r => r.MaterialId, r => r.Cost);
            }
        }

        var newRows = section.Rows.Select(row =>
        {
            var extraCells = new List<LedgerCellDto>();

            if (vehicleColumnId is { } vCol)
            {
                var value = row.VehicleId is { } vId && vehicleTotals.TryGetValue(vId, out var v)
                    ? v
                    : (decimal?)null;
                extraCells.Add(SourcedCell(vCol, value));
            }

            if (toolColumnId is { } tCol)
            {
                var value = row.ToolId is { } tId && toolTotals.TryGetValue(tId, out var t)
                    ? t
                    : (decimal?)null;
                extraCells.Add(SourcedCell(tCol, value));
            }

            if (materialColumnId is { } mCol)
            {
                var value = row.MaterialId is { } mId && materialTotals.TryGetValue(mId, out var m)
                    ? m
                    : (decimal?)null;
                extraCells.Add(SourcedCell(mCol, value));
            }

            return new LedgerRowDto
            {
                Id = row.Id,
                Label = row.Label,
                EmployeeId = row.EmployeeId,
                EmployeeName = row.EmployeeName,
                VehicleId = row.VehicleId,
                VehicleName = row.VehicleName,
                ToolId = row.ToolId,
                ToolName = row.ToolName,
                MaterialId = row.MaterialId,
                MaterialName = row.MaterialName,
                PromotedGeneralExpenseId = row.PromotedGeneralExpenseId,
                PromotedAccommodationRateId = row.PromotedAccommodationRateId,
                ColorTag = row.ColorTag,
                SortOrder = row.SortOrder,
                Cells = row.Cells.Concat(extraCells).ToList(),
            };
        }).ToList();

        return new LedgerSectionDto
        {
            Id = section.Id,
            Name = section.Name,
            ProjectId = section.ProjectId,
            ProjectName = section.ProjectName,
            SortOrder = section.SortOrder,
            RowCount = section.RowCount,
            Rows = newRows,
        };
    }

    private static LedgerCellDto SourcedCell(Guid columnId, decimal? value) => new()
    {
        ColumnId = columnId,
        Value = value?.ToString("0.00", CultureInfo.InvariantCulture),
        IsComputed = true,
    };
}
