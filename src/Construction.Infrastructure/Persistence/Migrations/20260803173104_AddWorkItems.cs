using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.AddColumn<Guid>(
                name: "WorkItemId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "work_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DueReminderSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_items", x => x.Id);
                    table.CheckConstraint("ck_work_items_defect_has_project", "\"Kind\" <> 2 OR \"ProjectId\" IS NOT NULL");
                    table.CheckConstraint("ck_work_items_position_complete", "(\"Latitude\" IS NULL) = (\"Longitude\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_work_items_employees_AssignedEmployeeId",
                        column: x => x.AssignedEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_work_items_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_work_items_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_work_items_users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_WorkItemId",
                table: "attachments",
                column: "WorkItemId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_work_items_AssignedEmployeeId_Status",
                table: "work_items",
                columns: new[] { "AssignedEmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_work_items_CreatedByUserId",
                table: "work_items",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "ix_work_items_pending_due_reminder",
                table: "work_items",
                column: "DueDate",
                filter: "\"DueDate\" IS NOT NULL AND \"DueReminderSentAt\" IS NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_work_items_ProjectId_Status",
                table: "work_items",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_work_items_ResolvedByUserId",
                table: "work_items",
                column: "ResolvedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_work_items_WorkItemId",
                table: "attachments",
                column: "WorkItemId",
                principalTable: "work_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attachments_work_items_WorkItemId",
                table: "attachments");

            migrationBuilder.DropTable(
                name: "work_items");

            migrationBuilder.DropIndex(
                name: "IX_attachments_WorkItemId",
                table: "attachments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "WorkItemId",
                table: "attachments");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END) = 1");
        }
    }
}
