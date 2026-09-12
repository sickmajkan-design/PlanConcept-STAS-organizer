using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAbsenceAndWorkItemConcurrencyTokens : Migration
    {
        // "xmin" is Postgres's own built-in system column, already on every
        // row of both tables — see the identical note on
        // AddTimeEntryConcurrencyToken. This migration exists only so EF's
        // model snapshot records the new tokens; running it changes nothing
        // in the database.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
