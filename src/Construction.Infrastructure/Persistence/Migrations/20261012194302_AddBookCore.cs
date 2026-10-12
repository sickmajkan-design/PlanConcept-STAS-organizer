using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBookCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "book_categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Group = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bucket = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SystemKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "book_category_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_category_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_book_category_rules_book_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "book_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "book_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OccurredOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Counterparty = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClientRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_book_entries", x => x.Id);
                    table.CheckConstraint("ck_book_entries_amount_positive", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_book_entries_book_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "book_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_book_entries_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_book_entries_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_book_entries_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_book_entries_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_book_entries_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "book_categories",
                columns: new[] { "Id", "Bucket", "CreatedAt", "Group", "IsActive", "Kind", "Name", "SortOrder", "SystemKey", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0b00-000000000001"), "Collection", new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Income", "Naplata klijentu", 1, "Collection", null },
                    { new Guid("00000000-0000-0000-0b00-000000000002"), "WorkerEarnings", new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Cost", "Plata radnika", 2, "WorkerPay", null },
                    { new Guid("00000000-0000-0000-0b00-000000000003"), "WorkerEarnings", new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Cost", "Kooperanti", 3, "Subcontractors", null },
                    { new Guid("00000000-0000-0000-0b00-000000000004"), "WorkerEarnings", new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Cost", "Kancelarija", 4, "Office", null },
                    { new Guid("00000000-0000-0000-0b00-000000000005"), "SiteCosts", new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Cost", "Gorivo", 5, "Fuel", null },
                    { new Guid("00000000-0000-0000-0b00-000000000006"), "SiteCosts", new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Cost", "Stanovanje", 6, "Housing", null },
                    { new Guid("00000000-0000-0000-0b00-000000000007"), "SiteCosts", new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Cost", "Rent a car", 7, "CarRental", null },
                    { new Guid("00000000-0000-0000-0b00-000000000008"), "SiteCosts", new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Cost", "Alat", 8, "Tools", null },
                    { new Guid("00000000-0000-0000-0b00-000000000009"), "SiteCosts", new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Cost", "Materijal", 9, "Materials", null },
                    { new Guid("00000000-0000-0000-0b00-00000000000a"), "Fixed", new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Cost", "Fiksni troškovi", 10, "FixedCosts", null },
                    { new Guid("00000000-0000-0000-0b00-00000000000b"), "Fixed", new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Cost", "Osiguranje", 11, "Insurance", null },
                    { new Guid("00000000-0000-0000-0b00-00000000000c"), "Fixed", new DateTime(2026, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, "Cost", "Ostalo", 99, "Other", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_book_categories_Kind_SortOrder",
                table: "book_categories",
                columns: new[] { "Kind", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_book_categories_SystemKey",
                table: "book_categories",
                column: "SystemKey",
                unique: true,
                filter: "\"SystemKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_book_category_rules_CategoryId",
                table: "book_category_rules",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_book_category_rules_Priority",
                table: "book_category_rules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_book_entries_BranchId",
                table: "book_entries",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_book_entries_CategoryId_OccurredOn",
                table: "book_entries",
                columns: new[] { "CategoryId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_book_entries_ClientRequestId",
                table: "book_entries",
                column: "ClientRequestId",
                unique: true,
                filter: "\"ClientRequestId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_book_entries_CustomerId",
                table: "book_entries",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_book_entries_EmployeeId",
                table: "book_entries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_book_entries_OccurredOn",
                table: "book_entries",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_book_entries_ProjectId_OccurredOn",
                table: "book_entries",
                columns: new[] { "ProjectId", "OccurredOn" });

            migrationBuilder.CreateIndex(
                name: "IX_book_entries_RecordedByUserId",
                table: "book_entries",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_book_entries_Source_SourceId",
                table: "book_entries",
                columns: new[] { "Source", "SourceId" },
                unique: true,
                filter: "\"SourceId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "book_category_rules");

            migrationBuilder.DropTable(
                name: "book_entries");

            migrationBuilder.DropTable(
                name: "book_categories");
        }
    }
}
