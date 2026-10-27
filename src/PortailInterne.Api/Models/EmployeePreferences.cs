namespace PortailInterne.Api.Models;

public enum CalendarHourRange
{
	WorkHours = 0,
	FullDay = 1
}

public class EmployeePreferences
{
	public int EmployeeId { get; set; }
	public Employee Employee { get; set; } = null!;
	public CalendarHourRange CalendarHourRange { get; set; } = CalendarHourRange.WorkHours;
}