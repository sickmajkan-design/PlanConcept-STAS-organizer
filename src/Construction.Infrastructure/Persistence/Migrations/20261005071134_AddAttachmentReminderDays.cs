using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachmentReminderDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attachment_expiry_reminders_AttachmentId_UserId",
                table: "attachment_expiry_reminders");

            migrationBuilder.AddColumn<int[]>(
                name: "ReminderDays",
                table: "attachments",
                type: "integer[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<int>(
                name: "DaysBefore",
                table: "attachment_expiry_reminders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_attachment_expiry_reminders_AttachmentId_UserId_DaysBefore",
                table: "attachment_expiry_reminders",
                columns: new[] { "AttachmentId", "UserId", "DaysBefore" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_attachment_expiry_reminders_AttachmentId_UserId_DaysBefore",
                table: "attachment_expiry_reminders");

            migrationBuilder.DropColumn(
                name: "ReminderDays",
                table: "attachments");

            migrationBuilder.DropColumn(
                name: "DaysBefore",
                table: "attachment_expiry_reminders");

            migrationBuilder.CreateIndex(
                name: "IX_attachment_expiry_reminders_AttachmentId_UserId",
                table: "attachment_expiry_reminders",
                columns: new[] { "AttachmentId", "UserId" },
                unique: true);
        }
    }
}
