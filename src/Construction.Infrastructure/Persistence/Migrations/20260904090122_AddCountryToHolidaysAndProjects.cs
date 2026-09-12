using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryToHolidaysAndProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_public_holidays_Date",
                table: "public_holidays");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "public_holidays",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "projects",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            // Every holiday on file so far was entered for Bosnia and
            // Herzegovina (the rows include "Dan nezavisnosti Bosne i
            // Hercegovine") — this backfill is what lets the unique index
            // below go on immediately after, rather than leaving 16 rows
            // with an empty country code.
            migrationBuilder.Sql("UPDATE public_holidays SET \"CountryCode\" = 'BA';");

            migrationBuilder.CreateIndex(
                name: "IX_public_holidays_CountryCode_Date",
                table: "public_holidays",
                columns: new[] { "CountryCode", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_public_holidays_CountryCode_Date",
                table: "public_holidays");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "public_holidays");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "projects");

            migrationBuilder.CreateIndex(
                name: "IX_public_holidays_Date",
                table: "public_holidays",
                column: "Date",
                unique: true);
        }
    }
}
