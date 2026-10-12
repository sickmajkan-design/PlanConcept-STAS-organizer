using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWagePayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayGroup",
                table: "ledger_rows",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Workers");

            migrationBuilder.CreateTable(
                name: "wage_payouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Group = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wage_payouts", x => x.Id);
                    table.CheckConstraint("ck_wage_payouts_amount_positive", "\"Amount\" > 0");
                    table.CheckConstraint("ck_wage_payouts_month", "\"Month\" BETWEEN 1 AND 12");
                    table.ForeignKey(
                        name: "FK_wage_payouts_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_wage_payouts_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_wage_payouts_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wage_payouts_BranchId",
                table: "wage_payouts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_wage_payouts_EmployeeId",
                table: "wage_payouts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_wage_payouts_RecordedByUserId",
                table: "wage_payouts",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_wage_payouts_Year_Month_Group",
                table: "wage_payouts",
                columns: new[] { "Year", "Month", "Group" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wage_payouts");

            migrationBuilder.DropColumn(
                name: "PayGroup",
                table: "ledger_rows");
        }
    }
}
