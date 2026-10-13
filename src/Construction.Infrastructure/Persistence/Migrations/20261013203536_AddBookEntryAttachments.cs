using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookEntryAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.AddColumn<Guid>(
                name: "BookEntryId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_attachments_BookEntryId",
                table: "attachments",
                column: "BookEntryId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"MaterialMovementId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"EmployeeRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"FinanceEntryId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"GeneralExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"RefundId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"SignedTimesheetId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"BookEntryId\" IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_book_entries_BookEntryId",
                table: "attachments",
                column: "BookEntryId",
                principalTable: "book_entries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attachments_book_entries_BookEntryId",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_attachments_BookEntryId",
                table: "attachments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "BookEntryId",
                table: "attachments");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"MaterialMovementId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"EmployeeRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"FinanceEntryId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"GeneralExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"RefundId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"SignedTimesheetId\" IS NULL THEN 0 ELSE 1 END) = 1");
        }
    }
}
