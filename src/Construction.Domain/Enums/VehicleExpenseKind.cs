namespace Construction.Domain.Enums;

public enum VehicleExpenseKind
{
    /// <summary>The only kind that carries litres.</summary>
    Fuel = 1,

    /// <summary>Scheduled servicing.</summary>
    Service = 2,

    /// <summary>Something broke.</summary>
    Repair = 3,

    Insurance = 4,

    Registration = 5,

    /// <summary>Tolls, parking, washing — the small recurring ones.</summary>
    Other = 99
}
