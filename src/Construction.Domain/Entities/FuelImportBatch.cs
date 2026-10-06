using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>One upload of a DKV statement: who, when, which file, and what it did.</summary>
public class FuelImportBatch : BaseEntity, IAuditable
{
    public string FileName { get; set; } = null!;

    public Guid? ImportedByUserId { get; set; }

    public int TotalRows { get; set; }

    /// <summary>Rows that were not seen before.</summary>
    public int NewCount { get; set; }

    /// <summary>Rows already on file whose billing status changed.</summary>
    public int UpdatedCount { get; set; }

    /// <summary>Rows already on file and unchanged.</summary>
    public int DuplicateCount { get; set; }

    public ICollection<FuelTransaction> Transactions { get; set; } = new List<FuelTransaction>();
}
