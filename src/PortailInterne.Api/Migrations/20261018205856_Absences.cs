using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortailInterne.Api.Migrations
{
    /// <inheritdoc />
    public partial class Absences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleAdjustment_Employees_CreatedById",
                table: "ScheduleAdjustment");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleAdjustment_Employees_EmployeeId",
                table: "ScheduleAdjustment");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleAdjustment_Employees_ReviewedById",
                table: "ScheduleAdjustment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScheduleAdjustment",
                table: "ScheduleAdjustment");

            migrationBuilder.RenameTable(
                name: "ScheduleAdjustment",
                newName: "ScheduleAdjustments");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleAdjustment_ReviewedById",
                table: "ScheduleAdjustments",
                newName: "IX_ScheduleAdjustments_ReviewedById");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleAdjustment_EmployeeId_Date",
                table: "ScheduleAdjustments",
                newName: "IX_ScheduleAdjustments_EmployeeId_Date");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleAdjustment_CreatedById",
                table: "ScheduleAdjustments",
                newName: "IX_ScheduleAdjustments_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScheduleAdjustments",
                table: "ScheduleAdjustments",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Absences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmployeeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PlannedStart = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    PlannedEnd = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    EndedAt = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                    ReturnConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedById = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewedById = table.Column<int>(type: "INTEGER", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Absences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Absences_Employees_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Absences_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Absences_Employees_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Absences_CreatedById",
                table: "Absences",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Absences_EmployeeId_Date",
                table: "Absences",
                columns: new[] { "EmployeeId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Absences_ReviewedById",
                table: "Absences",
                column: "ReviewedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleAdjustments_Employees_CreatedById",
                table: "ScheduleAdjustments",
                column: "CreatedById",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleAdjustments_Employees_EmployeeId",
                table: "ScheduleAdjustments",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleAdjustments_Employees_ReviewedById",
                table: "ScheduleAdjustments",
                column: "ReviewedById",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleAdjustments_Employees_CreatedById",
                table: "ScheduleAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleAdjustments_Employees_EmployeeId",
                table: "ScheduleAdjustments");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleAdjustments_Employees_ReviewedById",
                table: "ScheduleAdjustments");

            migrationBuilder.DropTable(
                name: "Absences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ScheduleAdjustments",
                table: "ScheduleAdjustments");

            migrationBuilder.RenameTable(
                name: "ScheduleAdjustments",
                newName: "ScheduleAdjustment");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleAdjustments_ReviewedById",
                table: "ScheduleAdjustment",
                newName: "IX_ScheduleAdjustment_ReviewedById");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleAdjustments_EmployeeId_Date",
                table: "ScheduleAdjustment",
                newName: "IX_ScheduleAdjustment_EmployeeId_Date");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleAdjustments_CreatedById",
                table: "ScheduleAdjustment",
                newName: "IX_ScheduleAdjustment_CreatedById");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ScheduleAdjustment",
                table: "ScheduleAdjustment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleAdjustment_Employees_CreatedById",
                table: "ScheduleAdjustment",
                column: "CreatedById",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleAdjustment_Employees_EmployeeId",
                table: "ScheduleAdjustment",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleAdjustment_Employees_ReviewedById",
                table: "ScheduleAdjustment",
                column: "ReviewedById",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
