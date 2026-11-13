using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortailInterne.Api.Migrations
{
    /// <inheritdoc />
    public partial class ParticipantResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Response",
                table: "MeetingParticipants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

			// L'organisateur participe d'office : ses lignes existantes passent à Accepte (1).
			migrationBuilder.Sql("""
				UPDATE MeetingParticipants
				SET Response = 1
				WHERE EmployeeId = (SELECT OrganizerId FROM Meetings WHERE Meetings.Id = MeetingParticipants.MeetingId);
				""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Response",
                table: "MeetingParticipants");
        }
    }
}
