using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectBudgetAlert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BudgetAlertBasis",
                table: "projects",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BudgetWarnPercent",
                table: "projects",
                type: "integer",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_projects_budget_warn_percent_range",
                table: "projects",
                sql: "\"BudgetWarnPercent\" IS NULL OR (\"BudgetWarnPercent\" >= 1 AND \"BudgetWarnPercent\" <= 99)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_projects_budget_warn_percent_range",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "BudgetAlertBasis",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "BudgetWarnPercent",
                table: "projects");
        }
    }
}
