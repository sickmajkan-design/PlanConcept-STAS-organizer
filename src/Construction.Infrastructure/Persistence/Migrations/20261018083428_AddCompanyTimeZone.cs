using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyTimeZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "company_settings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "company_settings");
        }
    }
}
