using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>One user-defined column of a <see cref="Ledger"/>, shared by every section in it.</summary>
public class LedgerColumn : BaseEntity, IAuditable
{
    public Guid LedgerId { get; set; }

    public Ledger Ledger { get; set; } = null!;

    public string Name { get; set; } = null!;

    public LedgerColumnDataType DataType { get; set; }

    public int SortOrder { get; set; }
}
