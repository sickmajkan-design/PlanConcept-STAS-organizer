using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneralExpensesAndAccommodations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.AddColumn<Guid>(
                name: "AccommodationId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AccommodationRateId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GeneralExpenseId",
                table: "attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "accommodations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accommodations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "general_expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_general_expenses", x => x.Id);
                    table.CheckConstraint("ck_general_expenses_amount_not_negative", "\"Amount\" >= 0");
                    table.ForeignKey(
                        name: "FK_general_expenses_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_general_expenses_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_general_expenses_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "accommodation_rates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccommodationId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_accommodation_rates", x => x.Id);
                    table.CheckConstraint("ck_accommodation_rates_ends_after_start", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.CheckConstraint("ck_accommodation_rates_positive", "\"MonthlyAmount\" > 0");
                    table.ForeignKey(
                        name: "FK_accommodation_rates_accommodations_AccommodationId",
                        column: x => x.AccommodationId,
                        principalTable: "accommodations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accommodation_rates_users_SetByUserId",
                        column: x => x.SetByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_AccommodationId",
                table: "attachments",
                column: "AccommodationId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_AccommodationRateId",
                table: "attachments",
                column: "AccommodationRateId");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_GeneralExpenseId",
                table: "attachments",
                column: "GeneralExpenseId");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"MaterialMovementId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"EmployeeRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"FinanceEntryId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleRentalRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"GeneralExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"AccommodationRateId\" IS NULL THEN 0 ELSE 1 END) = 1");

            migrationBuilder.CreateIndex(
                name: "IX_accommodation_rates_AccommodationId_StartDate",
                table: "accommodation_rates",
                columns: new[] { "AccommodationId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_accommodation_rates_SetByUserId",
                table: "accommodation_rates",
                column: "SetByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_accommodations_Address",
                table: "accommodations",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_general_expenses_Category_OccurredOn",
                table: "general_expenses",
                columns: new[] { "Category", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_general_expenses_EmployeeId",
                table: "general_expenses",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_general_expenses_ProjectId",
                table: "general_expenses",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_general_expenses_RecordedByUserId",
                table: "general_expenses",
                column: "RecordedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_accommodation_rates_AccommodationRateId",
                table: "attachments",
                column: "AccommodationRateId",
                principalTable: "accommodation_rates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_accommodations_AccommodationId",
                table: "attachments",
                column: "AccommodationId",
                principalTable: "accommodations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_attachments_general_expenses_GeneralExpenseId",
                table: "attachments",
                column: "GeneralExpenseId",
                principalTable: "general_expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attachments_accommodation_rates_AccommodationRateId",
                table: "attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_attachments_accommodations_AccommodationId",
                table: "attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_attachments_general_expenses_GeneralExpenseId",
                table: "attachments");

            migrationBuilder.DropTable(
                name: "accommodation_rates");

            migrationBuilder.DropTable(
                name: "general_expenses");

            migrationBuilder.DropTable(
                name: "accommodations");

            migrationBuilder.DropIndex(
                name: "IX_attachments_AccommodationId",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_attachments_AccommodationRateId",
                table: "attachments");

            migrationBuilder.DropIndex(
                name: "IX_attachments_GeneralExpenseId",
                table: "attachments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "AccommodationId",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "AccommodationRateId",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "GeneralExpenseId",
                table: "attachments");

            migrationBuilder.AddCheckConstraint(
                name: "ck_attachments_exactly_one_owner",
                table: "attachments",
                sql: "(CASE WHEN \"EmployeeId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ProjectId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"WorkItemId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"MaterialMovementId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"EmployeeRateId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"FinanceEntryId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"ToolExpenseId\" IS NULL THEN 0 ELSE 1 END\n+ CASE WHEN \"VehicleRentalRateId\" IS NULL THEN 0 ELSE 1 END) = 1");
        }
    }
}
