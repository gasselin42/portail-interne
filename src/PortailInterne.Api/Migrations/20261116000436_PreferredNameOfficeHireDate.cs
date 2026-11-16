using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortailInterne.Api.Migrations
{
    /// <inheritdoc />
    public partial class PreferredNameOfficeHireDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_LastNameKey_FirstNameKey",
                table: "Employees");

            migrationBuilder.AddColumn<string>(
                name: "DisplayFirstNameKey",
                table: "Employees",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "HireDate",
                table: "Employees",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "OfficeLocation",
                table: "Employees",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredName",
                table: "Employees",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_LastNameKey_DisplayFirstNameKey",
                table: "Employees",
                columns: new[] { "LastNameKey", "DisplayFirstNameKey" });

			migrationBuilder.Sql("""
				UPDATE Employees
				SET DisplayFirstNameKey = FirstNameKey
				""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_LastNameKey_DisplayFirstNameKey",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DisplayFirstNameKey",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "HireDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "OfficeLocation",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PreferredName",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_LastNameKey_FirstNameKey",
                table: "Employees",
                columns: new[] { "LastNameKey", "FirstNameKey" });
        }
    }
}
