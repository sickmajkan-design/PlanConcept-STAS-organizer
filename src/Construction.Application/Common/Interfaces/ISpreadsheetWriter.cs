using Construction.Application.Common.Spreadsheets;

namespace Construction.Application.Common.Interfaces;

/// <summary>Renders a <see cref="Spreadsheet"/> to an .xlsx file.</summary>
public interface ISpreadsheetWriter
{
    /// <summary>The MIME type the API should return the bytes with.</summary>
    string ContentType { get; }

    byte[] Write(Spreadsheet spreadsheet);
}
