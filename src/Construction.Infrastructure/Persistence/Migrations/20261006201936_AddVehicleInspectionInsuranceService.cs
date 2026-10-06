using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleInspectionInsuranceService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "InsuranceValidUntil",
                table: "vehicles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NextServiceDue",
                table: "vehicles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "TechnicalInspectionValidUntil",
                table: "vehicles",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsuranceValidUntil",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "NextServiceDue",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "TechnicalInspectionValidUntil",
                table: "vehicles");
        }
    }
}
