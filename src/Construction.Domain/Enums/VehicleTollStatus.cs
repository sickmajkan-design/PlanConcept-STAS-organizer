namespace Construction.Domain.Enums;

/// <summary>
/// Whether a <see cref="Entities.VehicleToll"/>'s current validity period
/// has been paid for. Deliberately just these two values — "expired" and
/// "expiring soon" are not stored here because they would go stale the
/// moment the clock moved past <see cref="Entities.VehicleToll.ValidUntil"/>
/// without anyone touching the row. They are computed at read time instead,
/// the same way <see cref="Entities.Attachment.IsExpiredOn"/> is.
/// </summary>
public enum VehicleTollStatus
{
    Unpaid = 1,
    Paid = 2
}
