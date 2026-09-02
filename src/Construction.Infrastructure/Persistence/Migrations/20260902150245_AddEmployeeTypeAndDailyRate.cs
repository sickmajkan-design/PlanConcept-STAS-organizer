using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeTypeAndDailyRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_employee_rates_positive",
                table: "employee_rates");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "employees",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<decimal>(
                name: "HourlyRate",
                table: "employee_rates",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyRate",
                table: "employee_rates",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RateType",
                table: "employee_rates",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_employees_Type",
                table: "employees",
                column: "Type");

            migrationBuilder.AddCheckConstraint(
                name: "ck_employee_rates_daily_positive",
                table: "employee_rates",
                sql: "\"DailyRate\" IS NULL OR \"DailyRate\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_employee_rates_positive",
                table: "employee_rates",
                sql: "\"HourlyRate\" IS NULL OR \"HourlyRate\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_employee_rates_type_matches_fields",
                table: "employee_rates",
                sql: "(\"RateType\" = 1 AND \"HourlyRate\" IS NOT NULL AND \"DailyRate\" IS NULL) OR (\"RateType\" = 2 AND \"DailyRate\" IS NOT NULL AND \"HourlyRate\" IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_employees_Type",
                table: "employees");

            migrationBuilder.DropCheckConstraint(
                name: "ck_employee_rates_daily_positive",
                table: "employee_rates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_employee_rates_positive",
                table: "employee_rates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_employee_rates_type_matches_fields",
                table: "employee_rates");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "DailyRate",
                table: "employee_rates");

            migrationBuilder.DropColumn(
                name: "RateType",
                table: "employee_rates");

            migrationBuilder.AlterColumn<decimal>(
                name: "HourlyRate",
                table: "employee_rates",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_employee_rates_positive",
                table: "employee_rates",
                sql: "\"HourlyRate\" > 0");
        }
    }
}
