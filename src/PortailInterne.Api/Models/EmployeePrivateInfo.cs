namespace PortailInterne.Api.Models;

public class EmployeePrivateInfo
{
	public int EmployeeId { get; set; }
	public Employee Employee { get; set; } = null!;
	public string? PersonalMobilePhone { get; set; }
	public string? PersonalEmail { get; set; }
	public string? AddressLine1 { get; set; }
	public string? AddressLine2 { get; set; }
	public string? City { get; set; }
	public string? Region { get; set; }
	public string? PostalCode { get; set; }
	public string? Country { get; set; }
	public int? BirthMonth { get; set; }
	public int? BirthDay { get; set; }
	public bool ShowBirthday { get; set; }
	public DateTime UpdatedAt { get; set; }
}