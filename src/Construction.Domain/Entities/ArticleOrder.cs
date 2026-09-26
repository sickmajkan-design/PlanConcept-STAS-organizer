using Construction.Domain.Common;
using Construction.Domain.Enums;

namespace Construction.Domain.Entities;

/// <summary>
/// A request for articles a person needs for the job: work trousers, boots, a
/// helmet, a tool that is missing. Not building material, which is stock.
/// </summary>
/// <remarks>
/// Anyone signed in may ask. The office orders it, sends it, and the person who
/// asked confirms it arrived, so the whole trip is visible at every step.
/// </remarks>
public class ArticleOrder : BaseEntity, IAuditable
{
    public Guid RequestedByUserId { get; set; }

    public User RequestedByUser { get; set; } = null!;

    /// <summary>The employee record behind the requester, when there is one. It names them and places them on a site.</summary>
    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    /// <summary>The site the articles are for. Optional: someone not posted anywhere may still need boots.</summary>
    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public ArticleOrderStatus Status { get; set; } = ArticleOrderStatus.Requested;

    public bool Urgent { get; set; }

    public string? Note { get; set; }

    /// <summary>Why it was declined. Set only when it was.</summary>
    public string? ReviewNote { get; set; }

    public Guid? HandledByUserId { get; set; }

    public User? HandledByUser { get; set; }

    public DateTime? OrderedAt { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public List<ArticleOrderItem> Items { get; set; } = [];
}

/// <summary>One line of a request.</summary>
public class ArticleOrderItem : BaseEntity
{
    public Guid ArticleOrderId { get; set; }

    public ArticleOrder ArticleOrder { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal Quantity { get; set; }

    /// <summary>"pcs", "pair"... free text, since the list of articles is not fixed.</summary>
    public string? Unit { get; set; }

    /// <summary>Size, colour, model.</summary>
    public string? Note { get; set; }
}
