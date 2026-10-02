namespace Construction.Domain.Enums;

/// <summary>What a business unit is in law, which decides whose identification numbers it carries.</summary>
public enum BranchKind
{
    /// <summary>A separate legal entity with its own registration and tax numbers.</summary>
    LegalEntity = 1,

    /// <summary>
    /// A representative office abroad. It is not a legal entity of its own, so a number it leaves
    /// blank is the company's (Podaci firme) on any document that names it.
    /// </summary>
    RepresentativeOffice = 2,
}
