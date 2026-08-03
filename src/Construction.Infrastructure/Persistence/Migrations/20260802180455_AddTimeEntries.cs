using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "time_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BreakMinutes = table.Column<int>(type: "integer", nullable: false),
                    WorkType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StartLatitude = table.Column<double>(type: "double precision", nullable: true),
                    StartLongitude = table.Column<double>(type: "double precision", nullable: true),
                    EndLatitude = table.Column<double>(type: "double precision", nullable: true),
                    EndLongitude = table.Column<double>(type: "double precision", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_entries", x => x.Id);
                    table.CheckConstraint("ck_time_entries_break_non_negative", "\"BreakMinutes\" >= 0");
                    table.CheckConstraint("ck_time_entries_ends_after_start", "\"EndedAt\" IS NULL OR \"EndedAt\" > \"StartedAt\"");
                    table.ForeignKey(
                        name: "FK_time_entries_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_time_entries_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_time_entries_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_time_entries_EmployeeId_StartedAt",
                table: "time_entries",
                columns: new[] { "EmployeeId", "StartedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_time_entries_one_open_per_employee",
                table: "time_entries",
                column: "EmployeeId",
                unique: true,
                filter: "\"EndedAt\" IS NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_time_entries_ProjectId",
                table: "time_entries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_time_entries_ReviewedByUserId",
                table: "time_entries",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_time_entries_Status_StartedAt",
                table: "time_entries",
                columns: new[] { "Status", "StartedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "time_entries");
        }
    }
}
