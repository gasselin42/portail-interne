namespace PortailInterne.Api.Dtos;

public class PendingAdjustmentItem
{
	public int Id { get; set; }
	public int EmployeeId { get; set; }
	public string EmployeeFirstName { get; set; } = string.Empty;
	public string EmployeeLastName { get; set; } = string.Empty;
	public DateOnly Date { get; set; }
	public TimeOnly? StartTime { get; set; }
	public TimeOnly? EndTime { get; set; }
	public int BreakMinutes { get; set; }
	public string? Reason { get; set; }
	public DateTime CreatedAt { get; set; }
	public bool CanApprove { get; set; }
}

public class PendingAbsenceItem
{
	public int Id { get; set; }
	public int EmployeeId { get; set; }
	public string EmployeeFirstName { get; set; } = string.Empty;
	public string EmployeeLastName { get; set; } = string.Empty;
	public DateOnly Date { get; set; }
	public TimeOnly? PlannedStart { get; set; }
	public TimeOnly? PlannedEnd { get; set; }
	public string? Reason { get; set; }
	public DateTime CreatedAt { get; set; }
	public bool CanApprove { get; set; }
}

public class ApprovalQueueResponse
{
	public List<PendingAdjustmentItem> Adjustments { get; set; } = [];
	public List<PendingAbsenceItem> Absences { get; set; } = [];
}
