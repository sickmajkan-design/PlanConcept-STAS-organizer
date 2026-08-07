using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Drops a column nothing ever wrote to and nothing ever read.
    /// </summary>
    /// <remarks>
    /// An employee photograph belongs in attachments, which stores the bytes,
    /// enforces who may see them and knows when a document lapses. This column
    /// was a second, weaker way to do the same thing: a free-text URL to some
    /// other host, which no screen displayed and no client ever set — the admin
    /// panel carried a form value and a translated label for it, but never
    /// rendered an input.
    ///
    /// The drop is not reversible for data. The column is empty in any
    /// deployment that only ever used these two clients; a deployment that
    /// filled it through the API directly should copy the values out first.
    /// </remarks>
    public partial class DropEmployeePhotoUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "employees");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "employees",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }
    }
}
