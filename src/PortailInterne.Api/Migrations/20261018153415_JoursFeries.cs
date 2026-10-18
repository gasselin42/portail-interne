using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PortailInterne.Api.Migrations
{
    /// <inheritdoc />
    public partial class JoursFeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SiteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Holidays_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Holidays",
                columns: new[] { "Id", "Date", "Name", "SiteId" },
                values: new object[,]
                {
                    { 1, new DateOnly(2026, 1, 1), "Jour de l'An", 1 },
                    { 2, new DateOnly(2026, 4, 3), "Vendredi saint", 1 },
                    { 3, new DateOnly(2026, 4, 6), "Lundi de Pâques", 1 },
                    { 4, new DateOnly(2026, 5, 18), "Journée nationale des patriotes", 1 },
                    { 5, new DateOnly(2026, 6, 24), "Fête nationale", 1 },
                    { 6, new DateOnly(2026, 7, 1), "Fête du Canada", 1 },
                    { 7, new DateOnly(2026, 9, 7), "Fête du Travail", 1 },
                    { 8, new DateOnly(2026, 10, 12), "Action de grâce", 1 },
                    { 9, new DateOnly(2026, 12, 25), "Noël", 1 },
                    { 10, new DateOnly(2027, 1, 1), "Jour de l'An", 1 },
                    { 11, new DateOnly(2027, 3, 26), "Vendredi saint", 1 },
                    { 12, new DateOnly(2027, 3, 29), "Lundi de Pâques", 1 },
                    { 13, new DateOnly(2027, 5, 24), "Journée nationale des patriotes", 1 },
                    { 14, new DateOnly(2027, 6, 24), "Fête nationale", 1 },
                    { 15, new DateOnly(2027, 7, 1), "Fête du Canada", 1 },
                    { 16, new DateOnly(2027, 9, 6), "Fête du Travail", 1 },
                    { 17, new DateOnly(2027, 10, 11), "Action de grâce", 1 },
                    { 18, new DateOnly(2027, 12, 25), "Noël", 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_SiteId_Date",
                table: "Holidays",
                columns: new[] { "SiteId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Holidays");
        }
    }
}
