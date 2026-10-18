using System.Globalization;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

public static class WorkScheduleRules
{
	private static readonly CultureInfo FrenchCanada = new("fr-CA");

	public static string? Validate(IReadOnlyList<WorkScheduleDay> days)
	{
		if (days.Count == 0)
			return "Un horaire doit avoir au moins un jour travaillé.";

		var seen = new HashSet<DayOfWeek>();
		foreach (var day in days)
		{			
			var dayOfWeek = FrenchCanada.DateTimeFormat.GetDayName(day.DayOfWeek);
			var dayName = char.ToUpper(dayOfWeek[0]) + dayOfWeek[1..];

			if (!seen.Add(day.DayOfWeek))
				return $"{dayName} apparaît deux fois.";

			if (day.EndTime <= day.StartTime)
				return $"{dayName} : la fin doit être après le début.";
			
			if (day.BreakMinutes < 0)
				return $"{dayName} : la pause ne peut pas être négative.";
			
			if (day.BreakMinutes >= (day.EndTime - day.StartTime).TotalMinutes)
				return $"{dayName} : la pause doit être plus courte que la journée.";
		}

		return null;
	}
}