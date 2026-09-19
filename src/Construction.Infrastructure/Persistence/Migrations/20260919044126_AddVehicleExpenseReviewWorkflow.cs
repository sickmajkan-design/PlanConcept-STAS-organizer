using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleExpenseReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "vehicle_expenses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "vehicle_expenses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "vehicle_expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "vehicle_expenses",
                type: "integer",
                nullable: false,
                // 1 = VehicleExpenseStatus.Pending. Every row that predates
                // this column backfills to it via this same default, which is
                // the point: 0 is not a value the enum defines at all.
                defaultValue: 1);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "vehicle_expenses",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_expenses_ReviewedByUserId",
                table: "vehicle_expenses",
                column: "ReviewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_vehicle_expenses_users_ReviewedByUserId",
                table: "vehicle_expenses",
                column: "ReviewedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vehicle_expenses_users_ReviewedByUserId",
                table: "vehicle_expenses");

            migrationBuilder.DropIndex(
                name: "IX_vehicle_expenses_ReviewedByUserId",
                table: "vehicle_expenses");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "vehicle_expenses");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "vehicle_expenses");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "vehicle_expenses");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "vehicle_expenses");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "vehicle_expenses");
        }
    }
}
