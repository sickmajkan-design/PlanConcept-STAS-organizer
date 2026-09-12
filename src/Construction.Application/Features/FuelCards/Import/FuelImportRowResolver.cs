namespace Construction.Application.Features.FuelCards.Import;

/// <summary>What a fuel card resolves to, for matching a statement row.</summary>
public record FuelCardLookupEntry(Guid VehicleId, string VehicleName, string Provider);

/// <summary>
/// Resolves every raw statement row against the column mapping and the known
/// fuel cards. Pure and DB-free so the exact same logic backs both the
/// preview (which writes nothing) and the commit (which does) — the two must
/// never disagree about which rows are clean.
/// </summary>
public static class FuelImportRowResolver
{
    public static List<FuelImportRowResult> Resolve(
        IReadOnlyList<IReadOnlyList<string>> rows,
        FuelImportColumnMapping mapping,
        bool hasHeaderRow,
        IReadOnlyDictionary<string, FuelCardLookupEntry> cardsByNumber)
    {
        var results = new List<FuelImportRowResult>();
        var dataRows = hasHeaderRow ? rows.Skip(1) : rows;
        var rowNumber = hasHeaderRow ? 1 : 0;

        foreach (var row in dataRows)
        {
            rowNumber++;
            results.Add(ResolveRow(rowNumber, row, mapping, cardsByNumber));
        }

        return results;
    }

    private static FuelImportRowResult ResolveRow(
        int rowNumber,
        IReadOnlyList<string> row,
        FuelImportColumnMapping mapping,
        IReadOnlyDictionary<string, FuelCardLookupEntry> cardsByNumber)
    {
        var cardNumber = Cell(row, mapping.CardNumberColumn)?.Trim();

        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return Problem(rowNumber, cardNumber, FuelImportRowStatus.MissingCardNumber,
                "No card number on this row.");
        }

        if (!cardsByNumber.TryGetValue(cardNumber.ToLowerInvariant(), out var card))
        {
            return Problem(rowNumber, cardNumber, FuelImportRowStatus.NoMatchingCard,
                $"No fuel card registered with number '{cardNumber}'.");
        }

        if (!FuelStatementValueParser.TryParseDate(Cell(row, mapping.OccurredOnColumn), out var occurredOn))
        {
            return Problem(rowNumber, cardNumber, FuelImportRowStatus.InvalidDate,
                "Could not read the date on this row.");
        }

        if (!FuelStatementValueParser.TryParseDecimal(Cell(row, mapping.AmountColumn), out var amount))
        {
            return Problem(rowNumber, cardNumber, FuelImportRowStatus.InvalidAmount,
                "Could not read the amount on this row.");
        }

        if (!FuelStatementValueParser.TryParseDecimal(Cell(row, mapping.LitresColumn), out var litres)
            || litres <= 0)
        {
            return Problem(rowNumber, cardNumber, FuelImportRowStatus.InvalidLitres,
                "Could not read the litres on this row.");
        }

        var supplier = mapping.SupplierColumn is { } supplierColumn
            ? Cell(row, supplierColumn)?.Trim()
            : null;

        var note = mapping.NoteColumn is { } noteColumn
            ? Cell(row, noteColumn)?.Trim()
            : null;

        var fuelProductType = mapping.FuelProductTypeColumn is { } fuelProductTypeColumn
            ? Cell(row, fuelProductTypeColumn)?.Trim()
            : null;

        // Odometer is never required — a statement that omits it, or a
        // reading that fails to parse, just leaves the expense without one
        // rather than rejecting an otherwise-clean row over it. Mirrors
        // VehicleExpense.OdometerKm's own "missing is better than invented"
        // reasoning for manual entry.
        int? odometerKm = null;
        if (mapping.OdometerColumn is { } odometerColumn
            && FuelStatementValueParser.TryParseDecimal(Cell(row, odometerColumn), out var odometer))
        {
            odometerKm = (int)decimal.Round(odometer);
        }

        return new FuelImportRowResult
        {
            RowNumber = rowNumber,
            CardNumber = cardNumber,
            VehicleId = card.VehicleId,
            VehicleName = card.VehicleName,
            OccurredOn = occurredOn,
            Amount = amount,
            Litres = litres,
            OdometerKm = odometerKm,
            FuelProductType = string.IsNullOrWhiteSpace(fuelProductType) ? null : fuelProductType,
            Supplier = string.IsNullOrWhiteSpace(supplier) ? null : supplier,
            Note = string.IsNullOrWhiteSpace(note) ? null : note,
            Status = FuelImportRowStatus.Ready,
        };
    }

    private static FuelImportRowResult Problem(
        int rowNumber, string? cardNumber, FuelImportRowStatus status, string reason) =>
        new()
        {
            RowNumber = rowNumber,
            CardNumber = cardNumber,
            Status = status,
            Reason = reason,
        };

    private static string? Cell(IReadOnlyList<string> row, int index) =>
        index >= 0 && index < row.Count ? row[index] : null;
}
