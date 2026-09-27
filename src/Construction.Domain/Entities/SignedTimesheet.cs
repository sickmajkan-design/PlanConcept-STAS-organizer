using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One project's client-signed timesheet for one calendar week — a row that exists so an
/// <see cref="Attachment"/> has something to hang off. The scan itself is the point; this
/// row is only the (project, week) key it is filed under.
/// </summary>
public class SignedTimesheet : BaseEntity, IAuditable
{
    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    /// <summary>The ISO week's own year, which is not always the calendar year of its days —
    /// see <see cref="Construction.Application.Features.Ledgers.Models.LedgerTemplates"/>.</summary>
    public int Year { get; set; }

    /// <summary>ISO-8601 week number, 1 through 53.</summary>
    public int IsoWeek { get; set; }
}
