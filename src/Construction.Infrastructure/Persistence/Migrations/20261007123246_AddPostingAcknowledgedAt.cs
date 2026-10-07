using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPostingAcknowledgedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AcknowledgedAt",
                table: "employee_projects",
                type: "timestamp with time zone",
                nullable: true);

            // Postings that already exist predate the confirmation; treat them as seen, so the schedule
            // does not show the whole workforce as unconfirmed on the day this ships.
            migrationBuilder.Sql("UPDATE employee_projects SET \"AcknowledgedAt\" = \"AssignedAt\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcknowledgedAt",
                table: "employee_projects");
        }
    }
}
