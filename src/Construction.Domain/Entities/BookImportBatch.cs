using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One import of a month from the spreadsheet the firm kept before: what was read, what was written and what
/// was left out, kept next to the month so the owner can see it again.
/// </summary>
public class BookImportBatch : BaseEntity, IAuditable
{
    public string FileName { get; set; } = null!;

    public int TargetYear { get; set; }

    public int TargetMonth { get; set; }

    /// <summary>"Completed" or "Undone".</summary>
    public string Status { get; set; } = "Completed";

    public int RowCount { get; set; }

    public int ImportedCount { get; set; }

    public int SkippedCount { get; set; }

    /// <summary>The settlement the import wrote into.</summary>
    public Guid? LedgerId { get; set; }

    /// <summary>Whether the import made the settlement. Only then can it be undone as a whole.</summary>
    public bool LedgerCreated { get; set; }

    /// <summary>When the import finished writing. A cell changed after it is a manual change.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Row by row what became of the file. It holds people's names, so it is cleared after a retention period
    /// by the maintenance sweep.
    /// </summary>
    public string? ReportJson { get; set; }

    public Guid? RecordedByUserId { get; set; }

    public User? RecordedByUser { get; set; }
}
