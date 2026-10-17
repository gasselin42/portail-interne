using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortailInterne.Api.Migrations
{
    /// <inheritdoc />
    public partial class SoldeAnnuelConges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnnualLeaveDays",
                table: "Employees",
                type: "INTEGER",
                nullable: false,
                defaultValue: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnualLeaveDays",
                table: "Employees");
        }
    }
}
