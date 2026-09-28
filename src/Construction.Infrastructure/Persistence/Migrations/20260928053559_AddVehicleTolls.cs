using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleTolls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicle_tolls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RouteSegment = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    PaidByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_tolls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_tolls_users_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vehicle_tolls_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_toll_expiry_reminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleTollId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_toll_expiry_reminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_toll_expiry_reminders_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vehicle_toll_expiry_reminders_vehicle_tolls_VehicleTollId",
                        column: x => x.VehicleTollId,
                        principalTable: "vehicle_tolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_toll_payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleTollId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: false),
                    PaidByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_toll_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_toll_payments_users_PaidByUserId",
                        column: x => x.PaidByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vehicle_toll_payments_vehicle_tolls_VehicleTollId",
                        column: x => x.VehicleTollId,
                        principalTable: "vehicle_tolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_toll_expiry_reminders_UserId",
                table: "vehicle_toll_expiry_reminders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_toll_expiry_reminders_VehicleTollId_UserId_ValidUnt~",
                table: "vehicle_toll_expiry_reminders",
                columns: new[] { "VehicleTollId", "UserId", "ValidUntil" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_toll_payments_PaidByUserId",
                table: "vehicle_toll_payments",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_toll_payments_VehicleTollId",
                table: "vehicle_toll_payments",
                column: "VehicleTollId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_tolls_PaidByUserId",
                table: "vehicle_tolls",
                column: "PaidByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_tolls_VehicleId_ValidUntil",
                table: "vehicle_tolls",
                columns: new[] { "VehicleId", "ValidUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicle_toll_expiry_reminders");

            migrationBuilder.DropTable(
                name: "vehicle_toll_payments");

            migrationBuilder.DropTable(
                name: "vehicle_tolls");
        }
    }
}
