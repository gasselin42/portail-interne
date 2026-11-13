using PortailInterne.Api.Models;

namespace PortailInterne.Api.Dtos;

public class PreferencesDto
{
	public CalendarHourRange CalendarHourRange { get; set; }
	public CalendarDensity CalendarDensity { get; set; }
}