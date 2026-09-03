using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>One block within a <see cref="Ledger"/> — a company or a site, e.g. "HELDELE".</summary>
public class LedgerSection : BaseEntity, IAuditable
{
    public Guid LedgerId { get; set; }

    public Ledger Ledger { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>Optional cross-reference to a real project. Free text otherwise.</summary>
    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public int SortOrder { get; set; }

    public ICollection<LedgerRow> Rows { get; set; } = new List<LedgerRow>();
}
