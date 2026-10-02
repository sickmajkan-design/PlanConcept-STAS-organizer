using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "ledgers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ledgers_BranchId",
                table: "ledgers",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_ledgers_branches_BranchId",
                table: "ledgers",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ledgers_branches_BranchId",
                table: "ledgers");

            migrationBuilder.DropIndex(
                name: "IX_ledgers_BranchId",
                table: "ledgers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ledgers");
        }
    }
}
