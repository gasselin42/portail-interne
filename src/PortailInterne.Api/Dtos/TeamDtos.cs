namespace PortailInterne.Api.Dtos;

public class TeamMemberResponse
{
	public int EmployeeId { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string JobTitle { get; set; } = string.Empty;
	public bool Details { get; set; }
	public List<EffectiveDayResponse> Days { get; set; } = [];
}