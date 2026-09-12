using Construction.Application.Features.FuelCards.Import;

namespace Construction.Application.Common.Interfaces;

/// <summary>
/// Reads an .xlsx workbook into plain rows of cell strings. Kept behind an
/// interface, like <see cref="ISpreadsheetWriter"/>, because ClosedXML lives
/// in the Infrastructure project and the Application layer must not reference
/// it directly.
/// </summary>
public interface IFuelStatementParser
{
    FuelStatementParseResult ParseWorkbook(Stream content);
}
