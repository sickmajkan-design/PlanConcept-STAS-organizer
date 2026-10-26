using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddToolKits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsKit",
                table: "tools",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "tool_kit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PayoutMethod = table.Column<int>(type: "integer", nullable: true),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tool_kit_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tool_kit_events_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tool_kit_events_tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tool_kit_events_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tool_kit_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tool_kit_items", x => x.Id);
                    table.CheckConstraint("ck_tool_kit_items_price_not_negative", "\"UnitPrice\" >= 0");
                    table.CheckConstraint("ck_tool_kit_items_quantity_positive", "\"Quantity\" >= 1");
                    table.ForeignKey(
                        name: "FK_tool_kit_items_tools_ToolId",
                        column: x => x.ToolId,
                        principalTable: "tools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tool_kit_events_EmployeeId_SettledAt",
                table: "tool_kit_events",
                columns: new[] { "EmployeeId", "SettledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tool_kit_events_RecordedByUserId",
                table: "tool_kit_events",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tool_kit_events_ToolId_CreatedAt",
                table: "tool_kit_events",
                columns: new[] { "ToolId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_tool_kit_items_ToolId_SortOrder",
                table: "tool_kit_items",
                columns: new[] { "ToolId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tool_kit_events");

            migrationBuilder.DropTable(
                name: "tool_kit_items");

            migrationBuilder.DropColumn(
                name: "IsKit",
                table: "tools");
        }
    }
}
