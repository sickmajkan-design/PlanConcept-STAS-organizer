namespace Construction.Domain.Enums;

/// <summary>How a worker's pay for a stretch of work was priced.</summary>
public enum FinanceEntryKind
{
    /// <summary>Hours worked times an hourly rate. The only kind that carries hours.</summary>
    WorkerPaymentHourly = 1,

    /// <summary>A flat amount agreed regardless of hours.</summary>
    WorkerPaymentFixed = 2,

    /// <summary>A flat amount for a day worked, whatever the hours were.</summary>
    WorkerPaymentDaily = 3
}
