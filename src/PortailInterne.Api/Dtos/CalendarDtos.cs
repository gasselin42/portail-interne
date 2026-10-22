using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Dtos;

/// <summary>Un ajustement en attente : affiché au calendrier, mais pas encore appliqué.</summary>
public class PendingAdjustmentResponse
{
	public int Id { get; set; }
	public TimeOnly? StartTime { get; set; }
	public TimeOnly? EndTime { get; set; }
	public int BreakMinutes { get; set; }
}

/// <summary>Un jour du calendrier : ses heures de travail effectives (R16).</summary>
public class EffectiveDayResponse
{
	public DateOnly Date { get; set; }
	public DaySource Source { get; set; }
	public TimeOnly? StartTime { get; set; }
	public TimeOnly? EndTime { get; set; }
	public int BreakMinutes { get; set; }
	public string? HolidayName { get; set; }
	public PendingAdjustmentResponse? PendingAdjustment { get; set; }
	public List<CalendarAbsenceResponse> Absences { get; set; } = [];
	public List<CalendarAbsenceResponse> PendingAbsences { get; set; } = [];
}

public class CalendarResponse
{
	public List<EffectiveDayResponse> Days { get; set; } = [];
	public List<LeaveRequestResponse> Leaves { get; set; } = [];
}

public class CalendarAbsenceResponse
{
	public int Id { get; set; }
	public LeaveStatus Status { get; set; }
	public string? Reason { get; set; }
	public TimeOnly? PlannedStart { get; set; }
	public TimeOnly? PlannedEnd { get; set; }
	public TimeOnly Start { get; set; }
	public TimeOnly? End { get; set; }
	public bool InProgress { get; set; }
	public bool ReturnConfirmed { get; set; }
}
