using Construction.Domain.Enums;

namespace Construction.Application.Features.Costs;

/// <summary>
/// Totals a stretch of finance entries into what a single posting cost,
/// however it was priced.
/// </summary>
/// <remarks>
/// Shared between the employee and project detail screens: both draw the same
/// "how long, and for how much" line under a posting, one from the employee's
/// side and one from the project's, so the arithmetic exists exactly once.
/// </remarks>
public static class AssignmentPaySummary
{
    public readonly record struct Entry(
        DateOnly OccurredOn,
        FinanceEntryKind Kind,
        decimal Amount,
        decimal? HoursWorked);

    public readonly record struct Totals(decimal Hours, int Days, decimal Amount);

    /// <summary>
    /// Sums the entries falling within a posting's dates. <paramref name="endDate"/>
    /// is the effective end — "today" for a posting still open — not the
    /// posting's own possibly-null <c>EndDate</c>.
    /// </summary>
    public static Totals For(
        IEnumerable<Entry> entries,
        DateOnly startDate,
        DateOnly endDate)
    {
        decimal hours = 0;
        decimal amount = 0;
        var days = 0;

        foreach (var entry in entries)
        {
            if (entry.OccurredOn < startDate || entry.OccurredOn > endDate)
            {
                continue;
            }

            amount += entry.Amount;

            switch (entry.Kind)
            {
                case FinanceEntryKind.WorkerPaymentHourly:
                    hours += entry.HoursWorked ?? 0;
                    break;
                case FinanceEntryKind.WorkerPaymentDaily:
                    days += 1;
                    break;
            }
        }

        return new Totals(hours, days, amount);
    }
}
