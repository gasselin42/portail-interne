using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Tests;

/// <summary>Validation d'un ajustement d'horaire (R13) et d'une absence dans la journée (R24).</summary>
public class AdjustmentAndAbsenceRulesTests
{
	// ─── Ajustement d'horaire ───────────────────────────────────────────────

	[Fact]
	public void Un_ajustement_avec_des_heures_valides_est_accepte()
	{
		Assert.Null(AdjustmentRules.Validate(Adjustment("08:00", "16:00", 30)));
	}

	[Fact]
	public void Un_jour_non_travaille_est_accepte()
	{
		Assert.Null(AdjustmentRules.Validate(Adjustment(null, null, 0)));
	}

	[Fact]
	public void Un_jour_non_travaille_n_a_pas_de_pause()
	{
		Assert.Equal("Un jour non travaillé n'a pas de pause.", AdjustmentRules.Validate(Adjustment(null, null, 30)));
	}

	[Theory]
	[InlineData("08:00", null)]
	[InlineData(null, "16:00")]
	public void Une_seule_heure_est_refusee(string? start, string? end)
	{
		Assert.Equal(
			"Indique à la fois l'heure de début et l'heure de fin.",
			AdjustmentRules.Validate(Adjustment(start, end, 0)));
	}

	[Theory]
	[InlineData("16:00", "08:00")] // fin avant le début
	[InlineData("08:00", "08:00")] // journée de 0 minute
	public void La_fin_doit_etre_apres_le_debut(string start, string end)
	{
		Assert.Equal("La fin doit être après le début.", AdjustmentRules.Validate(Adjustment(start, end, 0)));
	}

	[Fact]
	public void Une_pause_negative_est_refusee()
	{
		Assert.Equal("La pause ne peut pas être négative.", AdjustmentRules.Validate(Adjustment("08:00", "16:00", -5)));
	}

	[Theory]
	[InlineData(480)] // aussi longue que la journée (8 h à 16 h)
	[InlineData(600)] // plus longue que la journée
	public void La_pause_doit_etre_plus_courte_que_la_journee(int breakMinutes)
	{
		Assert.Equal(
			"La pause doit être plus courte que la journée.",
			AdjustmentRules.Validate(Adjustment("08:00", "16:00", breakMinutes)));
	}

	// ─── Absence ────────────────────────────────────────────────────────────

	[Fact]
	public void Une_absence_prevue_est_acceptee()
	{
		Assert.Null(AbsenceRules.Validate(Absence(planned: ("09:00", "11:00"))));
	}

	[Fact]
	public void Une_absence_non_prevue_commencee_est_acceptee()
	{
		// Urgence : « Je pars » à 15 h, pas encore de retour
		Assert.Null(AbsenceRules.Validate(Absence(startedAt: "15:00")));
	}

	[Fact]
	public void Un_retour_en_retard_sur_une_absence_prevue_est_accepte()
	{
		// Prévue de 9 h à 11 h, partie à l'heure (sans « Je pars »), revenue à midi
		Assert.Null(AbsenceRules.Validate(Absence(planned: ("09:00", "11:00"), endedAt: "12:00")));
	}

	[Fact]
	public void Un_retour_en_avance_sur_une_absence_prevue_est_accepte()
	{
		Assert.Null(AbsenceRules.Validate(Absence(planned: ("09:00", "11:00"), startedAt: "09:05", endedAt: "10:15")));
	}

	[Theory]
	[InlineData("09:00", null)]
	[InlineData(null, "11:00")]
	public void Une_seule_heure_prevue_est_refusee(string? start, string? end)
	{
		var absence = new Absence
		{
			PlannedStart = start is null ? null : TimeOnly.Parse(start),
			PlannedEnd = end is null ? null : TimeOnly.Parse(end),
		};

		Assert.Equal(
			"Indique à la fois l'heure de début et l'heure de fin, ou aucune des deux.",
			AbsenceRules.Validate(absence));
	}

	[Fact]
	public void La_fin_prevue_doit_etre_apres_le_debut_prevu()
	{
		Assert.Equal(
			"La fin prévue doit être après le début prévu.",
			AbsenceRules.Validate(Absence(planned: ("11:00", "09:00"))));
	}

	[Fact]
	public void Une_absence_non_prevue_sans_depart_est_refusee()
	{
		Assert.Equal(
			"Une absence non prévue doit avoir une heure de départ.",
			AbsenceRules.Validate(Absence(endedAt: "12:00")));
	}

	[Theory]
	[InlineData(null, "08:30")]    // retour avant le départ prévu (9 h)
	[InlineData("09:30", "09:30")] // retour à l'heure exacte du départ réel
	public void Le_retour_doit_suivre_le_depart(string? startedAt, string endedAt)
	{
		Assert.Equal(
			"Le retour doit être après le départ.",
			AbsenceRules.Validate(Absence(planned: ("09:00", "11:00"), startedAt: startedAt, endedAt: endedAt)));
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	private static ScheduleAdjustment Adjustment(string? start, string? end, int breakMinutes) => new()
	{
		StartTime = start is null ? null : TimeOnly.Parse(start),
		EndTime = end is null ? null : TimeOnly.Parse(end),
		BreakMinutes = breakMinutes,
	};

	private static Absence Absence((string Start, string End)? planned = null, string? startedAt = null, string? endedAt = null) => new()
	{
		PlannedStart = planned is null ? null : TimeOnly.Parse(planned.Value.Start),
		PlannedEnd = planned is null ? null : TimeOnly.Parse(planned.Value.End),
		StartedAt = startedAt is null ? null : TimeOnly.Parse(startedAt),
		EndedAt = endedAt is null ? null : TimeOnly.Parse(endedAt),
	};
}
