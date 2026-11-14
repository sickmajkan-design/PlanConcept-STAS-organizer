using Construction.Domain.Common;

namespace Construction.Domain.Entities;

/// <summary>
/// One kind of unexpected server error on one day: how many times it happened, where, and in which release it first
/// appeared that day. Holds no message, no user and no request data, only the exception type, the place in the code and
/// the route pattern, so it cannot carry personal data. Purged after 30 days.
/// </summary>
public class ServerError : BaseEntity
{
    /// <summary>SHA-256 (hex) of type, location and route: what makes two errors "the same".</summary>
    public string Key { get; set; } = null!;

    public DateOnly Day { get; set; }

    public string ExceptionType { get; set; } = null!;

    /// <summary>The first method of this application on the stack, e.g. <c>GetStorageStatusQueryHandler.Handle</c>.</summary>
    public string Location { get; set; } = null!;

    /// <summary>The route pattern, e.g. <c>GET /api/v{version:apiVersion}/attachments/{id}</c>; never the real address.</summary>
    public string Route { get; set; } = null!;

    public int Count { get; set; }

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public string Version { get; set; } = null!;
}
