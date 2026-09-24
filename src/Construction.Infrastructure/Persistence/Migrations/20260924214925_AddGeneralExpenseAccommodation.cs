using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneralExpenseAccommodation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccommodationId",
                table: "general_expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_general_expenses_AccommodationId",
                table: "general_expenses",
                column: "AccommodationId");

            migrationBuilder.AddForeignKey(
                name: "FK_general_expenses_accommodations_AccommodationId",
                table: "general_expenses",
                column: "AccommodationId",
                principalTable: "accommodations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_general_expenses_accommodations_AccommodationId",
                table: "general_expenses");

            migrationBuilder.DropIndex(
                name: "IX_general_expenses_AccommodationId",
                table: "general_expenses");

            migrationBuilder.DropColumn(
                name: "AccommodationId",
                table: "general_expenses");
        }
    }
}
