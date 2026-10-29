using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

public static class CalendarMapping
{
	private static CalendarAbsenceResponse ToCalendarAbsence(
		Absence a, TimeOnly? workdayEnd, DateOnly today, TimeOnly now, bool details)
	{
		var status = AbsenceState.Compute(a, today, now, workdayEnd);
		return new CalendarAbsenceResponse
		{
			Id = a.Id,
			Status = a.Status,
			Reason = details ? a.Reason : null,
			PlannedStart = a.PlannedStart,
			PlannedEnd = a.PlannedEnd,
			Start = status.Start,
			End = status.End,
			InProgress = status.InProgress,
			ReturnConfirmed = status.ReturnConfirmed,
		};
	}

	public static EffectiveDayResponse ToDayResponse(EffectiveDay d, DateOnly today, TimeOnly now, bool details)
	{
		return new EffectiveDayResponse
			{
				Date = d.Date,
				Source = d.Source,
				StartTime = d.StartTime,
				EndTime = d.EndTime,
				BreakMinutes = d.BreakMinutes,
				HolidayName = d.HolidayName,
				PendingAdjustment = 
					details && d.PendingAdjustment is not null
						? new PendingAdjustmentResponse
							{
								Id = d.PendingAdjustment.Id,
								StartTime = d.PendingAdjustment.StartTime,
								EndTime = d.PendingAdjustment.EndTime,
								BreakMinutes = d.PendingAdjustment.BreakMinutes
							}
						: null,
				Absences = d.Absences.Select(a => ToCalendarAbsence(a, d.EndTime, today, now, details)).ToList(),
				PendingAbsences = !details ? [] : d.PendingAbsences.Select(a => ToCalendarAbsence(a, d.EndTime, today, now, details)).ToList(),
			};
	}
}