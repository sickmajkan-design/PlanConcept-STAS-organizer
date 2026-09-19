using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandAccommodations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_accommodation_rates_positive",
                table: "accommodation_rates");

            migrationBuilder.RenameColumn(
                name: "MonthlyAmount",
                table: "accommodation_rates",
                newName: "Amount");

            migrationBuilder.AddColumn<decimal>(
                name: "AreaSquareMeters",
                table: "accommodations",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Beds",
                table: "accommodations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "accommodations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ContractEnd",
                table: "accommodations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractNumber",
                table: "accommodations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ContractStart",
                table: "accommodations",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DepositAmount",
                table: "accommodations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Floor",
                table: "accommodations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "LandlordEmail",
                table: "accommodations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandlordName",
                table: "accommodations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LandlordPhone",
                table: "accommodations",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "accommodations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rooms",
                table: "accommodations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "accommodations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "UtilitiesIncluded",
                table: "accommodations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "accommodation_rates",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "accommodation_stays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccommodationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accommodation_stays", x => x.Id);
                    table.CheckConstraint("ck_accommodation_stays_ends_after_start", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
                    table.ForeignKey(
                        name: "FK_accommodation_stays_accommodations_AccommodationId",
                        column: x => x.AccommodationId,
                        principalTable: "accommodations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accommodation_stays_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_accommodation_stays_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_accommodations_beds_positive",
                table: "accommodations",
                sql: "\"Beds\" IS NULL OR \"Beds\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_accommodations_contract_ends_after_start",
                table: "accommodations",
                sql: "\"ContractEnd\" IS NULL OR \"ContractStart\" IS NULL OR \"ContractEnd\" >= \"ContractStart\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_accommodations_rooms_positive",
                table: "accommodations",
                sql: "\"Rooms\" IS NULL OR \"Rooms\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_accommodation_rates_positive",
                table: "accommodation_rates",
                sql: "\"Amount\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_accommodation_stays_AccommodationId_StartDate",
                table: "accommodation_stays",
                columns: new[] { "AccommodationId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_accommodation_stays_EmployeeId_StartDate",
                table: "accommodation_stays",
                columns: new[] { "EmployeeId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_accommodation_stays_ProjectId",
                table: "accommodation_stays",
                column: "ProjectId");

            // A person lives in one place at a time. The handler says where they
            // already are; this is what holds when two requests race.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.Sql(
                """
                ALTER TABLE accommodation_stays
                    ADD CONSTRAINT ex_accommodation_stays_no_overlap
                    EXCLUDE USING gist (
                        "EmployeeId" WITH =,
                        daterange("StartDate", "EndDate", '[]') WITH &&
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accommodation_stays");

            migrationBuilder.DropCheckConstraint(
                name: "ck_accommodations_beds_positive",
                table: "accommodations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_accommodations_contract_ends_after_start",
                table: "accommodations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_accommodations_rooms_positive",
                table: "accommodations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_accommodation_rates_positive",
                table: "accommodation_rates");

            migrationBuilder.DropColumn(
                name: "AreaSquareMeters",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "Beds",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "City",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "ContractEnd",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "ContractNumber",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "ContractStart",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "DepositAmount",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "Floor",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "LandlordEmail",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "LandlordName",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "LandlordPhone",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "Rooms",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "UtilitiesIncluded",
                table: "accommodations");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "accommodation_rates");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "accommodation_rates",
                newName: "MonthlyAmount");

            migrationBuilder.AddCheckConstraint(
                name: "ck_accommodation_rates_positive",
                table: "accommodation_rates",
                sql: "\"MonthlyAmount\" > 0");
        }
    }
}
