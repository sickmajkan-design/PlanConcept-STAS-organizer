using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDkvFuelTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fuel_import_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ImportedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    NewCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedCount = table.Column<int>(type: "integer", nullable: false),
                    DuplicateCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fuel_import_batches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fuel_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    StatementVehicleLabel = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    OccurredAtTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ProductGroup = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Country = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    IsInvoiced = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Issue = table.Column<int>(type: "integer", nullable: false),
                    IssueDetail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VehicleExpenseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fuel_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fuel_transactions_fuel_import_batches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "fuel_import_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fuel_transactions_vehicle_expenses_VehicleExpenseId",
                        column: x => x.VehicleExpenseId,
                        principalTable: "vehicle_expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_fuel_transactions_vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fuel_transactions_CardNumber_OccurredOn_OccurredAtTime_Prod~",
                table: "fuel_transactions",
                columns: new[] { "CardNumber", "OccurredOn", "OccurredAtTime", "ProductCode", "Amount" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fuel_transactions_ImportBatchId",
                table: "fuel_transactions",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_transactions_Status",
                table: "fuel_transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_transactions_VehicleExpenseId",
                table: "fuel_transactions",
                column: "VehicleExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_fuel_transactions_VehicleId",
                table: "fuel_transactions",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fuel_transactions");

            migrationBuilder.DropTable(
                name: "fuel_import_batches");
        }
    }
}
