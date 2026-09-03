using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One month of the SuperAdmin's own free-form record-keeping — the in-app
/// equivalent of a monthly Excel tab. Columns, sections and rows are all
/// user-defined; nothing here is schema the rest of the app understands.
/// </summary>
public class Ledger : BaseEntity, ISoftDeletable, IAuditable
{
    public string Name { get; set; } = null!;

    public int Year { get; set; }

    public int Month { get; set; }

    public string? Note { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<LedgerColumn> Columns { get; set; } = new List<LedgerColumn>();

    public ICollection<LedgerSection> Sections { get; set; } = new List<LedgerSection>();
}
