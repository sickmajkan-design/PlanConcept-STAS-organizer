using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerSourcedColumnsAndPromotion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialId",
                table: "ledger_rows",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PromotedAccommodationRateId",
                table: "ledger_rows",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PromotedGeneralExpenseId",
                table: "ledger_rows",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ToolId",
                table: "ledger_rows",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleId",
                table: "ledger_rows",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceMetric",
                table: "ledger_columns",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ledger_rows_MaterialId",
                table: "ledger_rows",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ledger_rows_PromotedAccommodationRateId",
                table: "ledger_rows",
                column: "PromotedAccommodationRateId");

            migrationBuilder.CreateIndex(
                name: "IX_ledger_rows_PromotedGeneralExpenseId",
                table: "ledger_rows",
                column: "PromotedGeneralExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_ledger_rows_ToolId",
                table: "ledger_rows",
                column: "ToolId");

            migrationBuilder.CreateIndex(
                name: "IX_ledger_rows_VehicleId",
                table: "ledger_rows",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ledger_rows_accommodation_rates_PromotedAccommodationRateId",
                table: "ledger_rows",
                column: "PromotedAccommodationRateId",
                principalTable: "accommodation_rates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ledger_rows_general_expenses_PromotedGeneralExpenseId",
                table: "ledger_rows",
                column: "PromotedGeneralExpenseId",
                principalTable: "general_expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ledger_rows_materials_MaterialId",
                table: "ledger_rows",
                column: "MaterialId",
                principalTable: "materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ledger_rows_tools_ToolId",
                table: "ledger_rows",
                column: "ToolId",
                principalTable: "tools",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ledger_rows_vehicles_VehicleId",
                table: "ledger_rows",
                column: "VehicleId",
                principalTable: "vehicles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ledger_rows_accommodation_rates_PromotedAccommodationRateId",
                table: "ledger_rows");

            migrationBuilder.DropForeignKey(
                name: "FK_ledger_rows_general_expenses_PromotedGeneralExpenseId",
                table: "ledger_rows");

            migrationBuilder.DropForeignKey(
                name: "FK_ledger_rows_materials_MaterialId",
                table: "ledger_rows");

            migrationBuilder.DropForeignKey(
                name: "FK_ledger_rows_tools_ToolId",
                table: "ledger_rows");

            migrationBuilder.DropForeignKey(
                name: "FK_ledger_rows_vehicles_VehicleId",
                table: "ledger_rows");

            migrationBuilder.DropIndex(
                name: "IX_ledger_rows_MaterialId",
                table: "ledger_rows");

            migrationBuilder.DropIndex(
                name: "IX_ledger_rows_PromotedAccommodationRateId",
                table: "ledger_rows");

            migrationBuilder.DropIndex(
                name: "IX_ledger_rows_PromotedGeneralExpenseId",
                table: "ledger_rows");

            migrationBuilder.DropIndex(
                name: "IX_ledger_rows_ToolId",
                table: "ledger_rows");

            migrationBuilder.DropIndex(
                name: "IX_ledger_rows_VehicleId",
                table: "ledger_rows");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                table: "ledger_rows");

            migrationBuilder.DropColumn(
                name: "PromotedAccommodationRateId",
                table: "ledger_rows");

            migrationBuilder.DropColumn(
                name: "PromotedGeneralExpenseId",
                table: "ledger_rows");

            migrationBuilder.DropColumn(
                name: "ToolId",
                table: "ledger_rows");

            migrationBuilder.DropColumn(
                name: "VehicleId",
                table: "ledger_rows");

            migrationBuilder.DropColumn(
                name: "SourceMetric",
                table: "ledger_columns");
        }
    }
}
