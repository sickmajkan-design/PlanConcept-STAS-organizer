namespace Construction.Domain.Enums;

/// <summary>What a queued message is, and therefore which sender handles it.</summary>
public enum OutboxMessageType
{
    Email = 1,

    Push = 2
}
