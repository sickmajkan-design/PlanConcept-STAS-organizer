using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialMinimumQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinimumQuantity",
                table: "materials",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_materials_minimum_not_negative",
                table: "materials",
                sql: "\"MinimumQuantity\" IS NULL OR \"MinimumQuantity\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_materials_minimum_not_negative",
                table: "materials");

            migrationBuilder.DropColumn(
                name: "MinimumQuantity",
                table: "materials");
        }
    }
}
