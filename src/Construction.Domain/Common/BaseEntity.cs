namespace Construction.Domain.Common;

/// <summary>
/// Base class for all aggregate roots. Audit timestamps are set automatically
/// by the persistence layer (AuditableEntityInterceptor).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
