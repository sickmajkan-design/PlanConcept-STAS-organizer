using System.Globalization;
using Construction.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Models;

/// <summary>
/// The total of a column across every row of a ledger — what a summary box shows.
/// </summary>
/// <remarks>
/// A typed-in column is the sum of its stored cells. A computed column has no
/// stored cells to sum (only overrides), so it is computed row by row and the
/// results added, which is the only way a box such as "total margin" can be
/// right. One read of the ledger's cells serves every requested column.
/// </remarks>
public static class LedgerColumnTotals
{
    public static async Task<IReadOnlyDictionary<Guid, decimal>> ComputeAsync(
        IApplicationDbContext context,
        Guid ledgerId,
        IReadOnlyCollection<Guid> columnIds,
        CancellationToken cancellationToken)
    {
        var totals = columnIds.Distinct().ToDictionary(id => id, _ => 0m);

        if (totals.Count == 0)
        {
            return totals;
        }

        var columns = await context.LedgerColumns
            .AsNoTracking()
            .Where(c => c.LedgerId == ledgerId)
            .Select(c => new { c.Id, c.FormulaJson })
            .ToListAsync(cancellationToken);

        var calculator = new LedgerCalculator(
            columns.Select(c => (c.Id, LedgerFormula.Parse(c.FormulaJson))));

        var cells = (await context.LedgerCells
                .AsNoTracking()
                .Where(c => c.Row.Section.LedgerId == ledgerId)
                .Select(c => new { c.RowId, c.ColumnId, c.Value })
                .ToListAsync(cancellationToken))
            .GroupBy(c => c.RowId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(c => c.ColumnId, c => c.Value));

        // Every row counts, not only those with typed cells: a row can have
        // nothing typed and still carry hours from the timesheet.
        var rows = await LedgerRowRefs.LoadAsync(context, ledgerId, cancellationToken);

        var auto = await LedgerAutoValues.LoadAsync(
            context,
            LedgerAutoValues.SourcesOf(columns.Select(c => (c.Id, c.FormulaJson))),
            rows,
            cancellationToken);

        foreach (var row in rows)
        {
            var stored = cells.TryGetValue(row.RowId, out var s) ? s : new Dictionary<Guid, string?>();
            var computed = calculator.HasFormulas
                ? calculator.ComputeRow(stored, auto.TryGetValue(row.RowId, out var a) ? a : null)
                : null;

            foreach (var id in totals.Keys.ToList())
            {
                if (computed is not null && computed.TryGetValue(id, out var value))
                {
                    totals[id] += value.Value;
                }
                else if (stored.TryGetValue(id, out var raw))
                {
                    totals[id] += LedgerCellMath.ParseNumeric(raw);
                }
            }
        }

        return totals;
    }

    /// <summary>How a computed cell's number is written into a cell value.</summary>
    public static string Format(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
