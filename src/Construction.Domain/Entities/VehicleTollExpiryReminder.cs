using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// Records that a specific admin has already been told a specific toll is
/// lapsing for its current validity period, so the nightly sweep never
/// repeats itself to them. Same shape as <see cref="AttachmentExpiryReminder"/>
/// and the same reason: each admin sets their own lead time via
/// <see cref="User.DocumentExpiryReminderDays"/>.
/// </summary>
/// <remarks>
/// Scoped to the toll's <see cref="ValidUntil"/> at claim time, not just the
/// toll id: a renewal starts a new validity period, which is a new event
/// worth telling admins about again, so the claim from the previous period
/// must not suppress it.
/// </remarks>
public class VehicleTollExpiryReminder : BaseEntity
{
    public Guid VehicleTollId { get; set; }

    public VehicleToll VehicleToll { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>The <see cref="VehicleToll.ValidUntil"/> this claim was made against — see remarks.</summary>
    public DateOnly ValidUntil { get; set; }

    public DateTime SentAt { get; set; }
}
