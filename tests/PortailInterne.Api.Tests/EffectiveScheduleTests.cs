using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Horaire effectif d'un jour (R16) : horaire type → férié → ajustement approuvé → congé approuvé.
/// Horaire de test : lundi au jeudi, 8 h à 17 h, 30 min de pause.
/// </summary>
public class EffectiveScheduleTests
{
	private static readonly DateOnly Monday = new(2026, 11, 2);
	private static readonly DateOnly Friday = new(2026, 11, 6);

	// ─── Horaire type ───────────────────────────────────────────────────────

	[Fact]
	public void Un_jour_de_l_horaire_donne_ses_heures()
	{
		var day = EffectiveSchedule.ComputeDay(Monday, MondayToThursday(), null, null, null, false);

		Assert.Equal(DaySource.Horaire, day.Source);
		Assert.Equal(new TimeOnly(8, 0), day.StartTime);
		Assert.Equal(new TimeOnly(17, 0), day.EndTime);
		Assert.Equal(30, day.BreakMinutes);
	}

	[Fact]
	public void Un_jour_hors_de_l_horaire_n_est_pas_travaille()
	{
		var day = EffectiveSchedule.ComputeDay(Friday, MondayToThursday(), null, null, null, false);

		Assert.Equal(DaySource.Horaire, day.Source);
		Assert.Null(day.StartTime);
		Assert.Null(day.EndTime);
	}

	[Fact]
	public void Sans_horaire_le_jour_est_non_defini()
	{
		var day = EffectiveSchedule.ComputeDay(Monday, null, null, null, null, false);

		Assert.Equal(DaySource.NonDefini, day.Source);
		Assert.Null(day.StartTime);
	}

	// ─── Férié ──────────────────────────────────────────────────────────────

	[Fact]
	public void Un_ferie_n_est_pas_travaille_et_garde_son_nom()
	{
		var day = EffectiveSchedule.ComputeDay(Monday, MondayToThursday(), Holiday("Jour du test"), null, null, false);

		Assert.Equal(DaySource.Ferie, day.Source);
		Assert.Null(day.StartTime);
		Assert.Equal(0, day.BreakMinutes);
		Assert.Equal("Jour du test", day.HolidayName);
	}

	// ─── Ajustement ─────────────────────────────────────────────────────────

	[Fact]
	public void Un_ajustement_approuve_remplace_les_heures()
	{
		var day = EffectiveSchedule.ComputeDay(Monday, MondayToThursday(), null, Adjustment("09:00", "15:00"), null, false);

		Assert.Equal(DaySource.Ajustement, day.Source);
		Assert.Equal(new TimeOnly(9, 0), day.StartTime);
		Assert.Equal(new TimeOnly(15, 0), day.EndTime);
	}

	[Fact]
	public void Un_ajustement_fait_travailler_un_ferie()
	{
		var day = EffectiveSchedule.ComputeDay(Monday, MondayToThursday(), Holiday("Jour du test"), Adjustment("10:00", "14:00"), null, false);

		Assert.Equal(DaySource.Ajustement, day.Source);
		Assert.Equal(new TimeOnly(10, 0), day.StartTime);
		Assert.Equal("Jour du test", day.HolidayName); // le calendrier peut encore l'afficher
	}

	[Fact]
	public void Un_ajustement_fait_travailler_un_jour_hors_de_l_horaire()
	{
		var day = EffectiveSchedule.ComputeDay(Friday, MondayToThursday(), null, Adjustment("08:00", "12:00"), null, false);

		Assert.Equal(DaySource.Ajustement, day.Source);
		Assert.Equal(new TimeOnly(12, 0), day.EndTime);
	}

	[Fact]
	public void Un_ajustement_peut_retirer_une_journee()
	{
		var day = EffectiveSchedule.ComputeDay(Monday, MondayToThursday(), null, Adjustment(null, null), null, false);

		Assert.Equal(DaySource.Ajustement, day.Source);
		Assert.Null(day.StartTime);
	}

	[Fact]
	public void Un_ajustement_en_attente_est_affiche_mais_pas_applique()
	{
		var pending = Adjustment("09:00", "15:00");

		var day = EffectiveSchedule.ComputeDay(Monday, MondayToThursday(), null, null, pending, false);

		Assert.Equal(DaySource.Horaire, day.Source);
		Assert.Equal(new TimeOnly(8, 0), day.StartTime); // toujours les heures de l'horaire
		Assert.Same(pending, day.PendingAdjustment);
	}

	// ─── Congé ──────────────────────────────────────────────────────────────

	[Fact]
	public void Un_conge_approuve_l_emporte_sur_tout()
	{
		var day = EffectiveSchedule.ComputeDay(Monday, MondayToThursday(), Holiday("Jour du test"), Adjustment("10:00", "14:00"), null, true);

		Assert.Equal(DaySource.Conge, day.Source);
		Assert.Null(day.StartTime);
		Assert.Equal(0, day.BreakMinutes);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	private static WorkSchedule MondayToThursday() => new()
	{
		EffectiveFrom = new DateOnly(2026, 10, 1),
		Days =
		[
			.. new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }
				.Select(d => new WorkScheduleDay { DayOfWeek = d, StartTime = new(8, 0), EndTime = new(17, 0), BreakMinutes = 30 }),
		],
	};

	private static Holiday Holiday(string name) => new() { Date = Monday, Name = name };

	private static ScheduleAdjustment Adjustment(string? start, string? end) => new()
	{
		StartTime = start is null ? null : TimeOnly.Parse(start),
		EndTime = end is null ? null : TimeOnly.Parse(end),
		BreakMinutes = start is null ? 0 : 15,
	};
}
