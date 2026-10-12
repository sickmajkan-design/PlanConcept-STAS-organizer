using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// A cost that comes back on its own — rent of an office, an insurance, a subscription — kept as a template
/// from which one entry of the book is made for each period it falls in.
/// </summary>
/// <remarks>
/// Switching a template off stops it making more entries and leaves the ones it made. Changing the amount
/// applies from the next period: entries already made keep the amount they were made with.
/// </remarks>
public class RecurringExpense : BaseEntity, IAuditable
{
    public string Name { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public BookCategory Category { get; set; } = null!;

    /// <summary>Always more than zero.</summary>
    public decimal Amount { get; set; }

    public RecurringFrequency Frequency { get; set; }

    /// <summary>The first day of the first month it applies to.</summary>
    public DateOnly StartMonth { get; set; }

    /// <summary>The day of the month it falls due. 1 to 28, so it exists in every month.</summary>
    public int DayOfMonth { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    /// <summary>
    /// The country the cost belongs to when it is kept per country (two-letter code). Only a label for the
    /// entries; it changes nothing in how they are counted.
    /// </summary>
    public string? CountryCode { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>The last month it applies to. Null means it goes on until switched off.</summary>
    public DateOnly? EndDate { get; set; }

    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }
}
