namespace PortailInterne.Api.Models;

public enum MeetingKind
{
	Meeting = 0,
	PersonalBlock = 1
}

public class Meeting
{
	public int Id { get; set; }
	public MeetingKind Kind { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }
	public DateTime StartAt { get; set; }
	public DateTime EndAt { get; set; }
	public int OrganizerId { get; set; }
	public Employee Organizer { get; set; } = null!;
	public bool IsTeamMeeting { get; set; }
	public bool IsFlexible { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public List<MeetingParticipant> Participants { get; set; } = [];
}