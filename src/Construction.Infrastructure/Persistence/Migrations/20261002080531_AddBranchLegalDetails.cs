using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchLegalDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "branches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "branches",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "branches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "branches",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "branches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "branches",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "LegalEntity");

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "branches",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "branches",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerName",
                table: "branches",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "branches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "branches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "branches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "branches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatNumber",
                table: "branches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "City",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "OwnerName",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "VatNumber",
                table: "branches");
        }
    }
}
