using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCostTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_rates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SetByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_rates", x => x.Id);
                    table.CheckConstraint("ck_employee_rates_ends_after_start", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.CheckConstraint("ck_employee_rates_positive", "\"HourlyRate\" > 0");
                    table.ForeignKey(
                        name: "FK_employee_rates_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_rates_users_SetByUserId",
                        column: x => x.SetByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "material_movements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_material_movements", x => x.Id);
                    table.CheckConstraint("ck_material_movements_price_not_negative", "\"UnitPrice\" IS NULL OR \"UnitPrice\" >= 0");
                    table.CheckConstraint("ck_material_movements_quantity_signed_by_kind", "CASE WHEN \"Kind\" = 3\n     THEN \"Quantity\" <> 0\n     ELSE \"Quantity\" > 0\nEND");
                    table.ForeignKey(
                        name: "FK_material_movements_materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_material_movements_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_material_movements_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Litres = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    OdometerKm = table.Column<int>(type: "integer", nullable: true),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_expenses", x => x.Id);
                    table.CheckConstraint("ck_vehicle_expenses_amount_not_negative", "\"Amount\" >= 0");
                    table.CheckConstraint("ck_vehicle_expenses_litres_only_for_fuel", "CASE WHEN \"Kind\" = 1\n     THEN \"Litres\" IS NOT NULL AND \"Litres\" > 0\n     ELSE \"Litres\" IS NULL\nEND");
                    table.CheckConstraint("ck_vehicle_expenses_odometer_not_negative", "\"OdometerKm\" IS NULL OR \"OdometerKm\" >= 0");
                    table.ForeignKey(
                        name: "FK_vehicle_expenses_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vehicle_expenses_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_rates_EmployeeId_StartDate",
                table: "employee_rates",
                columns: new[] { "EmployeeId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_rates_SetByUserId",
                table: "employee_rates",
                column: "SetByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_material_movements_MaterialId_OccurredOn",
                table: "material_movements",
                columns: new[] { "MaterialId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_material_movements_ProjectId_OccurredOn",
                table: "material_movements",
                columns: new[] { "ProjectId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_material_movements_RecordedByUserId",
                table: "material_movements",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_expenses_Kind",
                table: "vehicle_expenses",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_expenses_RecordedByUserId",
                table: "vehicle_expenses",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_expenses_VehicleId_OccurredOn",
                table: "vehicle_expenses",
                columns: new[] { "VehicleId", "OccurredOn" });

            // Two rates covering the same day would make the cost of an hour
            // ambiguous, and the report would pick one silently. btree_gist is
            // already present from the schedule migration; the IF NOT EXISTS
            // keeps this migration standalone on a fresh database.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.Sql("""
                ALTER TABLE employee_rates
                    ADD CONSTRAINT ex_employee_rates_no_overlap
                    EXCLUDE USING gist ("EmployeeId" WITH =,
                        daterange("StartDate", "EndDate", '[]') WITH &&);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE employee_rates DROP CONSTRAINT IF EXISTS ex_employee_rates_no_overlap;");

            migrationBuilder.DropTable(
                name: "employee_rates");

            migrationBuilder.DropTable(
                name: "material_movements");

            migrationBuilder.DropTable(
                name: "vehicle_expenses");
        }
    }
}
