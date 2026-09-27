namespace Construction.Domain.Enums;

/// <summary>Where an issued invoice stands.</summary>
public enum InvoiceStatus
{
    /// <summary>Issued to the client and counted in the billing of its month.</summary>
    Issued = 0,

    /// <summary>The client has paid it. Still counted in the billing.</summary>
    Paid = 1,

    /// <summary>Withdrawn. No longer counted anywhere; kept so the number is never reused.</summary>
    Cancelled = 2,
}
