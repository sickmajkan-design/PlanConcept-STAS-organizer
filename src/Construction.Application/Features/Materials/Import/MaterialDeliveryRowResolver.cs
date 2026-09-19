using System.Globalization;
using System.Text;
using Construction.Application.Features.FuelCards.Import;

namespace Construction.Application.Features.Materials.Import;

/// <summary>
/// Reads the rows of an uploaded delivery list. The file has a header row and
/// the columns are found by name, so a list exported from anywhere works as
/// long as it says what each column is (Serbian or English names).
/// </summary>
public static class MaterialDeliveryRowResolver
{
    private static readonly Dictionary<string, string[]> Aliases = new()
    {
        ["material"] = ["material", "materijal", "naziv", "name", "artikal", "artikl"],
        ["quantity"] = ["quantity", "kolicina", "kol", "qty"],
        ["unit"] = ["unit", "jedinica", "jedinicamjere", "jm"],
        ["price"] = ["price", "unitprice", "cijena", "cena", "nabavnacijena", "cijenapojedinici"],
        ["invoice"] = ["invoice", "invoicenumber", "faktura", "brojfakture", "racun", "brojracuna"],
        ["supplier"] = ["supplier", "dobavljac", "isporucilac", "dobavljaci"],
        ["date"] = ["date", "datum", "datumprijema", "receivedon"],
        ["note"] = ["note", "napomena", "komentar"],
    };

    /// <summary>Lowercase, diacritics and separators stripped: "Broj fakture" and "broj_fakture" match.</summary>
    public static string Normalise(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var decomposed = raw.Trim().ToLowerInvariant()
            .Replace('đ', 'd').Replace('č', 'c').Replace('ć', 'c').Replace('š', 's').Replace('ž', 'z')
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder();

        foreach (var character in decomposed)
        {
            if (char.IsLetterOrDigit(character)
                && CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    public static IReadOnlyList<MaterialImportRowResult> Resolve(
        IReadOnlyList<IReadOnlyList<string>> rows,
        IReadOnlyDictionary<string, Guid> materialIdsByName,
        Func<Guid, string, decimal, decimal?, DateOnly, bool> isAlreadyImported,
        DateOnly today)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var header = rows[0];
        var columns = new Dictionary<string, int>();

        for (var i = 0; i < header.Count; i++)
        {
            var name = Normalise(header[i]);

            foreach (var (field, names) in Aliases)
            {
                if (!columns.ContainsKey(field) && names.Contains(name))
                {
                    columns[field] = i;
                }
            }
        }

        string? Cell(IReadOnlyList<string> row, string field) =>
            columns.TryGetValue(field, out var index) && index < row.Count
                ? row[index].Trim()
                : null;

        var results = new List<MaterialImportRowResult>();
        var seen = new HashSet<(string, string, decimal, decimal?, DateOnly)>();

        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var rowNumber = i + 1;

            var name = Cell(row, "material");
            var unit = Cell(row, "unit");
            var invoice = Cell(row, "invoice");
            var supplier = Cell(row, "supplier");
            var note = Cell(row, "note");

            var quantityText = Cell(row, "quantity");
            var priceText = Cell(row, "price");
            var dateText = Cell(row, "date");

            decimal? quantity = null;
            decimal? price = null;
            DateOnly? date = dateText is { Length: > 0 }
                ? FuelStatementValueParser.TryParseDate(dateText, out var parsedDate) ? parsedDate : null
                : today;

            if (FuelStatementValueParser.TryParseDecimal(quantityText, out var q))
            {
                quantity = q;
            }

            if (priceText is { Length: > 0 } && FuelStatementValueParser.TryParseDecimal(priceText, out var p))
            {
                price = p;
            }

            Guid? materialId = null;
            var status = MaterialImportRowStatus.Ready;

            if (string.IsNullOrWhiteSpace(name))
            {
                status = MaterialImportRowStatus.MissingMaterialName;
            }
            else if (quantity is not > 0)
            {
                status = MaterialImportRowStatus.InvalidQuantity;
            }
            else if (priceText is { Length: > 0 } && (price is null || price < 0))
            {
                status = MaterialImportRowStatus.InvalidPrice;
            }
            else if (string.IsNullOrWhiteSpace(invoice))
            {
                status = MaterialImportRowStatus.MissingInvoiceNumber;
            }
            else if (date is null || date > today)
            {
                status = MaterialImportRowStatus.InvalidDate;
            }
            else if (materialIdsByName.TryGetValue(name.Trim().ToLowerInvariant(), out var id))
            {
                materialId = id;
            }
            else if (string.IsNullOrWhiteSpace(unit))
            {
                status = MaterialImportRowStatus.UnknownMaterialNoUnit;
            }
            else
            {
                status = MaterialImportRowStatus.NewMaterial;
            }

            if (status is MaterialImportRowStatus.Ready or MaterialImportRowStatus.NewMaterial)
            {
                // The same line twice in one file, or one already imported on an
                // earlier run, must not be recorded twice.
                var key = (name!.Trim().ToLowerInvariant(), invoice!.Trim(), quantity!.Value, price, date!.Value);

                if (!seen.Add(key)
                    || (materialId is { } existing
                        && isAlreadyImported(existing, invoice.Trim(), quantity.Value, price, date.Value)))
                {
                    status = MaterialImportRowStatus.AlreadyImported;
                }
            }

            results.Add(new MaterialImportRowResult
            {
                RowNumber = rowNumber,
                MaterialName = name,
                Unit = string.IsNullOrWhiteSpace(unit) ? null : unit,
                Quantity = quantity,
                UnitPrice = price,
                InvoiceNumber = string.IsNullOrWhiteSpace(invoice) ? null : invoice,
                Supplier = string.IsNullOrWhiteSpace(supplier) ? null : supplier,
                OccurredOn = date,
                Note = string.IsNullOrWhiteSpace(note) ? null : note,
                MaterialId = materialId,
                Status = status
            });
        }

        return results;
    }
}
