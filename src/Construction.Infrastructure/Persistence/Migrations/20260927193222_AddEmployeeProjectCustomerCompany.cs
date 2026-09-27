using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Construction.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeProjectCustomerCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerCompanyId",
                table: "employee_projects",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_projects_CustomerCompanyId",
                table: "employee_projects",
                column: "CustomerCompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_employee_projects_customer_companies_CustomerCompanyId",
                table: "employee_projects",
                column: "CustomerCompanyId",
                principalTable: "customer_companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employee_projects_customer_companies_CustomerCompanyId",
                table: "employee_projects");

            migrationBuilder.DropIndex(
                name: "IX_employee_projects_CustomerCompanyId",
                table: "employee_projects");

            migrationBuilder.DropColumn(
                name: "CustomerCompanyId",
                table: "employee_projects");
        }
    }
}
