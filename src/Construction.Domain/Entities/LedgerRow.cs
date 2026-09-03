using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>One line within a <see cref="LedgerSection"/> — usually a worker.</summary>
public class LedgerRow : BaseEntity, IAuditable
{
    public Guid SectionId { get; set; }

    public LedgerSection Section { get; set; } = null!;

    public string Label { get; set; } = null!;

    /// <summary>Optional cross-reference to a real employee. Free text otherwise.</summary>
    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public int SortOrder { get; set; }

    public ICollection<LedgerCell> Cells { get; set; } = new List<LedgerCell>();
}
