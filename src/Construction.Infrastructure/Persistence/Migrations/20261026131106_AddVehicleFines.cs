using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleFines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicle_fines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DriverEmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    BorneBy = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_fines", x => x.Id);
                    table.CheckConstraint("ck_vehicle_fines_amount_not_negative", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_vehicle_fines_employees_DriverEmployeeId",
                        column: x => x.DriverEmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vehicle_fines_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vehicle_fines_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_fine_reminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleFineId = table.Column<Guid>(type: "uuid", nullable: false),
                    DaysBefore = table.Column<int>(type: "integer", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_fine_reminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_fine_reminders_vehicle_fines_VehicleFineId",
                        column: x => x.VehicleFineId,
                        principalTable: "vehicle_fines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_fine_reminders_VehicleFineId_DaysBefore_DueDate",
                table: "vehicle_fine_reminders",
                columns: new[] { "VehicleFineId", "DaysBefore", "DueDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_fines_DriverEmployeeId_Status",
                table: "vehicle_fines",
                columns: new[] { "DriverEmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_fines_RecordedByUserId",
                table: "vehicle_fines",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_fines_Status_DueDate",
                table: "vehicle_fines",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_fines_VehicleId_OccurredOn",
                table: "vehicle_fines",
                columns: new[] { "VehicleId", "OccurredOn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicle_fine_reminders");

            migrationBuilder.DropTable(
                name: "vehicle_fines");
        }
    }
}
