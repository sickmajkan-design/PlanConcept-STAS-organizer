namespace Construction.Domain.Enums;

/// <summary>How a book entry came to exist.</summary>
public enum BookEntrySource
{
    Manual = 1,

    /// <summary>Generated from a recurring-expense template.</summary>
    Template = 2,

    /// <summary>Imported from a spreadsheet.</summary>
    Import = 3,
}
