using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomersAndProjectHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentProjectId",
                table: "projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            // Backfill: every distinct non-blank value the free-text "Client"
            // column ever held becomes a real Customer row, and every project
            // that named it gets pointed at that row — done before the column
            // is dropped below, so none of this history is lost.
            migrationBuilder.Sql(
                """
                INSERT INTO customers ("Id", "Name", "IsDeleted", "CreatedAt")
                SELECT gen_random_uuid(), x."Client", false, now()
                FROM (SELECT DISTINCT "Client" FROM projects WHERE "Client" IS NOT NULL AND trim("Client") <> '') x;

                UPDATE projects
                SET "CustomerId" = c."Id"
                FROM customers c
                WHERE projects."Client" = c."Name";
                """);

            migrationBuilder.DropColumn(
                name: "Client",
                table: "projects");

            migrationBuilder.CreateIndex(
                name: "IX_projects_CustomerId",
                table: "projects",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_projects_ParentProjectId",
                table: "projects",
                column: "ParentProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_customers_Name",
                table: "customers",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_projects_customers_CustomerId",
                table: "projects",
                column: "CustomerId",
                principalTable: "customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_projects_projects_ParentProjectId",
                table: "projects",
                column: "ParentProjectId",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_projects_customers_CustomerId",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_projects_projects_ParentProjectId",
                table: "projects");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropIndex(
                name: "IX_projects_CustomerId",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "IX_projects_ParentProjectId",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "ParentProjectId",
                table: "projects");

            migrationBuilder.AddColumn<string>(
                name: "Client",
                table: "projects",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }
    }
}
