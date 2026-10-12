using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "ledgers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockedByUserId",
                table: "ledgers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultJson",
                table: "ledgers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnlockNote",
                table: "ledgers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ledgers_LockedByUserId",
                table: "ledgers",
                column: "LockedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ledgers_users_LockedByUserId",
                table: "ledgers",
                column: "LockedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ledgers_users_LockedByUserId",
                table: "ledgers");

            migrationBuilder.DropIndex(
                name: "IX_ledgers_LockedByUserId",
                table: "ledgers");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "ledgers");

            migrationBuilder.DropColumn(
                name: "LockedByUserId",
                table: "ledgers");

            migrationBuilder.DropColumn(
                name: "ResultJson",
                table: "ledgers");

            migrationBuilder.DropColumn(
                name: "UnlockNote",
                table: "ledgers");
        }
    }
}
