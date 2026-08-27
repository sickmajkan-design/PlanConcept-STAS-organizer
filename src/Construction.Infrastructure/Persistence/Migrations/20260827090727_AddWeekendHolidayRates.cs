using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeekendHolidayRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HolidayHourlyRate",
                table: "employee_rates",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeekendHourlyRate",
                table: "employee_rates",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "public_holidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_public_holidays", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_employee_rates_holiday_positive",
                table: "employee_rates",
                sql: "\"HolidayHourlyRate\" IS NULL OR \"HolidayHourlyRate\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_employee_rates_weekend_positive",
                table: "employee_rates",
                sql: "\"WeekendHourlyRate\" IS NULL OR \"WeekendHourlyRate\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_public_holidays_Date",
                table: "public_holidays",
                column: "Date",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "public_holidays");

            migrationBuilder.DropCheckConstraint(
                name: "ck_employee_rates_holiday_positive",
                table: "employee_rates");

            migrationBuilder.DropCheckConstraint(
                name: "ck_employee_rates_weekend_positive",
                table: "employee_rates");

            migrationBuilder.DropColumn(
                name: "HolidayHourlyRate",
                table: "employee_rates");

            migrationBuilder.DropColumn(
                name: "WeekendHourlyRate",
                table: "employee_rates");
        }
    }
}
