namespace PortailInterne.Api.Dtos;

public class BirthdayResponse
{
	public int EmployeeId { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public string? PhotoUrl { get; set; }
	public DateOnly Date { get; set; }
}