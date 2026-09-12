using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddToolRentalRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.AddColumn<int>(
                name: "OwnershipType",
                table: "tools",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "ToolRentalRateId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tool_rental_rates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonthlyAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SetByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tool_rental_rates", x => x.Id);
                    table.CheckConstraint("ck_tool_rental_rates_ends_after_start", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.CheckConstraint("ck_tool_rental_rates_positive", "\"MonthlyAmount\" > 0");
                    table.ForeignKey(
                        name: "FK_tool_rental_rates_tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tool_rental_rates_users_SetByUserId",
                        column: x => x.SetByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tool_rentals_out",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    RenterName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DailyRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SetByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tool_rentals_out", x => x.Id);
                    table.CheckConstraint("ck_tool_rentals_out_ends_after_start", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.CheckConstraint("ck_tool_rentals_out_rate_positive", "\"DailyRate\" > 0");
                    table.ForeignKey(
                        name: "FK_tool_rentals_out_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tool_rentals_out_tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tool_rentals_out_users_SetByUserId",
                        column: x => x.SetByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_rentals_out",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    RenterName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DailyRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SetByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_rentals_out", x => x.Id);
                    table.CheckConstraint("ck_vehicle_rentals_out_ends_after_start", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.CheckConstraint("ck_vehicle_rentals_out_rate_positive", "\"DailyRate\" > 0");
                    table.ForeignKey(
                        name: "FK_vehicle_rentals_out_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vehicle_rentals_out_users_SetByUserId",
                        column: x => x.SetByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vehicle_rentals_out_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tools_OwnershipType",
                table: "tools",
                column: "OwnershipType");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_ToolRentalRateId",
                table: "attachments",
                column: "ToolRentalRateId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"MaterialMovementId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"EmployeeRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"FinanceEntryId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"GeneralExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolRentalRateId\" IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_tool_rental_rates_SetByUserId",
                table: "tool_rental_rates",
                column: "SetByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tool_rental_rates_ToolId_StartDate",
                table: "tool_rental_rates",
                columns: new[] { "ToolId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_tool_rentals_out_CustomerId",
                table: "tool_rentals_out",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "ix_tool_rentals_out_one_open_per_tool",
                table: "tool_rentals_out",
                column: "ToolId",
                unique: true,
                filter: "\"EndDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tool_rentals_out_SetByUserId",
                table: "tool_rentals_out",
                column: "SetByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tool_rentals_out_ToolId_StartDate",
                table: "tool_rentals_out",
                columns: new[] { "ToolId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_rentals_out_CustomerId",
                table: "vehicle_rentals_out",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_rentals_out_one_open_per_vehicle",
                table: "vehicle_rentals_out",
                column: "VehicleId",
                unique: true,
                filter: "\"EndDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_rentals_out_SetByUserId",
                table: "vehicle_rentals_out",
                column: "SetByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_rentals_out_VehicleId_StartDate",
                table: "vehicle_rentals_out",
                columns: new[] { "VehicleId", "StartDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_tool_rental_rates_ToolRentalRateId",
                table: "attachments",
                column: "ToolRentalRateId",
                principalTable: "tool_rental_rates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attachments_tool_rental_rates_ToolRentalRateId",
                table: "attachments");

            migrationBuilder.DropTable(
                name: "tool_rental_rates");

            migrationBuilder.DropTable(
                name: "tool_rentals_out");

            migrationBuilder.DropTable(
                name: "vehicle_rentals_out");

            migrationBuilder.DropIndex(
                name: "IX_tools_OwnershipType",
                table: "tools");

            migrationBuilder.DropIndex(
                name: "IX_attachments_ToolRentalRateId",
                table: "attachments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "OwnershipType",
                table: "tools");

            migrationBuilder.DropColumn(
                name: "ToolRentalRateId",
                table: "attachments");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"MaterialMovementId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"EmployeeRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"FinanceEntryId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"GeneralExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationRateId\" IS NULL THEN 0 ELSE 1 END) = 1");
        }
    }
}
