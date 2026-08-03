namespace Construction.Domain.Enums;

public enum AbsenceType
{
    AnnualLeave = 1,
    SickLeave = 2,

    /// <summary>Time off without pay.</summary>
    UnpaidLeave = 3,

    /// <summary>Bereavement, marriage, blood donation — the statutory short ones.</summary>
    PaidSpecialLeave = 4,

    /// <summary>Training or a course away from site.</summary>
    Training = 5,

    Other = 99
}
