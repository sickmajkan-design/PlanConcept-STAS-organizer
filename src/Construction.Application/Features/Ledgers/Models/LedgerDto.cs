using System.Globalization;
using System.Linq.Expressions;
using Construction.Application.Common.Interfaces;
using Construction.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Construction.Application.Features.Ledgers.Models;

/// <summary>One row in the ledger list — no columns/sections/rows, just enough to pick one.</summary>
public class LedgerSummaryDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public int Year { get; init; }

    public int Month { get; init; }

    public string? Note { get; init; }

    public string? CreatedByName { get; init; }

    public DateTime CreatedAt { get; init; }
}

public class LedgerCellDto
{
    /// <summary>
    /// The real <see cref="Construction.Domain.Entities.LedgerCell"/> row's id —
    /// what its audit trail is keyed on. Null for a computed (sourced) cell,
    /// which is never actually stored and so has no edit history.
    /// </summary>
    public Guid? Id { get; init; }

    public Guid ColumnId { get; init; }

    public string? Value { get; init; }

    /// <summary>Hex background color, or null for none.</summary>
    public string? ColorTag { get; init; }

    /// <summary>
    /// True when this cell's <see cref="Value"/> was computed from real
    /// platform data (its column has a <c>SourceMetric</c>) rather than
    /// typed in — never persisted as a <see cref="Construction.Domain.Entities.LedgerCell"/>,
    /// and the API refuses a write to it.
    /// </summary>
    public bool IsComputed { get; init; }
}

public class LedgerRowDto
{
    public Guid Id { get; init; }

    public string Label { get; init; } = null!;

    public Guid? EmployeeId { get; init; }

    public string? EmployeeName { get; init; }

    public Guid? VehicleId { get; init; }

    public string? VehicleName { get; init; }

    public Guid? ToolId { get; init; }

    public string? ToolName { get; init; }

    public Guid? MaterialId { get; init; }

    public string? MaterialName { get; init; }

    /// <summary>Set once this row was pushed through the real General Expense form.</summary>
    public Guid? PromotedGeneralExpenseId { get; init; }

    /// <summary>Set once this row was pushed through the real Accommodation-rate form.</summary>
    public Guid? PromotedAccommodationRateId { get; init; }

    /// <summary>Hex background color for the whole row, or null for none.</summary>
    public string? ColorTag { get; init; }

    public int SortOrder { get; init; }

    public IReadOnlyCollection<LedgerCellDto> Cells { get; init; } = Array.Empty<LedgerCellDto>();
}

public class LedgerSectionDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public Guid? ProjectId { get; init; }

    public string? ProjectName { get; init; }

    public int SortOrder { get; init; }

    /// <summary>
    /// How many rows this section has, known even when <see cref="Rows"/> has not
    /// been loaded yet — the ledger shell needs this to show a row count on a
    /// collapsed section without paying for its rows and cells.
    /// </summary>
    public int RowCount { get; init; }

    /// <summary>Empty on the ledger shell; populated only by the per-section rows fetch.</summary>
    public IReadOnlyCollection<LedgerRowDto> Rows { get; init; } = Array.Empty<LedgerRowDto>();
}

public class LedgerColumnDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string DataType { get; init; } = null!;

    /// <summary>Null for a manual/free-typed column (today's only behavior); otherwise the metric its cells are computed from.</summary>
    public string? SourceMetric { get; init; }

    public int SortOrder { get; init; }
}

/// <summary>One box of a ledger's month-summary panel, with its current computed value.</summary>
public class LedgerSummaryBoxDto
{
    public Guid Id { get; init; }

    public string Label { get; init; } = null!;

    public Guid? SourceColumnId { get; init; }

    public string? SourceColumnName { get; init; }

    public decimal? ManualValue { get; init; }

    public int Sign { get; init; }

    public string? Color { get; init; }

    public int SortOrder { get; init; }

    /// <summary>
    /// The box's current figure — the live sum of <see cref="SourceColumnId"/> across
    /// every row in the ledger when set, otherwise <see cref="ManualValue"/> (or 0).
    /// </summary>
    public decimal Value { get; init; }
}

/// <summary>
/// One row somewhere in the ledger that has been pushed through to a real
/// General Expense or Accommodation rate — what the "promoted this month"
/// overview reads from.
/// </summary>
public class LedgerPromotionDto
{
    public Guid RowId { get; init; }

