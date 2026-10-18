namespace PortailInterne.Api.Models;

public class WorkSchedule
{
	public int Id { get; set; }
	public int EmployeeId { get; set; }
	public Employee Employee { get; set; } = null!; // navigation
	public DateOnly EffectiveFrom { get; set; }
	public int CreatedById { get; set; }
	public Employee CreatedBy { get; set; } = null!; // navigation
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public List<WorkScheduleDay> Days { get; set; } = [];
}