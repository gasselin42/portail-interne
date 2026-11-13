using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortailInterne.Api.Migrations
{
    /// <inheritdoc />
    public partial class SiteCountryAndPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Sites",
                type: "TEXT",
                maxLength: 2,
                nullable: false,
                defaultValue: "CA");

            migrationBuilder.AddColumn<string>(
                name: "MainPhoneNumber",
                table: "Sites",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Sites",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CountryCode", "MainPhoneNumber" },
                values: new object[] { "CA", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Sites");

            migrationBuilder.DropColumn(
                name: "MainPhoneNumber",
                table: "Sites");
        }
    }
}
