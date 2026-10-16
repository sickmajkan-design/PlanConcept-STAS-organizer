namespace Construction.Domain.Enums;

public enum AuditAction
{
    Created = 1,
    Updated = 2,

    /// <summary>
    /// A soft delete. The row is still on disk with <c>IsDeleted</c> set, so
    /// this records the moment it left circulation rather than the moment it
    /// ceased to exist.
    /// </summary>
    Deleted = 3,

    /// <summary>
    /// A document of a sensitive kind was opened by somebody other than the person it is about. Reading
    /// leaves no change to compare, so the entry carries what was opened and how.
    /// </summary>
    Viewed = 4
}
