using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleAndAbsences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_employee_projects",
                table: "employee_projects");

            migrationBuilder.DropIndex(
                name: "IX_employee_projects_ProjectId",
                table: "employee_projects");

            // Hand-written rather than left as EF generated it. The scaffolded
            // version added Id with a single default value, which would have
            // given every existing row the same primary key, and StartDate as
            // 0001-01-01, which would have dated every existing posting to the
            // first century. Both are only visible on a database that already
            // has assignments in it — that is, every deployed one.
            migrationBuilder.Sql(
                """
                ALTER TABLE employee_projects
                    ADD COLUMN "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
                    ADD COLUMN "CreatedAt" timestamp with time zone NOT NULL DEFAULT now(),
                    ADD COLUMN "UpdatedAt" timestamp with time zone NULL,
                    ADD COLUMN "EndDate" date NULL,
                    -- An existing posting started when it was recorded, and is
                    -- still running: that is what the old model meant.
                    ADD COLUMN "StartDate" date NOT NULL DEFAULT CURRENT_DATE;
                """);

            migrationBuilder.Sql(
                """
                UPDATE employee_projects
                SET "StartDate" = ("AssignedAt" AT TIME ZONE 'UTC')::date,
                    "CreatedAt" = "AssignedAt";
                """);

            // The defaults existed only to fill the rows already there; new
            // rows get their values from the application.
            migrationBuilder.Sql(
                """
                ALTER TABLE employee_projects
                    ALTER COLUMN "Id" DROP DEFAULT,
                    ALTER COLUMN "CreatedAt" DROP DEFAULT,
                    ALTER COLUMN "StartDate" DROP DEFAULT;
                """);

            migrationBuilder.AddPrimaryKey(
                name: "PK_employee_projects",
                table: "employee_projects",
                column: "Id");

            // The one thing that is always a mistake: the same person posted
            // to the same site twice over days that overlap. Different sites
            // may overlap on purpose — a supervisor covering two at once is
            // real, and forbidding it would make the board lie.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.Sql(
                """
                ALTER TABLE employee_projects
                    ADD CONSTRAINT ex_employee_projects_no_duplicate_posting
                    EXCLUDE USING gist (
                        "EmployeeId" WITH =,
                        "ProjectId" WITH =,
                        daterange("StartDate", "EndDate", '[]') WITH &&
                    );
                """);

            migrationBuilder.CreateTable(
                name: "absences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_absences", x => x.Id);
                    table.CheckConstraint("ck_absences_ends_after_start", "\"EndDate\" >= \"StartDate\"");
                    table.ForeignKey(
                        name: "FK_absences_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_absences_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_absences_users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_projects_EmployeeId_StartDate",
                table: "employee_projects",
                columns: new[] { "EmployeeId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_projects_ProjectId_StartDate",
                table: "employee_projects",
                columns: new[] { "ProjectId", "StartDate" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_employee_projects_ends_after_start",
                table: "employee_projects",
                sql: "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");

            migrationBuilder.CreateIndex(
                name: "IX_absences_EmployeeId_StartDate",
                table: "absences",
                columns: new[] { "EmployeeId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_absences_RequestedByUserId",
                table: "absences",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_absences_ReviewedByUserId",
                table: "absences",
                column: "ReviewedByUserId");

            // Two approved absences cannot cover the same day for one person.
            // Partial, because only an approved absence is a fact: a request
            // and a refusal are questions, and two of them overlapping is
            // ordinary — somebody asked twice and got one answer.
            migrationBuilder.Sql(
                """
                ALTER TABLE absences
                    ADD CONSTRAINT ex_absences_no_overlapping_approved
                    EXCLUDE USING gist (
                        "EmployeeId" WITH =,
                        daterange("StartDate", "EndDate", '[]') WITH &&
                    )
                    WHERE ("Status" = 2 AND "IsDeleted" = false);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_absences_Status",
                table: "absences",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "absences");

            // Before the key it depends on.
            migrationBuilder.Sql(
                """
                ALTER TABLE employee_projects
                    DROP CONSTRAINT IF EXISTS ex_employee_projects_no_duplicate_posting;
                """);

            migrationBuilder.DropPrimaryKey(
                name: "PK_employee_projects",
                table: "employee_projects");

            migrationBuilder.DropIndex(
                name: "IX_employee_projects_EmployeeId_StartDate",
                table: "employee_projects");

            migrationBuilder.DropIndex(
                name: "IX_employee_projects_ProjectId_StartDate",
                table: "employee_projects");

            migrationBuilder.DropCheckConstraint(
                name: "ck_employee_projects_ends_after_start",
                table: "employee_projects");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "employee_projects");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "employee_projects");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "employee_projects");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "employee_projects");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "employee_projects");

            migrationBuilder.AddPrimaryKey(
                name: "PK_employee_projects",
                table: "employee_projects",
                columns: new[] { "EmployeeId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_projects_ProjectId",
                table: "employee_projects",
                column: "ProjectId");
        }
    }
}
