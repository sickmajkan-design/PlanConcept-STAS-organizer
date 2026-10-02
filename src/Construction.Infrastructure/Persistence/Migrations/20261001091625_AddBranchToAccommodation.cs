using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchToAccommodation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "accommodations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_accommodations_BranchId",
                table: "accommodations",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_accommodations_branches_BranchId",
                table: "accommodations",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_accommodations_branches_BranchId",
                table: "accommodations");

            migrationBuilder.DropIndex(
                name: "IX_accommodations_BranchId",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "accommodations");
        }
    }
}
