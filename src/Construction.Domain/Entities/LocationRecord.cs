namespace Construction.Domain.Entities;

/// <summary>
/// GPS ping sent by the mobile app (every 60 seconds while logged in).
/// High-volume append-only table: bigint identity key, no audit columns.
/// </summary>
public class LocationRecord
{
    public long Id { get; set; }

    public Guid EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    /// <summary>Horizontal accuracy radius in meters, as reported by the device.</summary>
    public double? Accuracy { get; set; }

    public DateTime Timestamp { get; set; }

    public DateTime ReceivedAt { get; set; }
}
