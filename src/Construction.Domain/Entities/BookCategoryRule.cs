using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// "A description that contains this text probably belongs in that category." Only ever a
/// suggestion — the first rule by priority that matches is offered, nothing is filed on its own.
/// </summary>
public class BookCategoryRule : BaseEntity, IAuditable
{
    public string Text { get; set; } = null!;

    public Guid CategoryId { get; set; }

    public BookCategory Category { get; set; } = null!;

    /// <summary>Lower runs first.</summary>
    public int Priority { get; set; }
}
