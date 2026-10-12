using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PeriodMonth",
                table: "book_entries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PeriodYear",
                table: "book_entries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecurringExpenseId",
                table: "book_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "recurring_expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recurring_expenses", x => x.Id);
                    table.CheckConstraint("ck_recurring_expenses_amount_positive", "\"Amount\" > 0");
                    table.CheckConstraint("ck_recurring_expenses_day", "\"DayOfMonth\" BETWEEN 1 AND 28");
                    table.ForeignKey(
                        name: "FK_recurring_expenses_book_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "book_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurring_expenses_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recurring_expenses_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_book_entries_RecurringExpenseId_PeriodYear_PeriodMonth",
                table: "book_entries",
                columns: new[] { "RecurringExpenseId", "PeriodYear", "PeriodMonth" },
                unique: true,
                filter: "\"RecurringExpenseId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_expenses_BranchId",
                table: "recurring_expenses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_expenses_CategoryId",
                table: "recurring_expenses",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_expenses_IsActive",
                table: "recurring_expenses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_recurring_expenses_ProjectId",
                table: "recurring_expenses",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_book_entries_recurring_expenses_RecurringExpenseId",
                table: "book_entries",
                column: "RecurringExpenseId",
                principalTable: "recurring_expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_book_entries_recurring_expenses_RecurringExpenseId",
                table: "book_entries");

            migrationBuilder.DropTable(
                name: "recurring_expenses");

            migrationBuilder.DropIndex(
                name: "IX_book_entries_RecurringExpenseId_PeriodYear_PeriodMonth",
                table: "book_entries");

            migrationBuilder.DropColumn(
                name: "PeriodMonth",
                table: "book_entries");

            migrationBuilder.DropColumn(
                name: "PeriodYear",
                table: "book_entries");

            migrationBuilder.DropColumn(
                name: "RecurringExpenseId",
                table: "book_entries");
        }
    }
}
