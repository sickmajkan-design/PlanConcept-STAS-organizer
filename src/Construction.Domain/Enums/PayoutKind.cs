namespace Construction.Domain.Enums;

public enum PayoutKind
{
    /// <summary>Money paid before the month is settled. Entered when it is paid, and can happen several times.</summary>
    Advance = 1,

    /// <summary>The payment that closes the month for a group. Planned for a day, marked paid when it goes out.</summary>
    Final = 2,
}
