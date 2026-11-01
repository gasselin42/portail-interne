using System.Globalization;
using PortailInterne.Api.Dtos;

namespace PortailInterne.Api.Services;

public static class MeetingTexts
{
	public static string FormatWhen(DateTime start, TimeZoneInfo timeZone)
	{
		var fr = new CultureInfo("fr-CA");
		var local = TimeZoneInfo.ConvertTimeFromUtc(start, timeZone);
		return local.ToString("dddd d MMMM", fr) + " à " + (local.Minute == 0 ? $"{local.Hour} h" : $"{local.Hour} h {local.Minute:00}");
	}
}

public static class MeetingTime
{
	public static (DateOnly First, DateOnly Last) LocalDates(DateTime start, DateTime end, TimeZoneInfo zone)
	{
		var localStart = TimeZoneInfo.ConvertTimeFromUtc(start, zone);
		var localEnd = TimeZoneInfo.ConvertTimeFromUtc(end, zone);

		if (localEnd.Hour == 0 && localEnd.Minute == 0)
			localEnd = localEnd.AddDays(-1);

		return (
			new DateOnly(localStart.Year, localStart.Month, localStart.Day),
			new DateOnly(localEnd.Year, localEnd.Month, localEnd.Day)
		);
	}

	public static OutsideHoursReason? FindOutsideHoursReason(EffectiveDay day, TimeOnly start, TimeOnly end)
	{
		if (day.Source == DaySource.NonDefini)
			return null;
		
		if (day.Source == DaySource.Ferie)
			return OutsideHoursReason.Holiday;
		
		if (day.StartTime is null || day.EndTime is null)
			return OutsideHoursReason.NotWorking;
		
		if (start < day.StartTime.Value || end > day.EndTime.Value || end == TimeOnly.MinValue)
			return OutsideHoursReason.OutsideHours;
		
		foreach (var absence in day.Absences)
		{
			var absenceStart = absence.StartedAt ?? absence.PlannedStart;
			var absenceEnd = absence.EndedAt ?? absence.PlannedEnd ?? day.EndTime;

			if (absenceStart is null) continue;

			if (absenceStart < end && absenceEnd > start)
				return OutsideHoursReason.Absence;
		}

		return null;
	}
}