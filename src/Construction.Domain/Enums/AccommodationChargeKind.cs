namespace Construction.Domain.Enums;

/// <summary>How a charge on an accommodation is counted.</summary>
public enum AccommodationChargeKind
{
    /// <summary>A fixed amount per month for the whole unit, however many people live in it.</summary>
    Monthly = 1,

    /// <summary>An amount per person per day, counted only for the days somebody stays (a hotel, a night's booking).</summary>
    DailyPerPerson = 2,

    /// <summary>A single amount on a single date: a deposit, an agency fee, cleaning.</summary>
    OneOff = 3
}
