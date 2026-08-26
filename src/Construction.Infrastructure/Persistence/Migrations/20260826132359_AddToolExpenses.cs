using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddToolExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.AddColumn<Guid>(
                name: "ToolExpenseId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tool_expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tool_expenses", x => x.Id);
                    table.CheckConstraint("ck_tool_expenses_amount_not_negative", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_tool_expenses_tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tool_expenses_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_ToolExpenseId",
                table: "attachments",
                column: "ToolExpenseId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"MaterialMovementId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"EmployeeRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"FinanceEntryId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolExpenseId\" IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_tool_expenses_Kind",
                table: "tool_expenses",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_tool_expenses_RecordedByUserId",
                table: "tool_expenses",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tool_expenses_ToolId_OccurredOn",
                table: "tool_expenses",
                columns: new[] { "ToolId", "OccurredOn" });

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_tool_expenses_ToolExpenseId",
                table: "attachments",
                column: "ToolExpenseId",
                principalTable: "tool_expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attachments_tool_expenses_ToolExpenseId",
                table: "attachments");

            migrationBuilder.DropTable(
                name: "tool_expenses");

            migrationBuilder.DropIndex(
                name: "IX_attachments_ToolExpenseId",
                table: "attachments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "ToolExpenseId",
                table: "attachments");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"MaterialMovementId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"EmployeeRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"FinanceEntryId\" IS NULL THEN 0 ELSE 1 END) = 1");
        }
    }
}
