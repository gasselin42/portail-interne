using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortailInterne.Api.Migrations
{
    /// <inheritdoc />
    public partial class CongeDeclarePourAutrui : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "LeaveRequests",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_CreatedById",
                table: "LeaveRequests",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveRequests_Employees_CreatedById",
                table: "LeaveRequests",
                column: "CreatedById",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeaveRequests_Employees_CreatedById",
                table: "LeaveRequests");

            migrationBuilder.DropIndex(
                name: "IX_LeaveRequests_CreatedById",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "LeaveRequests");
        }
    }
}
