namespace PortailInterne.Api.Models;

public class WorkScheduleDay
{
	public int WorkScheduleId { get; set; }
	public DayOfWeek DayOfWeek { get; set; }
	public TimeOnly StartTime { get; set; }
	public TimeOnly EndTime { get; set; }
	public int BreakMinutes { get; set; }
}