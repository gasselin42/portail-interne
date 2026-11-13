namespace PortailInterne.Api.Models;

public enum CalendarHourRange
{
	WorkHours = 0,
	FullDay = 1
}

public enum CalendarDensity
{
	Normal = 0,
	Compact = 1,
	Airy = 2
}

public class EmployeePreferences
{
	public int EmployeeId { get; set; }
	public Employee Employee { get; set; } = null!;
	public CalendarHourRange CalendarHourRange { get; set; } = CalendarHourRange.WorkHours;
	public CalendarDensity CalendarDensity { get; set; } = CalendarDensity.Normal;
}