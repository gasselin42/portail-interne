namespace PortailInterne.Api.Models;

public class EmergencyContact
{
	public int Id { get; set; }
	public int EmployeeId { get; set; }
	public Employee Employee { get; set; } = null!;
	public string Name { get; set; } = string.Empty;
	public string Relationship { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public int SortOrder { get; set; }
}