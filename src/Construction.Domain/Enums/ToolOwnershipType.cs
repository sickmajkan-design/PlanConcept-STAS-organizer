namespace Construction.Domain.Enums;

/// <summary>Whether the company owns a tool outright, or pays for the use of it.</summary>
public enum ToolOwnershipType
{
    Owned = 1,

    /// <summary>Short-term hire.</summary>
    Rented = 2,

    /// <summary>A longer-term leasing contract.</summary>
    Leased = 3
}
