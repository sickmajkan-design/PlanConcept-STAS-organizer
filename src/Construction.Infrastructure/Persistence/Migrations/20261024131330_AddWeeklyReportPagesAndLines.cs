using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklyReportPagesAndLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceReportId",
                table: "ledger_cells",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "weekly_site_report_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    HoursSecondMonth = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_site_report_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weekly_site_report_lines_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_weekly_site_report_lines_weekly_site_reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "weekly_site_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_site_report_pages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_site_report_pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weekly_site_report_pages_weekly_site_reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "weekly_site_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ledger_cells_SourceReportId",
                table: "ledger_cells",
                column: "SourceReportId");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_site_report_lines_EmployeeId",
                table: "weekly_site_report_lines",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_site_report_lines_ReportId_SortOrder",
                table: "weekly_site_report_lines",
                columns: new[] { "ReportId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_weekly_site_report_pages_ReportId_SortOrder",
                table: "weekly_site_report_pages",
                columns: new[] { "ReportId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_ledger_cells_weekly_site_reports_SourceReportId",
                table: "ledger_cells",
                column: "SourceReportId",
                principalTable: "weekly_site_reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Reports made before they had pages keep their one file as page 1, so nothing already sent loses its picture.
            migrationBuilder.Sql(
                """
                INSERT INTO weekly_site_report_pages
                    ("Id", "CreatedAt", "ReportId", "SortOrder", "FileName", "ContentType", "SizeBytes", "StorageKey")
                SELECT gen_random_uuid(), "CreatedAt", "Id", 0, "FileName", "ContentType", "SizeBytes", "StorageKey"
                FROM weekly_site_reports;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ledger_cells_weekly_site_reports_SourceReportId",
                table: "ledger_cells");

            migrationBuilder.DropTable(
                name: "weekly_site_report_lines");

            migrationBuilder.DropTable(
                name: "weekly_site_report_pages");

            migrationBuilder.DropIndex(
                name: "IX_ledger_cells_SourceReportId",
                table: "ledger_cells");

            migrationBuilder.DropColumn(
                name: "SourceReportId",
                table: "ledger_cells");
        }
    }
}
