namespace PortailInterne.Api.Models;

public enum LeaveType
{
	Conge,
	Maladie
}

public enum LeaveStatus
{
	EnAttente,
	Approuve,
	Refuse,
	Annule
}

public class LeaveRequest
{
	public int Id { get; set; }

	public int EmployeeId { get; set; }
	public Employee Employee { get; set; } = null!;

	public DateTime StartDate { get; set; }
	public DateTime EndDate { get; set; }

	public LeaveType Type { get; set; }
	public LeaveStatus Status { get; set; } = LeaveStatus.EnAttente;

	public string? Reason { get; set; } = string.Empty;

	public int? ReviewedById { get; set; }
	public Employee? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

