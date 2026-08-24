using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleAssignedProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedProjectId",
                table: "vehicles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_AssignedProjectId",
                table: "vehicles",
                column: "AssignedProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_vehicles_projects_AssignedProjectId",
                table: "vehicles",
                column: "AssignedProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vehicles_projects_AssignedProjectId",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "IX_vehicles_AssignedProjectId",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "AssignedProjectId",
                table: "vehicles");
        }
    }
}
