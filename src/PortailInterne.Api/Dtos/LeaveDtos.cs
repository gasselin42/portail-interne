using System.ComponentModel.DataAnnotations;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Dtos;

public class CreateLeaveRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public LeaveType Type { get; set; }

    public string? Reason { get; set; }
}

public class LeaveRequestResponse
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeFirstName { get; set; } = string.Empty;
    public string EmployeeLastName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public LeaveType Type { get; set; }
    public LeaveStatus Status { get; set; }
    public string? Reason { get; set; }
	public int? WorkingDays { get; set; }
	public List<DateOnly>? UnpaidDates { get; set; }
    public DateTime CreatedAt { get; set; }
	public string? CreatedByFirstName { get; set; }
	public string? CreatedByLastName { get; set; }
}

public class ReviewLeaveRequest
{
    [Required]
    public LeaveStatus Status { get; set; }
}

public class DeclareSickLeaveRequest
{
	[Required]
	public int EmployeeId { get; set; }

	[Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public string? Reason { get; set; }
}

public class LeaveBalanceResponse
{
	public int Year { get; set; }
	public int AnnualDays { get; set; }
	public int ApprovedDays { get; set; }
	public int PendingDays { get; set; }
	public int RemainingDays { get; set; }
	public int UnpaidDays { get; set; }
}

public class LeavePreviewResponse
{
	public int WorkingDays { get; set; }
	public List<DateOnly> UnpaidDates { get; set; } = [];
}