using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeEntryConcurrencyToken : Migration
    {
        // "xmin" is Postgres's own built-in system column on every table — it
        // already exists and needs no migration. EF's migration generator
        // doesn't know that (it only sees a new concurrency-token property in
        // the model, not that the column backing it already exists), so the
        // AddColumn/DropColumn it wrote by default is deleted here. This
        // migration exists only so EF's model snapshot records the token;
        // running it changes nothing in the database.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
