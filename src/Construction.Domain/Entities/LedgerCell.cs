using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One cell's value, at the intersection of a <see cref="LedgerRow"/> and a
/// <see cref="LedgerColumn"/>.
/// </summary>
/// <remarks>
/// Always a string, whatever the column's declared <c>DataType</c> — the real
/// sheet this feature replaces mixes blanks, "0" and stray text in columns
/// that are otherwise numbers, and a typed value here would just mean some of
/// what someone types gets rejected. The frontend parses for display and for
/// the per-section subtotal; nothing here needs the value to be valid.
/// </remarks>
public class LedgerCell : BaseEntity, IAuditable
{
    public Guid RowId { get; set; }

    public LedgerRow Row { get; set; } = null!;

    public Guid ColumnId { get; set; }

    public LedgerColumn Column { get; set; } = null!;

    public string? Value { get; set; }
}
