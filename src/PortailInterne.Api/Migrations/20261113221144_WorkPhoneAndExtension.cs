using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortailInterne.Api.Migrations
{
    /// <inheritdoc />
    public partial class WorkPhoneAndExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "Employees",
				newName: "WorkPhone");

            migrationBuilder.AddColumn<string>(
                name: "PhoneExtension",
                table: "Employees",
                type: "TEXT",
                maxLength: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneExtension",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "WorkPhone",
                table: "Employees",
				newName: "PhoneNumber");
		}
    }
}
