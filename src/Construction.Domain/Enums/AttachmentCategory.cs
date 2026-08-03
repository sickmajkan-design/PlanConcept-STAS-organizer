namespace Construction.Domain.Enums;

/// <summary>
/// What an attached file is, which decides where it is shown and whether its
/// expiry is anyone's problem.
/// </summary>
public enum AttachmentCategory
{
    /// <summary>Employment contract or annex.</summary>
    Contract = 1,

    /// <summary>Training or competence certificate — the kind that lapses.</summary>
    Certificate = 2,

    /// <summary>Occupational medical check.</summary>
    MedicalCheck = 3,

    /// <summary>Licence or permit, for a person or a vehicle.</summary>
    Licence = 4,

    /// <summary>Insurance policy.</summary>
    Insurance = 5,

    /// <summary>Drawing, permit or other site paperwork.</summary>
    SiteDocument = 6,

    /// <summary>Photograph taken on site.</summary>
    Photo = 7,

    Other = 99
}
