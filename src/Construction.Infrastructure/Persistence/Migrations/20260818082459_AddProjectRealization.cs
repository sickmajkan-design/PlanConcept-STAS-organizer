using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectRealization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ContractValue",
                table: "projects",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "project_revenues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_revenues", x => x.Id);
                    table.CheckConstraint("ck_project_revenues_amount_not_negative", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_project_revenues_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_revenues_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_projects_contract_value_not_negative",
                table: "projects",
                sql: "\"ContractValue\" IS NULL OR \"ContractValue\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_project_revenues_ProjectId_OccurredOn",
                table: "project_revenues",
                columns: new[] { "ProjectId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_project_revenues_RecordedByUserId",
                table: "project_revenues",
                column: "RecordedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_revenues");

            migrationBuilder.DropCheckConstraint(
                name: "ck_projects_contract_value_not_negative",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "ContractValue",
                table: "projects");
        }
    }
}
