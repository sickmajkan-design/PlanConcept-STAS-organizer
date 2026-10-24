using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationBroadcasts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BroadcastId",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReminder",
                table: "notifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Via",
                table: "notifications",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "notification_broadcasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RequiresAcknowledgment = table.Column<bool>(type: "boolean", nullable: false),
                    SentByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AudienceJson = table.Column<string>(type: "jsonb", nullable: false),
                    RecipientCount = table.Column<int>(type: "integer", nullable: false),
                    WithoutAccountCount = table.Column<int>(type: "integer", nullable: false),
                    LastReminderAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification_broadcasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notification_broadcasts_users_SentByUserId",
                        column: x => x.SentByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_BroadcastId",
                table: "notifications",
                column: "BroadcastId");

            migrationBuilder.CreateIndex(
                name: "IX_notification_broadcasts_CreatedAt",
                table: "notification_broadcasts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_notification_broadcasts_SentByUserId",
                table: "notification_broadcasts",
                column: "SentByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_notification_broadcasts_BroadcastId",
                table: "notifications",
                column: "BroadcastId",
                principalTable: "notification_broadcasts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_notifications_notification_broadcasts_BroadcastId",
                table: "notifications");

            migrationBuilder.DropTable(
                name: "notification_broadcasts");

            migrationBuilder.DropIndex(
                name: "IX_notifications_BroadcastId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "BroadcastId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "IsReminder",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "Via",
                table: "notifications");
        }
    }
}
