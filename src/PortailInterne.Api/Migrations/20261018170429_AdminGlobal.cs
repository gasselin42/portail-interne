using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortailInterne.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdminGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql("UPDATE UserAccounts SET Role = 2 WHERE Email = 'admin@portail.local';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
			migrationBuilder.Sql("UPDATE UserAccounts SET Role = 0 WHERE Email = 'admin@portail.local';");
        }
    }
}
