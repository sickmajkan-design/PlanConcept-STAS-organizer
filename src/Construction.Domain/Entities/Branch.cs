using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One of the operator's own organizational units (poslovna jedinica) — e.g. a regional office.
/// Not to be confused with <see cref="CustomerCompany"/>, which is a legal entity of a client.
/// </summary>
/// <remarks>
/// A project optionally belongs to one branch; vehicles, tools, costs and time entries are
/// attributed to a branch through the project they are assigned to, never directly.
/// </remarks>
public class Branch : BaseEntity, IAuditable
{
    public string Name { get; set; } = null!;

    /// <summary>Hex colour (#RRGGBB) of the dot shown before the branch name in the UI.</summary>
    public string Color { get; set; } = "#3457D5";

    /// <summary>A branch with projects cannot be deleted, only switched off so it is no longer offered.</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
