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
    public DateTime CreatedAt { get; set; }
}

public class ReviewLeaveRequest
{
    [Required]
    public LeaveStatus Status { get; set; }
}