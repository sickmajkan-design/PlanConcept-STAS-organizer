using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyReportSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EnteredByUserId",
                table: "weekly_site_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "weekly_site_reports",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SourceNote",
                table: "weekly_site_reports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_weekly_site_reports_EnteredByUserId",
                table: "weekly_site_reports",
                column: "EnteredByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_weekly_site_reports_users_EnteredByUserId",
                table: "weekly_site_reports",
                column: "EnteredByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_weekly_site_reports_users_EnteredByUserId",
                table: "weekly_site_reports");

            migrationBuilder.DropIndex(
                name: "IX_weekly_site_reports_EnteredByUserId",
                table: "weekly_site_reports");

            migrationBuilder.DropColumn(
                name: "EnteredByUserId",
                table: "weekly_site_reports");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "weekly_site_reports");

            migrationBuilder.DropColumn(
                name: "SourceNote",
                table: "weekly_site_reports");
        }
    }
}
