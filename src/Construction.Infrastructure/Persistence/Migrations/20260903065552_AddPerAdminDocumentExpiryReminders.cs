using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerAdminDocumentExpiryReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_attachments_pending_expiry",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "ExpiryReminderSentAt",
                table: "attachments");

            migrationBuilder.AddColumn<int>(
                name: "DocumentExpiryReminderDays",
                table: "users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "attachment_expiry_reminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AttachmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachment_expiry_reminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attachment_expiry_reminders_attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attachment_expiry_reminders_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_users_reminder_days_positive",
                table: "users",
                sql: "\"DocumentExpiryReminderDays\" IS NULL OR \"DocumentExpiryReminderDays\" > 0");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_pending_expiry",
                table: "attachments",
                column: "ExpiresAt",
                filter: "\"ExpiresAt\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_attachment_expiry_reminders_AttachmentId_UserId",
                table: "attachment_expiry_reminders",
                columns: new[] { "AttachmentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attachment_expiry_reminders_UserId",
                table: "attachment_expiry_reminders",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachment_expiry_reminders");

            migrationBuilder.DropCheckConstraint(
                name: "ck_users_reminder_days_positive",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_attachments_pending_expiry",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "DocumentExpiryReminderDays",
                table: "users");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryReminderSentAt",
                table: "attachments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_attachments_pending_expiry",
                table: "attachments",
                column: "ExpiresAt",
                filter: "\"ExpiresAt\" IS NOT NULL AND \"ExpiryReminderSentAt\" IS NULL AND \"IsDeleted\" = false");
        }
    }
}
