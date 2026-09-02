using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAbsenceEditProposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProposedAt",
                table: "absences",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProposedByEmployee",
                table: "absences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ProposedByUserId",
                table: "absences",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ProposedEndDate",
                table: "absences",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedReason",
                table: "absences",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ProposedStartDate",
                table: "absences",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_absences_ProposedByUserId",
                table: "absences",
                column: "ProposedByUserId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_absences_proposed_ends_after_start",
                table: "absences",
                sql: "\"ProposedEndDate\" IS NULL OR \"ProposedEndDate\" >= \"ProposedStartDate\"");

            migrationBuilder.AddForeignKey(
                name: "FK_absences_users_ProposedByUserId",
                table: "absences",
                column: "ProposedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_absences_users_ProposedByUserId",
                table: "absences");

            migrationBuilder.DropIndex(
                name: "IX_absences_ProposedByUserId",
                table: "absences");

            migrationBuilder.DropCheckConstraint(
                name: "ck_absences_proposed_ends_after_start",
                table: "absences");

            migrationBuilder.DropColumn(
                name: "ProposedAt",
                table: "absences");

            migrationBuilder.DropColumn(
                name: "ProposedByEmployee",
                table: "absences");

            migrationBuilder.DropColumn(
                name: "ProposedByUserId",
                table: "absences");

            migrationBuilder.DropColumn(
                name: "ProposedEndDate",
                table: "absences");

            migrationBuilder.DropColumn(
                name: "ProposedReason",
                table: "absences");

            migrationBuilder.DropColumn(
                name: "ProposedStartDate",
                table: "absences");
        }
    }
}
