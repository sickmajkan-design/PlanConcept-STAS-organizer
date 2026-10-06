using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// Records that the office has already been told a vehicle date is coming up, at one stage (30 days
/// before, 7 days before), so the daily sweep never says the same thing twice.
/// </summary>
/// <remarks>
/// Scoped to the date at claim time, not just to the vehicle: renewing the registration moves the date,
/// which is a new event that deserves its own reminders, so the claims for the old date must not
/// suppress them. One notice goes to every admin (unlike the toll sweep, which keeps a claim per admin),
/// because these dates have no per-person lead time.
/// </remarks>
public class VehicleDateReminder : BaseEntity
{
    public Guid VehicleId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;

    public VehicleDateKind Kind { get; set; }

    /// <summary>The date the reminder was about when it was claimed.</summary>
    public DateOnly DueDate { get; set; }

    /// <summary>How many days before <see cref="DueDate"/> this stage is: 30 or 7.</summary>
    public int DaysBefore { get; set; }

    public DateTime SentAt { get; set; }
}
