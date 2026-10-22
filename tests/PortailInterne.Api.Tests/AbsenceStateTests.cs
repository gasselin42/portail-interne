using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Où en est une absence (R24). Règle clé : si l'employé a cliqué « Je pars », on attend son
/// « Je suis de retour » (au plus tard la fin de la journée) ; sinon, l'absence suit le plan.
/// Journée de travail : jusqu'à 17 h. « Aujourd'hui » = 2 novembre 2026.
/// </summary>
public class AbsenceStateTests
{
	private static readonly DateOnly Today = new(2026, 11, 2);
	private static readonly TimeOnly EndOfWorkday = new(17, 0);

	// ─── Absence prévue, rien cliqué : elle suit le plan ────────────────────

	[Fact]
	public void Une_absence_prevue_est_en_cours_pendant_ses_heures()
	{
		var status = Compute(Absence(planned: ("09:00", "11:00")), now: "10:00");

		Assert.True(status.InProgress);
		Assert.Equal(new TimeOnly(9, 0), status.Start);
		Assert.Null(status.End); // encore inconnue
	}

	[Fact]
	public void Une_absence_prevue_se_termine_a_l_heure_prevue()
	{
		var status = Compute(Absence(planned: ("09:00", "11:00")), now: "14:00");

		Assert.False(status.InProgress);
		Assert.Equal(new TimeOnly(11, 0), status.End);
		Assert.True(status.ReturnConfirmed); // rien ne contredit le plan
	}

	[Fact]
	public void Une_absence_prevue_pas_encore_commencee_n_est_pas_en_cours()
	{
		var status = Compute(Absence(planned: ("09:00", "11:00")), now: "08:00");

		Assert.False(status.InProgress);
	}

	[Fact]
	public void Une_absence_prevue_demain_n_est_pas_en_cours_et_garde_ses_heures()
	{
		var tomorrow = Absence(planned: ("09:00", "11:00"), date: Today.AddDays(1));

		var status = Compute(tomorrow, now: "10:00");

		Assert.False(status.InProgress);
		Assert.Equal(new TimeOnly(11, 0), status.End);
	}

	// ─── « Je suis de retour » cliqué ───────────────────────────────────────

	[Fact]
	public void Un_retour_en_retard_donne_la_vraie_heure_de_retour()
	{
		var status = Compute(Absence(planned: ("09:00", "11:00"), endedAt: "12:00"), now: "14:00");

		Assert.False(status.InProgress);
		Assert.Equal(new TimeOnly(12, 0), status.End);
		Assert.True(status.ReturnConfirmed);
	}

	[Fact]
	public void Un_retour_en_avance_donne_la_vraie_heure_de_retour()
	{
		var status = Compute(Absence(planned: ("09:00", "11:00"), startedAt: "09:05", endedAt: "10:15"), now: "10:30");

		Assert.Equal(new TimeOnly(9, 5), status.Start);
		Assert.Equal(new TimeOnly(10, 15), status.End);
	}

	// ─── « Je pars » cliqué, pas encore de retour ───────────────────────────

	[Fact]
	public void Apres_je_pars_l_absence_prevue_reste_en_cours_apres_la_fin_prevue()
	{
		// Parti à 9 h pour un rendez-vous prévu jusqu'à 11 h, retenu : il est midi
		var status = Compute(Absence(planned: ("09:00", "11:00"), startedAt: "09:00"), now: "12:00");

		Assert.True(status.InProgress);
	}

	[Fact]
	public void Une_urgence_est_en_cours_jusqu_au_retour()
	{
		var status = Compute(Absence(startedAt: "15:00"), now: "16:00");

		Assert.True(status.InProgress);
		Assert.Equal(new TimeOnly(15, 0), status.Start);
	}

	[Fact]
	public void Sans_retour_l_urgence_se_ferme_a_la_fin_de_la_journee_non_confirmee()
	{
		var status = Compute(Absence(startedAt: "15:00"), now: "17:30");

		Assert.False(status.InProgress);
		Assert.Equal(EndOfWorkday, status.End);
		Assert.False(status.ReturnConfirmed);
	}

	[Fact]
	public void Le_lendemain_une_urgence_sans_retour_est_fermee_non_confirmee()
	{
		var yesterday = Absence(startedAt: "15:00", date: Today.AddDays(-1));

		var status = Compute(yesterday, now: "09:00");

		Assert.False(status.InProgress);
		Assert.Equal(EndOfWorkday, status.End);
		Assert.False(status.ReturnConfirmed);
	}

	[Fact]
	public void Un_jour_non_travaille_l_urgence_dure_jusqu_a_minuit()
	{
		var status = AbsenceState.Compute(Absence(startedAt: "15:00"), Today, new TimeOnly(22, 0), workdayEnd: null);

		Assert.True(status.InProgress);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	private static AbsenceStatus Compute(Absence absence, string now) =>
		AbsenceState.Compute(absence, Today, TimeOnly.Parse(now), EndOfWorkday);

	private static Absence Absence(
		(string Start, string End)? planned = null,
		string? startedAt = null,
		string? endedAt = null,
		DateOnly? date = null) => new()
	{
		Date = date ?? Today,
		PlannedStart = planned is null ? null : TimeOnly.Parse(planned.Value.Start),
		PlannedEnd = planned is null ? null : TimeOnly.Parse(planned.Value.End),
		StartedAt = startedAt is null ? null : TimeOnly.Parse(startedAt),
		EndedAt = endedAt is null ? null : TimeOnly.Parse(endedAt),
	};
}
