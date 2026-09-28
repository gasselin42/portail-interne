using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortailInterne.Api.Migrations
{
    /// <inheritdoc />
    public partial class EmployeePhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoFileName",
                table: "Employees",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoFileName",
                table: "Employees");
        }
    }
}
