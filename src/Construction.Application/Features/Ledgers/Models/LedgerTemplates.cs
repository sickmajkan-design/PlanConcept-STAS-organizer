using System.Globalization;
using Construction.Domain.Entities;
using Construction.Domain.Enums;

namespace Construction.Application.Features.Ledgers.Models;

/// <summary>
/// Ready-made ledger layouts, so a new month starts as a working payroll
/// instead of an empty table the owner has to design.
/// </summary>
public static class LedgerTemplates
{
    /// <summary>
    /// Monthly payroll and margin per client, with hours typed in from the signed
    /// timesheets. This is the default: the owner's rule is that only the signed
    /// hours are authoritative for pay, so the app's own hours are a cross-check
    /// (see the hours-differ-from-app check), not the source.
    /// </summary>
    public const string Payroll = "Payroll";

    /// <summary>
    /// The same layout with the hour columns filled from approved time entries,
    /// which a figure typed over becomes a visible override. For a firm that does
    /// treat the app's hours as the record.
    /// </summary>
    public const string PayrollAppHours = "PayrollAppHours";

    /// <summary>The <see cref="LedgerColumn.SystemKey"/> values the payroll template uses.</summary>
    public static class Keys
    {
        public const string WorkerRate = "workerRate";
        public const string Hours = "hours";
        public const string ClientRate = "clientRate";
        public const string Pay = "pay";
        public const string Billing = "billing";
        public const string Margin = "margin";
        public const string Contributions = "contributions";
        public const string Rent = "rent";
        public const string Fuel = "fuel";
        public const string Housing = "housing";
        public const string Holiday = "holiday";
        public const string Difference = "difference";
        public const string Advance = "advance";
        public const string Bonus = "regres";
        public const string Result = "result";

        public static string Week(int number) => $"week{number}";

        /// <summary>
        /// The columns whose figures carry from one month to the next: the rates and
        /// the regres. Everything else is that month's own.
        /// </summary>
        /// <remarks>
        /// Contributions are not in this list on purpose. They come off the payslip
        /// and are not always the same, so a figure carried over would sit in the
        /// new month looking entered when nobody had looked at a payslip.
        /// </remarks>
        public static readonly IReadOnlyCollection<string> Recurring =
            [WorkerRate, ClientRate, Bonus];
    }

    /// <summary>
    /// Hour columns a month can need: a month can touch six calendar weeks, and one
    /// column each keeps a copied month simple. One a month does not use is empty.
    /// </summary>
    public const int WeekColumns = 6;

    /// <summary>The calendar weeks a month touches, each cut off at the month's ends.</summary>
    public static IReadOnlyList<(DateOnly From, DateOnly To, int IsoWeek)> MonthWeeks(int year, int month)
    {
        var weeks = new List<(DateOnly, DateOnly, int)>();
        var end = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        for (var start = new DateOnly(year, month, 1); start <= end;)
        {
            var toSunday = ((int)DayOfWeek.Sunday - (int)start.DayOfWeek + 7) % 7;
            var last = start.AddDays(toSunday) < end ? start.AddDays(toSunday) : end;

            weeks.Add((start, last, ISOWeek.GetWeekOfYear(start.ToDateTime(TimeOnly.MinValue))));
            start = last.AddDays(1);
        }

        return weeks;
    }

    /// <summary>What the n-th hour column is called: the calendar week it covers, as the spreadsheet does.</summary>
    public static string WeekName(int number, int year, int month)
    {
        var weeks = MonthWeeks(year, month);

        return number <= weeks.Count ? $"KW{weeks[number - 1].IsoWeek}" : $"Sedmica {number}";
    }

