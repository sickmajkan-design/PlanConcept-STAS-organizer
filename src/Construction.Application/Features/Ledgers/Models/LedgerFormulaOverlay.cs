namespace Construction.Application.Features.Ledgers.Models;

/// <summary>
/// Adds a section's computed cells on top of what is stored, the way
/// <see cref="LedgerSourcedValues"/> adds sourced ones.
/// </summary>
public static class LedgerFormulaOverlay
{
    public static LedgerSectionDto Apply(
        LedgerSectionDto section,
        LedgerCalculator calculator,
        IReadOnlyDictionary<Guid, IReadOnlyDictionary<Guid, decimal>>? autoValues = null)
    {
        if (!calculator.HasFormulas || section.Rows.Count == 0)
        {
            return section;
        }

        var rows = section.Rows.Select(row =>
        {
            // Sourced cells are already among the row's cells, so a formula
            // can use them as inputs exactly like a typed value.
            var stored = row.Cells
                .Where(c => c.ColumnId != Guid.Empty)
                .GroupBy(c => c.ColumnId)
                .ToDictionary(g => g.Key, g => g.First().Value);

            var computed = calculator.ComputeRow(
                stored,
                autoValues is not null && autoValues.TryGetValue(row.Id, out var auto) ? auto : null);

            var cells = row.Cells
                .Where(c => !computed.ContainsKey(c.ColumnId))
                .Concat(computed.Select(kv =>
                {
                    var existing = row.Cells.FirstOrDefault(c => c.ColumnId == kv.Key);

                    return new LedgerCellDto
                    {
                        Id = kv.Value.IsTyped ? existing?.Id : null,
                        ColumnId = kv.Key,
                        Value = LedgerColumnTotals.Format(kv.Value.Value),
                        ColorTag = existing?.ColorTag,
                        IsComputed = !kv.Value.IsTyped,
                        IsOverride = kv.Value.IsOverride,
                    };
                }))
                .ToList();

            return new LedgerRowDto
            {
                Id = row.Id,
                Label = row.Label,
                EmployeeId = row.EmployeeId,
                EmployeeName = row.EmployeeName,
                VehicleId = row.VehicleId,
                VehicleName = row.VehicleName,
                ToolId = row.ToolId,
                ToolName = row.ToolName,
                MaterialId = row.MaterialId,
                MaterialName = row.MaterialName,
                PromotedGeneralExpenseId = row.PromotedGeneralExpenseId,
                PromotedAccommodationRateId = row.PromotedAccommodationRateId,
                ColorTag = row.ColorTag,
                SortOrder = row.SortOrder,
                Cells = cells,
            };
        }).ToList();

        return new LedgerSectionDto
        {
            Id = section.Id,
            Name = section.Name,
            ProjectId = section.ProjectId,
            ProjectName = section.ProjectName,
            SortOrder = section.SortOrder,
            RowCount = section.RowCount,
            Rows = rows,
        };
    }
}
