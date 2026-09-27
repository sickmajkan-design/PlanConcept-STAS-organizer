namespace Construction.Domain.Enums;

/// <summary>
/// How the firm bills the client for the work done on a site. It decides where the payroll's
/// billing figure comes from.
/// </summary>
public enum ProjectBillingMode
{
    /// <summary>By the hour: hours worked times the client's price per hour. The default.</summary>
    Hourly = 0,

    /// <summary>A fixed sum agreed for the job: billed by the invoices the firm issues for it.</summary>
    FlatRate = 1,

    /// <summary>By measured work (Aufmaß): billed by the invoices issued for what was measured.</summary>
    Measured = 2,
}
