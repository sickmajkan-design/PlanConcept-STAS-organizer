using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// Money actually received against a project's contract — an invoice paid, a
/// milestone signed off, an advance.
/// </summary>
/// <remarks>
/// The other half of <see cref="Project.ContractValue"/>: the contract says
/// what was agreed, this says what has come in against it so far. Kept as a
/// running ledger rather than a single "amount realized" total on the project
/// itself, so the annual realization plan can split what came in this
/// calendar year from what came in earlier — the same reason a rate is dated
/// rather than a column on <see cref="Employee"/>.
/// </remarks>
public class ProjectRevenue : BaseEntity, IAuditable
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    /// <summary>What was received, in the system's single currency.</summary>
    public decimal Amount { get; set; }

    public DateOnly OccurredOn { get; set; }

    public string? Note { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public User? RecordedByUser { get; set; }
}
