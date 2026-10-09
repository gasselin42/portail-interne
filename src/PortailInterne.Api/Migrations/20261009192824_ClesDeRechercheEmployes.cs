using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortailInterne.Api.Migrations
{
    /// <inheritdoc />
    public partial class ClesDeRechercheEmployes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstNameKey",
                table: "Employees",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JobTitleKey",
                table: "Employees",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastNameKey",
                table: "Employees",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_LastNameKey_FirstNameKey",
                table: "Employees",
                columns: new[] { "LastNameKey", "FirstNameKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_LastNameKey_FirstNameKey",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "FirstNameKey",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "JobTitleKey",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LastNameKey",
                table: "Employees");
        }
    }
}
