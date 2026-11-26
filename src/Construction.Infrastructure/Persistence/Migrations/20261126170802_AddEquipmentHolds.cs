using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentHolds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "equipment_holds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    From = table.Column<DateOnly>(type: "date", nullable: false),
                    To = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_equipment_holds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "planned_equipment_changes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planned_equipment_changes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_holds_From_To",
                table: "equipment_holds",
                columns: new[] { "From", "To" });

            migrationBuilder.CreateIndex(
                name: "IX_equipment_holds_Kind_ItemId_From",
                table: "equipment_holds",
                columns: new[] { "Kind", "ItemId", "From" });

            migrationBuilder.CreateIndex(
                name: "IX_planned_equipment_changes_EffectiveFrom",
                table: "planned_equipment_changes",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_planned_equipment_changes_Kind_ItemId",
                table: "planned_equipment_changes",
                columns: new[] { "Kind", "ItemId" });

            // History starts today: where every item stands now becomes its first stretch. Earlier days stay unknown on
            // purpose, rather than guessed from today.
            migrationBuilder.Sql("""
                INSERT INTO equipment_holds ("Id", "Kind", "ItemId", "EmployeeId", "ProjectId", "From", "To", "CreatedAt")
                SELECT gen_random_uuid(), 0, v."Id", v."AssignedEmployeeId", v."AssignedProjectId", (now() AT TIME ZONE 'Europe/Belgrade')::date, NULL, now()
                FROM vehicles v WHERE NOT v."IsDeleted";
                INSERT INTO equipment_holds ("Id", "Kind", "ItemId", "EmployeeId", "ProjectId", "From", "To", "CreatedAt")
                SELECT gen_random_uuid(), 1, t."Id", t."AssignedEmployeeId", t."AssignedProjectId", (now() AT TIME ZONE 'Europe/Belgrade')::date, NULL, now()
                FROM tools t WHERE NOT t."IsDeleted";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "equipment_holds");

            migrationBuilder.DropTable(
                name: "planned_equipment_changes");
        }
    }
}
