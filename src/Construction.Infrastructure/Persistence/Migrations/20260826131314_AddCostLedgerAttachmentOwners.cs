using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCostLedgerAttachmentOwners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.AddColumn<Guid>(
                name: "EmployeeRateId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FinanceEntryId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialMovementId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleExpenseId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_attachments_EmployeeRateId",
                table: "attachments",
                column: "EmployeeRateId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_FinanceEntryId",
                table: "attachments",
                column: "FinanceEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_MaterialMovementId",
                table: "attachments",
                column: "MaterialMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_VehicleExpenseId",
                table: "attachments",
                column: "VehicleExpenseId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"MaterialMovementId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"EmployeeRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"FinanceEntryId\" IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_employee_rates_EmployeeRateId",
                table: "attachments",
                column: "EmployeeRateId",
                principalTable: "employee_rates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_finance_entries_FinanceEntryId",
                table: "attachments",
                column: "FinanceEntryId",
                principalTable: "finance_entries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_material_movements_MaterialMovementId",
                table: "attachments",
                column: "MaterialMovementId",
                principalTable: "material_movements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_vehicle_expenses_VehicleExpenseId",
                table: "attachments",
                column: "VehicleExpenseId",
                principalTable: "vehicle_expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attachments_employee_rates_EmployeeRateId",
                table: "attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_attachments_finance_entries_FinanceEntryId",
                table: "attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_attachments_material_movements_MaterialMovementId",
                table: "attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_attachments_vehicle_expenses_VehicleExpenseId",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_attachments_EmployeeRateId",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_attachments_FinanceEntryId",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_attachments_MaterialMovementId",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_attachments_VehicleExpenseId",
                table: "attachments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "EmployeeRateId",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "FinanceEntryId",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "MaterialMovementId",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "VehicleExpenseId",
                table: "attachments");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END) = 1");
        }
    }
}
