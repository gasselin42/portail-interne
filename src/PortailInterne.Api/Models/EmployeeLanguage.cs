namespace PortailInterne.Api.Models;

public class EmployeeLanguage
{
	public int EmployeeId { get; set; }
	public Employee Employee { get; set; } = null!;
	public string LanguageCode { get; set; } = string.Empty;
}