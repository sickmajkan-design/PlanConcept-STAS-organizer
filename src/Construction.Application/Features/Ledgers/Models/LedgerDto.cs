using System.Linq.Expressions;
using Construction.Domain.Entities;

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
    public Guid ColumnId { get; init; }

    public string? Value { get; init; }
}

public class LedgerRowDto
{
    public Guid Id { get; init; }

    public string Label { get; init; } = null!;

    public Guid? EmployeeId { get; init; }

    public string? EmployeeName { get; init; }

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

    public IReadOnlyCollection<LedgerRowDto> Rows { get; init; } = Array.Empty<LedgerRowDto>();
}

public class LedgerColumnDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string DataType { get; init; } = null!;

    public int SortOrder { get; init; }
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

/// <summary>How a <see cref="Ledger"/> becomes its full <see cref="LedgerDetailDto"/>, columns/sections/rows/cells and all.</summary>
public static class LedgerDetailMapping
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
                            SortOrder = r.SortOrder,
                            Cells = r.Cells
                                .Select(cell => new LedgerCellDto
                                {
                                    ColumnId = cell.ColumnId,
                                    Value = cell.Value,
                                })
                                .ToList(),
                        })
                        .ToList(),
                })
                .ToList(),
        };

    private static readonly Func<Ledger, LedgerDetailDto> Compiled = Projection.Compile();

    public static LedgerDetailDto ToDto(Ledger ledger) => Compiled(ledger);
}
