namespace Construction.Domain.Enums;

public enum ToolExpenseKind
{
    /// <summary>Something broke.</summary>
    Repair = 1,

    /// <summary>Scheduled servicing.</summary>
    Maintenance = 2,

    /// <summary>Recertifying an instrument's accuracy.</summary>
    Calibration = 3,

    Other = 99
}
