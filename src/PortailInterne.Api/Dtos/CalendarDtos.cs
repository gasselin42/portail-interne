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
}

public class CalendarResponse
{
	public List<EffectiveDayResponse> Days { get; set; } = [];
	public List<LeaveRequestResponse> Leaves { get; set; } = [];
}
