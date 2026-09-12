using Construction.Application.Common.Exceptions;
using Construction.Application.Features.Ledgers.Commands;
using Construction.Application.Features.Ledgers.Commands.CreateLedger;
using Construction.Application.Features.Ledgers.Commands.SetLedgerCell;
using Construction.Application.Features.Ledgers.Commands.SetLedgerColor;
using Construction.Application.Features.Ledgers.Models;
using Construction.Application.Features.Ledgers.Queries.GetLedgerSectionRows;
using Construction.Domain.Entities;
using Construction.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Construction.IntegrationTests;

/// <summary>
/// Coloring a row or cell is the SuperAdmin's own free-form highlighting —
/// the in-app equivalent of fill color in the spreadsheet this feature
/// replaces. The one thing worth a database round trip here: a cell's color
/// must never disturb its value, and vice versa, since the grid commits them
/// through two separate endpoints on purpose.
/// </summary>
[Collection(DatabaseCollection.Name)]
public class LedgerColorTests : IntegrationTestBase
{
    public LedgerColorTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private static void ActAs(TestScope scope, User user) =>
        scope.CurrentUser.SignInAs(user.Id, user.Role, null, user.Email);

    private async Task<(Guid RowId, Guid ColumnId, User SuperAdmin)> SeedRowAndColumnAsync()
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        var ledger = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new CreateLedgerCommand { Name = "Test month", Year = 2026, Month = 9 });
        });

        var column = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new AddLedgerColumnCommand
            {
                LedgerId = ledger.Id,
                Name = "Amount",
                DataType = LedgerColumnDataType.Number
            });
        });

        var section = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new AddLedgerSectionCommand { LedgerId = ledger.Id, Name = "Block" });
        });

        var row = await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new AddLedgerRowCommand { SectionId = section.Id, Label = "Worker" });
        });

        return (row.Id, column.Id, superAdmin);
    }

    [Fact]
    public async Task Coloring_a_row_shows_up_when_the_section_is_read_back()
    {
        var (rowId, _, superAdmin) = await SeedRowAndColumnAsync();

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new SetLedgerRowColorCommand { RowId = rowId, Color = "#F6C6C6" });
        });

        var row = await GetRowAsync(rowId, superAdmin);

        Assert.Equal("#F6C6C6", row.ColorTag);
    }

    [Fact]
    public async Task Clearing_a_rows_color_sets_it_back_to_null()
    {
        var (rowId, _, superAdmin) = await SeedRowAndColumnAsync();

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new SetLedgerRowColorCommand { RowId = rowId, Color = "#F6C6C6" });
        });

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new SetLedgerRowColorCommand { RowId = rowId, Color = null });
        });

        var row = await GetRowAsync(rowId, superAdmin);

        Assert.Null(row.ColorTag);
    }

    [Fact]
    public async Task Coloring_a_cell_that_already_has_a_value_leaves_the_value_alone()
    {
        var (rowId, columnId, superAdmin) = await SeedRowAndColumnAsync();

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new SetLedgerCellCommand { RowId = rowId, ColumnId = columnId, Value = "42" });
        });

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new SetLedgerCellColorCommand
            {
                RowId = rowId,
                ColumnId = columnId,
                Color = "#C7E8B9"
            });
        });

        var row = await GetRowAsync(rowId, superAdmin);
        var cell = Assert.Single(row.Cells);

        Assert.Equal("42", cell.Value);
        Assert.Equal("#C7E8B9", cell.ColorTag);
    }

    [Fact]
    public async Task Coloring_a_cell_that_has_no_value_yet_creates_it_with_a_null_value()
    {
        // Picking a color for a still-empty cell is a normal thing to want —
        // the color-only endpoint must not require a value to already exist.
        var (rowId, columnId, superAdmin) = await SeedRowAndColumnAsync();

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new SetLedgerCellColorCommand
            {
                RowId = rowId,
                ColumnId = columnId,
                Color = "#F9DFA0"
            });
        });

        var row = await GetRowAsync(rowId, superAdmin);
        var cell = Assert.Single(row.Cells);

        Assert.Null(cell.Value);
        Assert.Equal("#F9DFA0", cell.ColorTag);
    }

    [Fact]
    public async Task Setting_a_cells_value_leaves_its_color_alone()
    {
        var (rowId, columnId, superAdmin) = await SeedRowAndColumnAsync();

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new SetLedgerCellColorCommand
            {
                RowId = rowId,
                ColumnId = columnId,
                Color = "#B9DDF2"
            });
        });

        await InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new SetLedgerCellCommand { RowId = rowId, ColumnId = columnId, Value = "100" });
        });

        var row = await GetRowAsync(rowId, superAdmin);
        var cell = Assert.Single(row.Cells);

        Assert.Equal("100", cell.Value);
        Assert.Equal("#B9DDF2", cell.ColorTag);
    }

    [Fact]
    public async Task Coloring_a_row_that_does_not_exist_reports_not_found()
    {
        var superAdmin = await InScope(scope => TestData.SeedUserAsync(scope, UserRole.SuperAdmin));

        await Assert.ThrowsAsync<NotFoundException>(() => InScope(scope =>
        {
            ActAs(scope, superAdmin);
            return scope.Send(new SetLedgerRowColorCommand { RowId = Guid.NewGuid(), Color = "#F6C6C6" });
        }));
    }

    /// <summary>The row lives inside a section; fetch the section and pick it out
    /// rather than adding a single-row query nothing else needs.</summary>
    private async Task<LedgerRowDto> GetRowAsync(Guid rowId, User actor)
    {
        var rowInfo = await InScope(scope => scope.Db.LedgerRows
            .Where(r => r.Id == rowId)
            .Select(r => new { r.SectionId, LedgerId = r.Section.LedgerId })
            .SingleAsync());

        var section = await InScope(scope =>
        {
            ActAs(scope, actor);
            return scope.Send(new GetLedgerSectionRowsQuery(rowInfo.LedgerId, rowInfo.SectionId));
        });

        return section.Rows.Single(r => r.Id == rowId);
    }
}
