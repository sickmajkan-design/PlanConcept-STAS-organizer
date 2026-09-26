using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.AddColumn<Guid>(
                name: "RefundId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "refunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PayrollYear = table.Column<int>(type: "integer", nullable: true),
                    PayrollMonth = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refunds", x => x.Id);
                    table.CheckConstraint("ck_refunds_amount_positive", "\"Amount\" > 0");
                    table.CheckConstraint("ck_refunds_payroll_month", "\"PayrollMonth\" IS NULL OR \"PayrollMonth\" BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "FK_refunds_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refunds_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_refunds_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_refunds_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_RefundId",
                table: "attachments",
                column: "RefundId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"MaterialMovementId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"EmployeeRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"FinanceEntryId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"GeneralExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"RefundId\" IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_EmployeeId_PayrollYear_PayrollMonth",
                table: "refunds",
                columns: new[] { "EmployeeId", "PayrollYear", "PayrollMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_refunds_ProjectId",
                table: "refunds",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_RequestedByUserId",
                table: "refunds",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_ReviewedByUserId",
                table: "refunds",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_refunds_Status",
                table: "refunds",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_refunds_RefundId",
                table: "attachments",
                column: "RefundId",
                principalTable: "refunds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attachments_refunds_RefundId",
                table: "attachments");

            migrationBuilder.DropTable(
                name: "refunds");

            migrationBuilder.DropIndex(
                name: "IX_attachments_RefundId",
                table: "attachments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "RefundId",
                table: "attachments");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"MaterialMovementId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"EmployeeRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"FinanceEntryId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"GeneralExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolRentalRateId\" IS NULL THEN 0 ELSE 1 END) = 1");
        }
    }
}
