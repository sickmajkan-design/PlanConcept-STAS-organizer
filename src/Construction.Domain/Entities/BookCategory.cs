using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// A category a book entry is filed under — data, not an enum, so the owner can add their own.
/// </summary>
/// <remarks>
/// System categories carry a <see cref="SystemKey"/>: they can be renamed but never switched
/// off, because the monthly settlement and the overview rely on them. Entries are never deleted
/// with their category; a category that is no longer used is switched off instead.
/// </remarks>
public class BookCategory : BaseEntity, IAuditable
{
    public string Name { get; set; } = null!;

    public BookCategoryKind Kind { get; set; }

    /// <summary>An optional heading for grouping in lists.</summary>
    public string? Group { get; set; }

    public BookCategoryBucket Bucket { get; set; }

    /// <summary>Set only on the categories the system seeds.</summary>
    public string? SystemKey { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
