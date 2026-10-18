using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dashboard_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_dashboard_snapshots_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_snapshots_UserId_Day_Key",
                table: "dashboard_snapshots",
                columns: new[] { "UserId", "Day", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dashboard_snapshots");
        }
    }
}
