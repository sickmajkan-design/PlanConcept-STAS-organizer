using Construction.Application.Common.Exceptions;
using Construction.Application.Common.Interfaces;
using Construction.Application.Common.Spreadsheets;
using Construction.Application.Features.Exports.Queries;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.Ledgers.Queries.GetLedgerSectionRows;
using Construction.Application.Features.Ledgers.Queries.GetLedgerSummary;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Queries.ExportLedger;

/// <summary>
/// One month as a spreadsheet, laid out as it is on screen — section by section,
/// with each section's totals — so it can be handed to an accountant as it is.
/// </summary>
/// <remarks>
/// <para>
/// What is exported is what the screen shows: computed columns are their values,
/// sourced columns their figures. Numbers stay numbers, so the accountant can add
/// them up. A figure typed over a calculation is named in a "Napomena" column, so
/// it is not mistaken for a computed one.
/// </para>
/// <para>
/// A second sheet carries the summary boxes and the net total.
/// </para>
/// </remarks>
public record ExportLedgerQuery(Guid LedgerId) : IRequest<ExportFile>;

public class ExportLedgerQueryHandler : IRequestHandler<ExportLedgerQuery, ExportFile>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly ISpreadsheetWriter _writer;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ExportLedgerQueryHandler(
        IApplicationDbContext context,
        ISender sender,
        ISpreadsheetWriter writer,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _sender = sender;
        _writer = writer;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ExportFile> Handle(ExportLedgerQuery request, CancellationToken cancellationToken)
    {
        var ledger = await _context.Ledgers
            .AsNoTracking()
            .Where(l => l.Id == request.LedgerId)
            .Select(l => new { l.Name, l.Year, l.Month })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Ledger), request.LedgerId);

        var columns = await _context.LedgerColumns
            .AsNoTracking()
            .Where(c => c.LedgerId == request.LedgerId)
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Id, c.Name, c.DataType })
            .ToListAsync(cancellationToken);

        var sectionIds = await _context.LedgerSections
            .AsNoTracking()
            .Where(s => s.LedgerId == request.LedgerId)
            .OrderBy(s => s.SortOrder)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var kinds = columns.Select(c => c.DataType switch
        {
            LedgerColumnDataType.Currency => SpreadsheetValueKind.Money,
            LedgerColumnDataType.Number => SpreadsheetValueKind.Quantity,
            _ => SpreadsheetValueKind.Text,
        }).ToList();

        var header = new List<SpreadsheetColumn>
        {
            new("Sekcija", SpreadsheetValueKind.Text),
            new("Red", SpreadsheetValueKind.Text),
        };
        header.AddRange(columns.Select((c, i) => new SpreadsheetColumn(c.Name, kinds[i])));
        header.Add(new SpreadsheetColumn("Napomena", SpreadsheetValueKind.Text));

        object? Cell(LedgerCellDto? cell, int index)
        {
            if (cell?.Value is null)
            {
                return null;
            }

            return kinds[index] == SpreadsheetValueKind.Text
                ? cell.Value
                : LedgerCellMath.ParseNumeric(cell.Value);
        }

        var rows = new List<IReadOnlyList<object?>>();

        foreach (var sectionId in sectionIds)
        {
            var section = await _sender.Send(new GetLedgerSectionRowsQuery(request.LedgerId, sectionId), cancellationToken);
            var totals = new decimal[columns.Count];

            foreach (var row in section.Rows)
            {
                var line = new List<object?> { section.Name, row.Label };
                var overridden = new List<string>();

                for (var i = 0; i < columns.Count; i++)
                {
                    var cell = row.Cells.FirstOrDefault(c => c.ColumnId == columns[i].Id);
                    line.Add(Cell(cell, i));

                    if (cell?.IsOverride == true)
                    {
                        overridden.Add(columns[i].Name);
                    }

                    if (kinds[i] != SpreadsheetValueKind.Text && cell?.Value is not null)
                    {
                        totals[i] += LedgerCellMath.ParseNumeric(cell.Value);
                    }
                }

                line.Add(overridden.Count == 0 ? null : "Ručno: " + string.Join(", ", overridden));
                rows.Add(line);
            }

            // The section's own totals, so a reader does not have to add a block.
            var total = new List<object?> { section.Name, "Zbir" };
            total.AddRange(totals.Select((t, i) => kinds[i] == SpreadsheetValueKind.Text ? null : (object?)t));
            total.Add(null);
            rows.Add(total);
        }

        var summary = await _sender.Send(new GetLedgerSummaryQuery(request.LedgerId), cancellationToken);

        var summaryRows = summary.Boxes
            .Select(b => (IReadOnlyList<object?>)new List<object?> { b.Label, b.Sign > 0 ? "+" : "−", b.Value })
            .ToList();
        summaryRows.Add(new List<object?> { "Neto zbir (zarada)", null, summary.NetTotal });

        var spreadsheet = new Spreadsheet(
            [
                new SpreadsheetSheet(ledger.Name.Length > 28 ? ledger.Name[..28] : ledger.Name, header, rows),
                new SpreadsheetSheet(
                    "Sažetak",
                    [
                        new SpreadsheetColumn("Stavka", SpreadsheetValueKind.Text),
                        new SpreadsheetColumn("Predznak", SpreadsheetValueKind.Text),
                        new SpreadsheetColumn("Iznos", SpreadsheetValueKind.Money),
                    ],
                    summaryRows),
            ],
            _dateTimeProvider.UtcNow.ToString("dd.MM.yyyy. HH:mm"));

        return new ExportFile(
            $"evidencija-{ledger.Year}-{ledger.Month:00}.xlsx",
            _writer.ContentType,
            _writer.Write(spreadsheet));
    }
}
