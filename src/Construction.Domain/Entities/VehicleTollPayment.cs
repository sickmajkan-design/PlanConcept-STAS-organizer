using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One payment or renewal of a <see cref="VehicleToll"/>, kept forever even
/// after the toll it belongs to is renewed again or deleted.
/// </summary>
/// <remarks>
/// <see cref="VehicleToll"/> only ever carries its current validity — every
/// time it is marked paid, the state about to be overwritten is copied here
/// first, so "who paid this and when, every time" survives the overwrite.
/// Append-only: nothing in the API updates or deletes a row here, which is
/// also why this does not implement <see cref="ISoftDeletable"/>.
/// </remarks>
public class VehicleTollPayment : BaseEntity, IAuditable
{
    public Guid VehicleTollId { get; set; }

    public VehicleToll VehicleToll { get; set; } = null!;

    public DateOnly ValidUntil { get; set; }

    public Guid PaidByUserId { get; set; }

    public User PaidByUser { get; set; } = null!;

    public DateTime PaidAt { get; set; }
}
