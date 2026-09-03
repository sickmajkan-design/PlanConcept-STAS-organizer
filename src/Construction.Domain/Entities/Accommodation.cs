using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// A rented apartment or house the company houses workers in — the asset
/// itself; what it costs per month lives in <see cref="AccommodationRate"/>.
/// </summary>
public class Accommodation : BaseEntity, ISoftDeletable, IAuditable
{
    public string Address { get; set; } = null!;

    public string? Note { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<AccommodationRate> Rates { get; set; } = new List<AccommodationRate>();
}
