namespace PortailInterne.Api.Models;

public class MeetingParticipant
{
	public int MeetingId { get; set; }
	public Meeting Meeting { get; set; } = null!;
	public int EmployeeId { get; set; }
	public Employee Employee { get; set; } = null!;
	public string? Color { get; set; }
}