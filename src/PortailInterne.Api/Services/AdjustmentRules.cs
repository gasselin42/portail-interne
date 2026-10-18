using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

public static class AdjustmentRules
{
	public static string? Validate(ScheduleAdjustment adjustment)
	{
		if (adjustment.StartTime.HasValue != adjustment.EndTime.HasValue)
			return "Indique à la fois l'heure de début et l'heure de fin.";

		// Jour non travaillé : pas d'heures, donc pas de pause
		if (adjustment.StartTime is null)
			return adjustment.BreakMinutes == 0 ? null : "Un jour non travaillé n'a pas de pause.";

		var start = adjustment.StartTime.Value;
		var end = adjustment.EndTime!.Value;

		if (end <= start)
			return "La fin doit être après le début.";

		if (adjustment.BreakMinutes < 0)
			return "La pause ne peut pas être négative.";

		if (adjustment.BreakMinutes >= (end - start).TotalMinutes)
			return "La pause doit être plus courte que la journée.";

		return null;
	}
}