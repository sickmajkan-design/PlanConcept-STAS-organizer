using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOvertimeAndTravelRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeHourlyRate",
                table: "employee_rates",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TravelHourlyRate",
                table: "employee_rates",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_employee_rates_overtime_positive",
                table: "employee_rates",
                sql: "\"OvertimeHourlyRate\" IS NULL OR \"OvertimeHourlyRate\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_employee_rates_travel_positive",
                table: "employee_rates",
                sql: "\"TravelHourlyRate\" IS NULL OR \"TravelHourlyRate\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_employee_rates_overtime_positive",
                table: "employee_rates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_employee_rates_travel_positive",
                table: "employee_rates");

            migrationBuilder.DropColumn(
                name: "OvertimeHourlyRate",
                table: "employee_rates");

            migrationBuilder.DropColumn(
                name: "TravelHourlyRate",
                table: "employee_rates");
        }
    }
}
