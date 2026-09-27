using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSignedTimesheets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.AddColumn<Guid>(
                name: "SignedTimesheetId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "signed_timesheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    IsoWeek = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signed_timesheets", x => x.Id);
                    table.CheckConstraint("ck_signed_timesheets_iso_week", "\"IsoWeek\" BETWEEN 1 AND 53");
                    table.ForeignKey(
                        name: "FK_signed_timesheets_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_SignedTimesheetId",
                table: "attachments",
                column: "SignedTimesheetId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"MaterialMovementId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"EmployeeRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"FinanceEntryId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"GeneralExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"RefundId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"SignedTimesheetId\" IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_signed_timesheets_ProjectId_Year_IsoWeek",
                table: "signed_timesheets",
                columns: new[] { "ProjectId", "Year", "IsoWeek" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_signed_timesheets_SignedTimesheetId",
                table: "attachments",
                column: "SignedTimesheetId",
                principalTable: "signed_timesheets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attachments_signed_timesheets_SignedTimesheetId",
                table: "attachments");

            migrationBuilder.DropTable(
                name: "signed_timesheets");

            migrationBuilder.DropIndex(
                name: "IX_attachments_SignedTimesheetId",
                table: "attachments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "SignedTimesheetId",
                table: "attachments");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"MaterialMovementId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"EmployeeRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"FinanceEntryId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"GeneralExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"RefundId\" IS NULL THEN 0 ELSE 1 END) = 1");
        }
    }
}
