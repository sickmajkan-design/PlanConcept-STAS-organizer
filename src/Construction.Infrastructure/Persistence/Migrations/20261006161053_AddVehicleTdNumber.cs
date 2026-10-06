using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleTdNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TdNumber",
                table: "vehicles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_TdNumber",
                table: "vehicles",
                column: "TdNumber",
                unique: true,
                filter: "\"TdNumber\" IS NOT NULL AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vehicles_TdNumber",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "TdNumber",
                table: "vehicles");
        }
    }
}
