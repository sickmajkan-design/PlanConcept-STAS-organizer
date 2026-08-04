namespace Construction.Application.Common.Spreadsheets;

/// <summary>
/// Column headings and sheet names, in the reader's language.
/// </summary>
/// <remarks>
/// The API is otherwise English-only, and everywhere else that is defensible
/// because the clients translate what they display. An export is different:
/// the file leaves the system and is opened in Excel by someone who never sees
/// the app, so nothing downstream can translate it. A Serbian office opening a
/// payroll export with English headings is exactly the gap this exists to
/// close.
///
/// Deliberately small. Only the columns the exports actually emit are here; a
/// general translation layer for the whole API is a different decision, and a
/// bigger one.
/// </remarks>
public static class ExportLabels
{
    /// <summary>Serbian unless the caller asks for English.</summary>
    /// <remarks>
    /// Serbian is the default because the system is built for a Serbian
    /// company. A missing or unrecognised language gets the language most of
    /// its users read, not the developer's.
    /// </remarks>
    public static bool IsEnglish(string? language) =>
        language is not null
        && language.StartsWith("en", StringComparison.OrdinalIgnoreCase);

    private static readonly Dictionary<string, (string Sr, string En)> Labels = new()
    {
        ["sheet.timeEntries"] = ("Radni sati", "Work hours"),
        ["sheet.projectCosts"] = ("Troškovi po gradilištu", "Costs by site"),
        ["sheet.vehicleCosts"] = ("Troškovi vozila", "Vehicle costs"),
        ["sheet.materialMovements"] = ("Promet materijala", "Stock movements"),

        ["employee"] = ("Radnik", "Employee"),
        ["project"] = ("Gradilište", "Site"),
        ["vehicle"] = ("Vozilo", "Vehicle"),
        ["material"] = ("Materijal", "Material"),
        ["date"] = ("Datum", "Date"),
        ["started"] = ("Početak", "Started"),
        ["ended"] = ("Kraj", "Ended"),
        ["break"] = ("Pauza (min)", "Break (min)"),
        ["worked"] = ("Radno vreme", "Worked"),
        ["workType"] = ("Vrsta rada", "Work type"),
        ["status"] = ("Status", "Status"),
        ["note"] = ("Napomena", "Note"),
        ["hours"] = ("Sati", "Hours"),
        ["labourCost"] = ("Trošak rada", "Labour"),
        ["unpricedHours"] = ("Sati bez cene", "Unpriced hours"),
        ["materialCost"] = ("Materijal", "Material"),
        ["total"] = ("Ukupno", "Total"),
        ["grandTotal"] = ("Sve zajedno", "Everything"),
        ["fuelCost"] = ("Gorivo", "Fuel"),
        ["litres"] = ("Litara", "Litres"),
        ["consumption"] = ("l/100 km", "l/100 km"),
        ["distance"] = ("Pređeno (km)", "Distance (km)"),
        ["serviceCost"] = ("Servis", "Service"),
        ["otherCost"] = ("Ostalo", "Other"),
        ["kind"] = ("Vrsta", "Kind"),
        ["quantity"] = ("Količina", "Quantity"),
        ["unit"] = ("Jedinica", "Unit"),
        ["unitPrice"] = ("Cena po jedinici", "Unit price"),
        ["value"] = ("Vrednost", "Value"),
        ["recordedBy"] = ("Evidentirao", "Recorded by"),
        ["period"] = ("Period", "Period")
    };

    public static string Get(string key, bool english)
    {
        // A key this build has not been taught yet shows as itself rather than
        // blanking a column heading, which would leave the reader guessing.
        if (!Labels.TryGetValue(key, out var pair))
        {
            return key;
        }

        return english ? pair.En : pair.Sr;
    }
}
