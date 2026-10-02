using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchToRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "vehicles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "tools",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "general_expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "finance_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BranchId",
                table: "company_revenues",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_BranchId",
                table: "vehicles",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_tools_BranchId",
                table: "tools",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_general_expenses_BranchId",
                table: "general_expenses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_finance_entries_BranchId",
                table: "finance_entries",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_company_revenues_BranchId",
                table: "company_revenues",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_company_revenues_branches_BranchId",
                table: "company_revenues",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_finance_entries_branches_BranchId",
                table: "finance_entries",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_general_expenses_branches_BranchId",
                table: "general_expenses",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tools_branches_BranchId",
                table: "tools",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_vehicles_branches_BranchId",
                table: "vehicles",
                column: "BranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_company_revenues_branches_BranchId",
                table: "company_revenues");

            migrationBuilder.DropForeignKey(
                name: "FK_finance_entries_branches_BranchId",
                table: "finance_entries");

            migrationBuilder.DropForeignKey(
                name: "FK_general_expenses_branches_BranchId",
                table: "general_expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_tools_branches_BranchId",
                table: "tools");

            migrationBuilder.DropForeignKey(
                name: "FK_vehicles_branches_BranchId",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "IX_vehicles_BranchId",
                table: "vehicles");

            migrationBuilder.DropIndex(
                name: "IX_tools_BranchId",
                table: "tools");

            migrationBuilder.DropIndex(
                name: "IX_general_expenses_BranchId",
                table: "general_expenses");

            migrationBuilder.DropIndex(
                name: "IX_finance_entries_BranchId",
                table: "finance_entries");

            migrationBuilder.DropIndex(
                name: "IX_company_revenues_BranchId",
                table: "company_revenues");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "tools");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "general_expenses");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "finance_entries");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "company_revenues");
        }
    }
}
