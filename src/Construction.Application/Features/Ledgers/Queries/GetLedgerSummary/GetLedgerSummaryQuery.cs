using Construction.Application.Common.Interfaces;
using Construction.Application.Features.Ledgers.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Queries.GetLedgerSummary;

/// <summary>
/// A ledger's month-summary panel — every box's live figure and the net total
/// they add up to. Cell values for every box referencing a real column are
/// fetched in one query regardless of how many boxes there are, since a box's
/// column is usually shared by only a handful of the ledger's rows and the
/// nested per-section fetch already avoids loading the rest.
/// </summary>
public record GetLedgerSummaryQuery(Guid LedgerId) : IRequest<LedgerSummaryPanelDto>;

public class GetLedgerSummaryQueryHandler : IRequestHandler<GetLedgerSummaryQuery, LedgerSummaryPanelDto>
{
    private readonly IApplicationDbContext _context;

    public GetLedgerSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerSummaryPanelDto> Handle(
        GetLedgerSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var boxes = await _context.LedgerSummaryBoxes
            .AsNoTracking()
            .Where(b => b.LedgerId == request.LedgerId)
            .OrderBy(b => b.SortOrder)
            .Select(b => new
            {
                b.Id,
                b.Label,
                b.SourceColumnId,
                SourceColumnName = b.SourceColumn != null ? b.SourceColumn.Name : null,
                b.ManualValue,
                b.Sign,
                b.Color,
                b.SortOrder,
            })
            .ToListAsync(cancellationToken);

        var sourceColumnIds = boxes
            .Where(b => b.SourceColumnId.HasValue)
            .Select(b => b.SourceColumnId!.Value)
            .Distinct()
            .ToList();

        // One query for every referenced column's cell values, grouped afterwards —
        // not one query per box.
        var cellValuesByColumn = sourceColumnIds.Count == 0
            ? new Dictionary<Guid, List<string?>>()
            : await _context.LedgerCells
                .Where(cell => cell.Row.Section.LedgerId == request.LedgerId
                    && sourceColumnIds.Contains(cell.ColumnId))
                .GroupBy(cell => cell.ColumnId)
                .Select(g => new { ColumnId = g.Key, Values = g.Select(c => c.Value).ToList() })
                .ToDictionaryAsync(g => g.ColumnId, g => g.Values, cancellationToken);

        var boxDtos = new List<LedgerSummaryBoxDto>();
        decimal netTotal = 0m;

        foreach (var box in boxes)
        {
            var value = box.SourceColumnId is { } columnId
                ? (cellValuesByColumn.TryGetValue(columnId, out var values)
                    ? values.Sum(LedgerCellMath.ParseNumeric)
                    : 0m)
                : box.ManualValue ?? 0m;

            netTotal += value * box.Sign;

            boxDtos.Add(new LedgerSummaryBoxDto
            {
                Id = box.Id,
                Label = box.Label,
                SourceColumnId = box.SourceColumnId,
                SourceColumnName = box.SourceColumnName,
                ManualValue = box.ManualValue,
                Sign = box.Sign,
                Color = box.Color,
                SortOrder = box.SortOrder,
                Value = value,
            });
        }

        return new LedgerSummaryPanelDto { Boxes = boxDtos, NetTotal = netTotal };
    }
}
