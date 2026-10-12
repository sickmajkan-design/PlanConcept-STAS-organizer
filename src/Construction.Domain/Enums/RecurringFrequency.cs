namespace Construction.Domain.Enums;

/// <summary>How often a recurring cost falls due. The value is the number of months between two of them.</summary>
public enum RecurringFrequency
{
    Monthly = 1,

    Quarterly = 3,

    Annual = 12,
}
