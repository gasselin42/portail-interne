using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

/// <summary>D'où viennent les heures d'un jour (la dernière couche qui s'applique, R16).</summary>
public enum DaySource
{
	Horaire,    // l'horaire type (heures, ou jour non travaillé de l'horaire)
	Ferie,
	Ajustement,
	Conge,
	NonDefini,  // aucun horaire en vigueur ce jour-là
}

/// <summary>Un jour de l'horaire effectif. Heures vides = non travaillé.</summary>
public record EffectiveDay(
	DateOnly Date,
	DaySource Source,
	TimeOnly? StartTime,
	TimeOnly? EndTime,
	int BreakMinutes,
	string? HolidayName,
	ScheduleAdjustment? PendingAdjustment)
{
	/// <summary>Les absences actives du jour (en attente ou approuvées), triées par heure. Vide un jour de congé.</summary>
	public IReadOnlyList<Absence> Absences { get; init; } = [];

	/// <summary>Les absences demandées, pas encore approuvées : affichées, jamais effectives.</summary>
	public IReadOnlyList<Absence> PendingAbsences { get; init; } = [];
}

public static class EffectiveSchedule
{
	public static EffectiveDay ComputeDay(
			DateOnly date,
			WorkSchedule? schedule,          // l'horaire en vigueur ce jour-là, ou null
			Holiday? holiday,                // le férié du site ce jour-là, ou null
			ScheduleAdjustment? approved,    // l'ajustement approuvé, ou null
			ScheduleAdjustment? pending,     // l'ajustement en attente, ou null
			bool onApprovedLeave)            // un congé approuvé couvre-t-il ce jour ?
	{
		// 1. L'horaire type
		DaySource source;
		TimeOnly? start = null;
		TimeOnly? end = null;
		var breakMinutes = 0;

		if (schedule is null)
			source = DaySource.NonDefini;
		else
		{
			source = DaySource.Horaire;
			var day = schedule.Days.FirstOrDefault(d => d.DayOfWeek == date.DayOfWeek);
			if (day is not null) // sinon : jour hors de l'horaire, donc non travaillé
			{
				start = day.StartTime;
				end = day.EndTime;
				breakMinutes = day.BreakMinutes;
			}
		}

		// 2. Le férié : non travaillé
		if (holiday is not null)
		{
			source = DaySource.Ferie;
			(start, end, breakMinutes) = (null, null, 0);
		}

		// 3. L'ajustement approuvé : ses heures (vides = non travaillé), même un férié
		if (approved is not null)
		{
			source = DaySource.Ajustement;
			(start, end, breakMinutes) = (approved.StartTime, approved.EndTime, approved.BreakMinutes);
		}

		// 4. Le congé approuvé : non travaillé, par-dessus tout le reste
		if (onApprovedLeave)
		{
			source = DaySource.Conge;
			(start, end, breakMinutes) = (null, null, 0);
		}

		return new EffectiveDay(date, source, start, end, breakMinutes, holiday?.Name, pending);
	}
}