    public string RowLabel { get; init; } = null!;

    public string SectionName { get; init; } = null!;

    /// <summary>"GeneralExpense" or "AccommodationRate".</summary>
    public string Target { get; init; } = null!;

    public Guid TargetId { get; init; }

    public decimal Amount { get; init; }

    public DateOnly OccurredOn { get; init; }
}

/// <summary>
/// One row somewhere in the ledger with no Employee/Vehicle/Tool/Material
/// link at all — what the "unlinked rows" audit reads from. A lightweight
/// list rather than requiring every section to be opened to find them.
/// </summary>
public class LedgerUnlinkedRowDto
{
    public Guid RowId { get; init; }

    public string RowLabel { get; init; } = null!;

    public Guid SectionId { get; init; }

    public string SectionName { get; init; } = null!;
}

/// <summary>A ledger's whole summary panel: every box plus the net total they add up to.</summary>
public class LedgerSummaryPanelDto
{
    public IReadOnlyCollection<LedgerSummaryBoxDto> Boxes { get; init; } = Array.Empty<LedgerSummaryBoxDto>();

    /// <summary>Sum of every box's <c>Value * Sign</c> — the in-app "ZARADA" figure.</summary>
    public decimal NetTotal { get; init; }
}

/// <summary>The whole month, in one shape — every column, section, row and cell.</summary>
public class LedgerDetailDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public int Year { get; init; }

    public int Month { get; init; }

    public string? Note { get; init; }

    public string? CreatedByName { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public IReadOnlyCollection<LedgerColumnDto> Columns { get; init; } = Array.Empty<LedgerColumnDto>();

    public IReadOnlyCollection<LedgerSectionDto> Sections { get; init; } = Array.Empty<LedgerSectionDto>();
}

/// <summary>How a <see cref="Ledger"/> becomes a <see cref="LedgerSummaryDto"/>.</summary>
public static class LedgerSummaryMapping
{
    public static readonly Expression<Func<Ledger, LedgerSummaryDto>> Projection = ledger =>
        new LedgerSummaryDto
        {
            Id = ledger.Id,
            Name = ledger.Name,
            Year = ledger.Year,
            Month = ledger.Month,
            Note = ledger.Note,
            CreatedByName = ledger.CreatedByUser != null ? ledger.CreatedByUser.Email : null,
            CreatedAt = ledger.CreatedAt,
        };

    private static readonly Func<Ledger, LedgerSummaryDto> Compiled = Projection.Compile();

    public static LedgerSummaryDto ToDto(Ledger ledger) => Compiled(ledger);
}

/// <summary>How a <see cref="LedgerSection"/> becomes a <see cref="LedgerSectionDto"/> with its rows and cells populated.</summary>
public static class LedgerSectionDetailMapping
{
    public static readonly Expression<Func<LedgerSection, LedgerSectionDto>> Projection = s =>
        new LedgerSectionDto
        {
            Id = s.Id,
            Name = s.Name,
            ProjectId = s.ProjectId,
            ProjectName = s.Project != null ? s.Project.Name : null,
            SortOrder = s.SortOrder,
            RowCount = s.Rows.Count,
            Rows = s.Rows
                .OrderBy(r => r.SortOrder)
                .Select(r => new LedgerRowDto
                {
                    Id = r.Id,
                    Label = r.Label,
                    EmployeeId = r.EmployeeId,
                    EmployeeName = r.Employee != null
                        ? r.Employee.FirstName + " " + r.Employee.LastName
                        : null,
                    VehicleId = r.VehicleId,
                    VehicleName = r.Vehicle != null
                        ? r.Vehicle.Brand + " " + r.Vehicle.Model + " (" + r.Vehicle.RegistrationNumber + ")"
                        : null,
                    ToolId = r.ToolId,
                    ToolName = r.Tool != null ? r.Tool.Name : null,
                    MaterialId = r.MaterialId,
                    MaterialName = r.Material != null ? r.Material.Name : null,
                    PromotedGeneralExpenseId = r.PromotedGeneralExpenseId,
                    PromotedAccommodationRateId = r.PromotedAccommodationRateId,
                    SortOrder = r.SortOrder,
                    ColorTag = r.ColorTag,
                    Cells = r.Cells
                        .Select(cell => new LedgerCellDto
                        {
                            Id = cell.Id,
                            ColumnId = cell.ColumnId,
                            Value = cell.Value,
                            ColorTag = cell.ColorTag,
                        })
                        .ToList(),
                })
                .ToList(),
        };

