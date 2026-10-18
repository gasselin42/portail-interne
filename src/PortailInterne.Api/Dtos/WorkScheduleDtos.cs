using System.ComponentModel.DataAnnotations;

namespace PortailInterne.Api.Dtos;

public class WorkScheduleDayDto
{
	public DayOfWeek DayOfWeek { get; set; }
	public TimeOnly StartTime { get; set; }
	public TimeOnly EndTime { get; set; }
	public int BreakMinutes { get; set; }
}

public class SaveWorkScheduleRequest
{
	[Required]
	public DateOnly? EffectiveFrom { get; set; }

	[Required]
	public List<WorkScheduleDayDto> Days { get; set; } = [];
}

public class WorkScheduleResponse
{
	public int Id { get; set; }
	public DateOnly EffectiveFrom { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedByFirstName { get; set; } = string.Empty;
	public string CreatedByLastName { get; set; } = string.Empty;
	public bool IsEditable { get; set; }
	public List<WorkScheduleDayDto> Days { get; set; } = [];
}