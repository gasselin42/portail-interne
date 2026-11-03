using System.Globalization;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

public static class MeetingTexts
{
	public static string FormatWhen(DateTime start, TimeZoneInfo timeZone)
	{
		var fr = new CultureInfo("fr-CA");
		var local = TimeZoneInfo.ConvertTimeFromUtc(start, timeZone);
		return local.ToString("dddd d MMMM", fr) + " à " + (local.Minute == 0 ? $"{local.Hour} h" : $"{local.Hour} h {local.Minute:00}");
	}

	public static Notification Notification(int recipientId, NotificationType type, DateTime start, TimeZoneInfo zone, string message)
	{
		var localStart = TimeZoneInfo.ConvertTimeFromUtc(start, zone);
		return new Notification
		{
			RecipientId = recipientId,
			Type = type,
			Message = message,
			Link = $"/calendar?vue=jour&date={localStart:yyyy-MM-dd}"
		};
	}

	public static string MissedPart(DateTime absenceStart, DateTime absenceEnd, DateTime meetingStart, DateTime meetingEnd, string title)
	{
		var coversStart = absenceStart <= meetingStart;
		var coversEnd = absenceEnd >= meetingEnd;

		if (coversStart && coversEnd) return $"« {title} »";
		if (coversStart) return $"le début de « {title} »";
		if (coversEnd) return $"la fin de « {title} »";
		return $"une partie de « {title} »";
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

	public static DateTime ToUtc(DateOnly date, TimeOnly time, TimeZoneInfo zone)
	{
		var local = date.ToDateTime(time);
		if (zone.IsInvalidTime(local))
			local = local.AddHours(1);
		return TimeZoneInfo.ConvertTimeToUtc(local, zone);
	}
}

public static class MeetingMapping
{
	public static MeetingResponse ToResponse(Meeting meeting, string? color)
	{
		return new MeetingResponse
		{
			Id = meeting.Id,
			Kind = meeting.Kind,
			Title = meeting.Title,
			Description = meeting.Description,
			StartAt = meeting.StartAt,
			EndAt = meeting.EndAt,
			OrganizerId = meeting.OrganizerId,
			IsTeamMeeting = meeting.IsTeamMeeting,
			IsFlexible = meeting.IsFlexible,
			Color = color ?? MeetingColors.Default
		};
	}
}