    private static readonly Func<LedgerSection, LedgerSectionDto> Compiled = Projection.Compile();

    public static LedgerSectionDto ToDto(LedgerSection section) => Compiled(section);
}

/// <summary>
/// Turns a <see cref="LedgerCell"/>'s always-string value into a number, matching the
/// frontend's own parsing (comma or dot as the decimal separator; blank or
/// unparseable text counts as zero — the real sheet mixes blanks, "0" and text
/// in nominally-numeric columns, and nothing here should ever reject that).
/// </summary>
public static class LedgerCellMath
{
    public static decimal ParseNumeric(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        var normalized = value.Replace(',', '.');

        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0m;
    }
}

/// <summary>How a single freshly-written <see cref="LedgerSummaryBox"/> becomes its DTO, value included.</summary>
public static class LedgerSummaryBoxMapping
{
    public static async Task<LedgerSummaryBoxDto> ToDtoAsync(
        IApplicationDbContext context,
        LedgerSummaryBox box,
        CancellationToken cancellationToken)
    {
        string? columnName = null;
        decimal value;

        if (box.SourceColumnId is { } columnId)
        {
            columnName = await context.LedgerColumns
                .Where(c => c.Id == columnId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken);

            var cellValues = await context.LedgerCells
                .Where(cell => cell.ColumnId == columnId && cell.Row.Section.LedgerId == box.LedgerId)
                .Select(cell => cell.Value)
                .ToListAsync(cancellationToken);

            value = cellValues.Sum(LedgerCellMath.ParseNumeric);
        }
        else
        {
            value = box.ManualValue ?? 0m;
        }

        return new LedgerSummaryBoxDto
        {
            Id = box.Id,
            Label = box.Label,
            SourceColumnId = box.SourceColumnId,
            SourceColumnName = columnName,
            ManualValue = box.ManualValue,
            Sign = box.Sign,
            Color = box.Color,
            SortOrder = box.SortOrder,
            Value = value,
        };
    }
}

/// <summary>
/// How a <see cref="Ledger"/> becomes its <see cref="LedgerDetailDto"/> shell — columns and section
/// headers (with row counts), but no rows or cells. What <c>GetLedgerById</c> returns: with a real
/// month's worth of data (dozens of sections, hundreds of rows), fetching every cell up front is
/// wasted work when sections open one at a time — <see cref="LedgerSectionDetailMapping"/> fetches a
/// single section's rows on demand instead.
/// </summary>
public static class LedgerShellMapping
{
    public static readonly Expression<Func<Ledger, LedgerDetailDto>> Projection = ledger =>
        new LedgerDetailDto
        {
            Id = ledger.Id,
            Name = ledger.Name,
            Year = ledger.Year,
            Month = ledger.Month,
            Note = ledger.Note,
            CreatedByName = ledger.CreatedByUser != null ? ledger.CreatedByUser.Email : null,
            CreatedAt = ledger.CreatedAt,
            UpdatedAt = ledger.UpdatedAt,
            Columns = ledger.Columns
                .OrderBy(c => c.SortOrder)
                .Select(c => new LedgerColumnDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    DataType = c.DataType.ToString(),
                    SourceMetric = c.SourceMetric != null ? c.SourceMetric.ToString() : null,
                    SortOrder = c.SortOrder,
                })
                .ToList(),
            Sections = ledger.Sections
                .OrderBy(s => s.SortOrder)
                .Select(s => new LedgerSectionDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    ProjectId = s.ProjectId,
                    ProjectName = s.Project != null ? s.Project.Name : null,
                    SortOrder = s.SortOrder,
                    RowCount = s.Rows.Count,
                    Rows = Array.Empty<LedgerRowDto>(),
                })
                .ToList(),
        };

    private static readonly Func<Ledger, LedgerDetailDto> Compiled = Projection.Compile();

    public static LedgerDetailDto ToDto(Ledger ledger) => Compiled(ledger);
}