    /// <summary>
    /// Where a template column's automatic figure comes from for a given month, or
    /// null for a column that has none. A week the month does not have gets an empty
    /// range, so it reads zero.
    /// </summary>
    public static LedgerFormulaSource? SourceFor(string? key, int year, int month)
    {
        var first = new DateOnly(year, month, 1);
        var last = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        // What the whole month adds up to.
        switch (key)
        {
            case Keys.WorkerRate:
                return new LedgerFormulaSource(LedgerSourceKinds.EmployeeHourlyRate, first, last);
            case Keys.Fuel:
                return new LedgerFormulaSource(LedgerSourceKinds.VehicleFuelCost, first, last);
            case Keys.Rent:
                return new LedgerFormulaSource(LedgerSourceKinds.VehicleRentalCost, first, last);
            case Keys.Housing:
                return new LedgerFormulaSource(LedgerSourceKinds.AccommodationCost, first, last);
        }

        if (key is not null && key.StartsWith("week", StringComparison.Ordinal)
            && int.TryParse(key.AsSpan(4), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            && number is >= 1 and <= WeekColumns)
        {
            var weeks = MonthWeeks(year, month);

            if (number <= weeks.Count)
            {
                return new LedgerFormulaSource(LedgerSourceKinds.TimeEntryHours, weeks[number - 1].From, weeks[number - 1].To);
            }

            return new LedgerFormulaSource(LedgerSourceKinds.TimeEntryHours, last.AddDays(1), last);
        }

        return null;
    }

    public static bool IsKnown(string? template) =>
        string.Equals(template, Payroll, StringComparison.OrdinalIgnoreCase)
        || string.Equals(template, PayrollAppHours, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the template fills the hour columns from the app's time entries.</summary>
    public static bool HoursFromApp(string? template) =>
        string.Equals(template, PayrollAppHours, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Adds the payroll columns and summary boxes to a ledger.
    /// </summary>
    /// <remarks>
    /// The formulas are the ones a spreadsheet payroll runs on: hours are the sum of
    /// the weeks; a worker's pay is rate × hours − advance + difference + holiday;
    /// what the client is billed is hours × the client rate; the margin is billed
    /// minus pay; a section's result is the margin less every cost against it. The
    /// firm's profit is the summary boxes with their signs — income added, every
    /// cost taken away.
    /// </remarks>
    public static void ApplyPayroll(Ledger ledger, bool hoursFromApp = false)
    {
        var columns = new Dictionary<string, LedgerColumn>();
        var order = 0;

        LedgerColumn Add(string key, string name, LedgerColumnDataType type)
        {
            var column = new LedgerColumn
            {
                Id = Guid.CreateVersion7(),
                Name = name,
                DataType = type,
                SystemKey = key,
                SortOrder = order++,
            };

            columns[key] = column;
            ledger.Columns.Add(column);

            return column;
        }

        Guid Id(string key) => columns[key].Id;

        Add(Keys.WorkerRate, "Cijena sata radnika", LedgerColumnDataType.Currency);

        for (var week = 1; week <= WeekColumns; week++)
        {
            Add(Keys.Week(week), WeekName(week, ledger.Year, ledger.Month), LedgerColumnDataType.Number);
        }

        // Cost columns are added before the formulas that use them so every id exists.
        Add(Keys.Hours, "Sati", LedgerColumnDataType.Number);
        Add(Keys.ClientRate, "Cijena sata za klijenta", LedgerColumnDataType.Currency);
        Add(Keys.Pay, "Zarada radnika", LedgerColumnDataType.Currency);
        Add(Keys.Billing, "Naplata klijentu", LedgerColumnDataType.Currency);
        Add(Keys.Margin, "Marža", LedgerColumnDataType.Currency);
        Add(Keys.Contributions, "Doprinosi", LedgerColumnDataType.Currency);
        Add(Keys.Rent, "Rent a car", LedgerColumnDataType.Currency);
        Add(Keys.Fuel, "Gorivo", LedgerColumnDataType.Currency);
        Add(Keys.Housing, "Stanovanje", LedgerColumnDataType.Currency);
        // The signs are in the names: leave is added to the pay, an advance already
        // paid out is taken off it. One column in the old spreadsheet was called
        // both, depending on the section.
        Add(Keys.Holiday, "Godišnji odmor (+)", LedgerColumnDataType.Currency);
        Add(Keys.Difference, "Razlika od prošle plate / bonus", LedgerColumnDataType.Currency);
        Add(Keys.Advance, "Akontacija (−)", LedgerColumnDataType.Currency);
        Add(Keys.Bonus, "Regres", LedgerColumnDataType.Currency);
        Add(Keys.Result, "Rezultat", LedgerColumnDataType.Currency);

        LedgerFormulaTerm Plus(string key) => new(Id(key), 1);
        LedgerFormulaTerm Minus(string key) => new(Id(key), -1);

        // Hours per week and the hourly rate come from the system where it knows
        // them; typing a figure over one is a visible override.
        columns[Keys.WorkerRate].FormulaJson =
            new LedgerFormula([], [], SourceFor(Keys.WorkerRate, ledger.Year, ledger.Month)).ToJson();

        // Hours are typed from the signed timesheets unless the template says the
        // app's approved hours are the record.
        if (hoursFromApp)
        {
            for (var week = 1; week <= WeekColumns; week++)
            {
                columns[Keys.Week(week)].FormulaJson =
                    new LedgerFormula([], [], SourceFor(Keys.Week(week), ledger.Year, ledger.Month)).ToJson();
            }
        }

        // Fuel, rented cars and housing come from their own modules, for the person.
        foreach (var key in new[] { Keys.Fuel, Keys.Rent, Keys.Housing })
        {
            columns[key].FormulaJson =
                new LedgerFormula([], [], SourceFor(key, ledger.Year, ledger.Month)).ToJson();
        }

        columns[Keys.Hours].FormulaJson = new LedgerFormula(
            [],
            Enumerable.Range(1, WeekColumns).Select(w => Plus(Keys.Week(w))).ToList()).ToJson();

        columns[Keys.Pay].FormulaJson = new LedgerFormula(
            [Id(Keys.WorkerRate), Id(Keys.Hours)],
            [Minus(Keys.Advance), Plus(Keys.Difference), Plus(Keys.Holiday)]).ToJson();

        columns[Keys.Billing].FormulaJson = new LedgerFormula(
            [Id(Keys.Hours), Id(Keys.ClientRate)], []).ToJson();

        columns[Keys.Margin].FormulaJson = new LedgerFormula(
            [], [Plus(Keys.Billing), Minus(Keys.Pay)]).ToJson();

        columns[Keys.Result].FormulaJson = new LedgerFormula(
            [],
            [
                Plus(Keys.Margin), Minus(Keys.Contributions), Minus(Keys.Rent), Minus(Keys.Fuel),
                Minus(Keys.Housing), Minus(Keys.Advance), Minus(Keys.Bonus),
            ]).ToJson();

        var boxOrder = 0;

        void Box(string label, int sign, string? sourceKey, string? color = null) =>
            ledger.SummaryBoxes.Add(new LedgerSummaryBox
            {
                Label = label,
                Sign = sign,
                SourceColumnId = sourceKey is null ? null : Id(sourceKey),
                ManualValue = sourceKey is null ? 0m : null,
                Color = color,
                SortOrder = boxOrder++,
            });

        // Income is the one box that adds; its colour sets it apart from the costs.
        Box("Ukupan prihod (marža)", 1, Keys.Margin, "#FFE08A");
        Box("Gorivo", -1, Keys.Fuel);
        Box("Regres", -1, Keys.Bonus);
        Box("Doprinosi", -1, Keys.Contributions);
        Box("Stanovi", -1, Keys.Housing);
        Box("Auta (rent a car)", -1, Keys.Rent);
        Box("Fiksni troškovi", -1, null);
        Box("Alat i uniforme", -1, null);
    }
}
