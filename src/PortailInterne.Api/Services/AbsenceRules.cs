using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

public static class AbsenceRules
{
	public static string? Validate(Absence absence)
	{
		if (absence.PlannedStart.HasValue != absence.PlannedEnd.HasValue)
			return "Indique à la fois l'heure de début et l'heure de fin, ou aucune des deux.";

		if (absence.PlannedStart is not null && absence.PlannedEnd <= absence.PlannedStart)
			return "La fin prévue doit être après le début prévu.";

		// Sans plan, c'est une absence non prévue : elle commence forcément par « Je pars »
		if (absence.PlannedStart is null && absence.StartedAt is null)
			return "Une absence non prévue doit avoir une heure de départ.";

		// Le retour doit suivre le départ, réel s'il existe, sinon prévu
		var start = absence.StartedAt ?? absence.PlannedStart;
		if (absence.EndedAt is not null && absence.EndedAt <= start)
			return "Le retour doit être après le départ.";

		return null;
	}
}