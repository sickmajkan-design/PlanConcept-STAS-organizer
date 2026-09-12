using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWeeklySiteReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WeeklyReportsForwardEmail",
                table: "company_settings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "weekly_report_reminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsoYear = table.Column<int>(type: "integer", nullable: false),
                    IsoWeek = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_report_reminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weekly_report_reminders_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_weekly_report_reminders_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weekly_site_reports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsoYear = table.Column<int>(type: "integer", nullable: false),
                    IsoWeek = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weekly_site_reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weekly_site_reports_employees_SubmittedByEmployeeId",
                        column: x => x.SubmittedByEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_weekly_site_reports_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_weekly_site_reports_users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weekly_report_reminders_EmployeeId",
                table: "weekly_report_reminders",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_report_reminders_ProjectId_EmployeeId_IsoYear_IsoWeek",
                table: "weekly_report_reminders",
                columns: new[] { "ProjectId", "EmployeeId", "IsoYear", "IsoWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weekly_site_reports_IsoYear_IsoWeek",
                table: "weekly_site_reports",
                columns: new[] { "IsoYear", "IsoWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_weekly_site_reports_ProcessedByUserId",
                table: "weekly_site_reports",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_weekly_site_reports_ProjectId_IsoYear_IsoWeek",
                table: "weekly_site_reports",
                columns: new[] { "ProjectId", "IsoYear", "IsoWeek" });

            migrationBuilder.CreateIndex(
                name: "IX_weekly_site_reports_SubmittedByEmployeeId",
                table: "weekly_site_reports",
                column: "SubmittedByEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weekly_report_reminders");

            migrationBuilder.DropTable(
                name: "weekly_site_reports");

            migrationBuilder.DropColumn(
                name: "WeeklyReportsForwardEmail",
                table: "company_settings");
        }
    }
}
