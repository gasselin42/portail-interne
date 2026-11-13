namespace PortailInterne.Api.Models;

public class MeetingParticipant
{
	public int MeetingId { get; set; }
	public Meeting Meeting { get; set; } = null!;
	public int EmployeeId { get; set; }
	public Employee Employee { get; set; } = null!;
	public ParticipantResponse Response { get; set; }
	public string? Color { get; set; }
}

public enum ParticipantResponse
{
	EnAttente = 0,
	Accepte = 1,
	Decline = 2
}