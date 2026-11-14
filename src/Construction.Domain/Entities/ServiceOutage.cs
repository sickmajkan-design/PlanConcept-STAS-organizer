using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// A stretch of time in which the application was not running, found when it came back: the last heartbeat it wrote
/// before going quiet, and the moment it started again. Covers a crash, a stopped host and a long restart; a normal
/// deploy is shorter than the gap that counts (see <c>UptimeService</c>).
/// </summary>
public class ServiceOutage : BaseEntity
{
    public DateTime StartedAt { get; set; }

    public DateTime EndedAt { get; set; }
}

/// <summary>One row, always: the last time the running application wrote "I am here".</summary>
public class ServiceHeartbeat : BaseEntity
{
    /// <summary>The fixed id of the only row.</summary>
    public static readonly Guid SingleId = new("00000000-0000-0000-0000-0000000000b1");

    public DateTime LastBeatAt { get; set; }
}
