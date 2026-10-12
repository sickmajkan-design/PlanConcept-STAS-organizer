using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaymentsAndClientRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ClientHourlyRate",
                table: "projects",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "invoice_payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    PaidOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_payments", x => x.Id);
                    table.CheckConstraint("ck_invoice_payments_amount_positive", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_invoice_payments_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoice_payments_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_projects_client_hourly_rate_not_negative",
                table: "projects",
                sql: "\"ClientHourlyRate\" IS NULL OR \"ClientHourlyRate\" >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_payments_InvoiceId",
                table: "invoice_payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_payments_PaidOn",
                table: "invoice_payments",
                column: "PaidOn");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_payments_RecordedByUserId",
                table: "invoice_payments",
                column: "RecordedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_projects_client_hourly_rate_not_negative",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "ClientHourlyRate",
                table: "projects");
        }
    }
}
