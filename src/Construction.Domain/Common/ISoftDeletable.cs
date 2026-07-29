namespace Construction.Domain.Common;

/// <summary>
/// Entities implementing this interface are never physically removed.
/// The persistence layer converts deletes into updates and applies a
/// global query filter so soft-deleted rows are excluded by default.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTime? DeletedAt { get; set; }
}
