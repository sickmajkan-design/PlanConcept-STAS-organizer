using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HeadEmployeeId",
                table: "branches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentBranchId",
                table: "branches",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "branches",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            // Every unit that exists today stands directly under the company: its path is itself.
            migrationBuilder.Sql("UPDATE branches SET \"Path\" = ',' || \"Id\"::text || ',';");

            migrationBuilder.CreateIndex(
                name: "IX_branches_HeadEmployeeId",
                table: "branches",
                column: "HeadEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_branches_ParentBranchId",
                table: "branches",
                column: "ParentBranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_branches_branches_ParentBranchId",
                table: "branches",
                column: "ParentBranchId",
                principalTable: "branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_branches_employees_HeadEmployeeId",
                table: "branches",
                column: "HeadEmployeeId",
                principalTable: "employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_branches_branches_ParentBranchId",
                table: "branches");

            migrationBuilder.DropForeignKey(
                name: "FK_branches_employees_HeadEmployeeId",
                table: "branches");

            migrationBuilder.DropIndex(
                name: "IX_branches_HeadEmployeeId",
                table: "branches");

            migrationBuilder.DropIndex(
                name: "IX_branches_ParentBranchId",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "HeadEmployeeId",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "ParentBranchId",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "branches");
        }
    }
}
