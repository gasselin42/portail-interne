using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortailInterne.Api.Migrations
{
    /// <inheritdoc />
    public partial class EmployeePreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeePreferences",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    CalendarHourRange = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePreferences", x => x.EmployeeId);
                    table.ForeignKey(
                        name: "FK_EmployeePreferences_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeePreferences");
        }
    }
}
