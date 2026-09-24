using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerColumnFormulas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormulaJson",
                table: "ledger_columns",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemKey",
                table: "ledger_columns",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormulaJson",
                table: "ledger_columns");

            migrationBuilder.DropColumn(
                name: "SystemKey",
                table: "ledger_columns");
        }
    }
}
