using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMain",
                table: "branches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LogoContentType",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoDarkContentType",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoDarkStorageKey",
                table: "branches",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoStorageKey",
                table: "branches",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LogoUpdatedAt",
                table: "branches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_branches_IsMain",
                table: "branches",
                column: "IsMain",
                unique: true,
                filter: "\"IsMain\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_branches_IsMain",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "IsMain",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "LogoContentType",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "LogoDarkContentType",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "LogoDarkStorageKey",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "LogoStorageKey",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "LogoUpdatedAt",
                table: "branches");
        }
    }
}
