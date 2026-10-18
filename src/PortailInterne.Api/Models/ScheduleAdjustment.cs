namespace PortailInterne.Api.Models;

public class ScheduleAdjustment
{
	public int Id { get; set; }
	public int EmployeeId { get; set; }
	public Employee Employee { get; set; } = null!;
	public DateOnly Date { get; set; }
	public TimeOnly? StartTime { get; set; }
	public TimeOnly? EndTime { get; set; }
	public int BreakMinutes { get; set; }
	public string? Reason { get; set; }
	public LeaveStatus Status { get; set; }
	public int CreatedById { get; set; }
	public Employee CreatedBy { get; set; } = null!;
	public int? ReviewedById { get; set; }
	public Employee? ReviewedBy { get; set; }
	public DateTime? ReviewedAt { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}