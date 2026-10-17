using PortailInterne.Api.Services;

namespace PortailInterne.Api.Tests;

/// <summary>Règle R19 : solde annuel, jours ouvrables et jours sans solde.</summary>
public class LeaveBalanceTests
{
	// ─── Jours ouvrables ────────────────────────────────────────────────────

	[Fact]
	public void Une_semaine_complete_compte_cinq_jours_ouvrables()
	{
		// Du lundi 16 au dimanche 22 novembre 2026
		var leave = Leave(1, "2026-11-16", "2026-11-22", "2026-10-01");

		var usage = LeaveBalance.ComputeUsage(20, [leave]);

		Assert.Equal(5, usage[1].WorkingDays);
		Assert.Empty(usage[1].UnpaidDates);
	}

	[Fact]
	public void Un_conge_d_un_jour_compte_un_jour_ouvrable()
	{
		var leave = Leave(1, "2026-11-18", "2026-11-18", "2026-10-01");

		var usage = LeaveBalance.ComputeUsage(20, [leave]);

		Assert.Equal(1, usage[1].WorkingDays);
	}

	[Fact]
	public void Une_fin_de_semaine_seule_ne_compte_aucun_jour()
	{
		// Samedi 21 et dimanche 22 novembre 2026
		var leave = Leave(1, "2026-11-21", "2026-11-22", "2026-10-01");

		var usage = LeaveBalance.ComputeUsage(20, [leave]);

		Assert.Equal(0, usage[1].WorkingDays);
		Assert.Empty(usage[1].UnpaidDates);
	}

	// ─── Dépassement du solde ───────────────────────────────────────────────

	[Theory]
	[InlineData(3, 2)] // dépassement : 2 jours sans solde
	[InlineData(5, 0)] // limite exacte : tout le solde utilisé, aucun dépassement
	[InlineData(0, 5)] // aucun solde : tout est sans solde
	public void Les_jours_au_dela_du_solde_sont_sans_solde(int annualDays, int expectedUnpaid)
	{
		// Du lundi 16 au vendredi 20 novembre 2026
		var leave = Leave(1, "2026-11-16", "2026-11-20", "2026-10-01");

		var usage = LeaveBalance.ComputeUsage(annualDays, [leave]);

		Assert.Equal(5, usage[1].WorkingDays);
		Assert.Equal(expectedUnpaid, usage[1].UnpaidDates.Count);
	}

	[Fact]
	public void Les_jours_sans_solde_sont_les_derniers_du_conge()
	{
		var leave = Leave(1, "2026-11-16", "2026-11-20", "2026-10-01");

		var usage = LeaveBalance.ComputeUsage(3, [leave]);

		Assert.Equal([new DateOnly(2026, 11, 19), new DateOnly(2026, 11, 20)], usage[1].UnpaidDates);
	}

	// ─── Ordre de création ──────────────────────────────────────────────────

	[Fact]
	public void La_demande_creee_en_dernier_porte_les_jours_sans_solde()
	{
		// A a lieu plus tard dans l'année, mais elle a été créée en premier
		var a = Leave(1, "2026-11-16", "2026-11-20", "2026-10-01");
		var b = Leave(2, "2026-11-02", "2026-11-02", "2026-10-05");

		var usage = LeaveBalance.ComputeUsage(5, [a, b]);

		Assert.Empty(usage[1].UnpaidDates);
		Assert.Equal([new DateOnly(2026, 11, 2)], usage[2].UnpaidDates);
	}

	[Fact]
	public void L_ordre_de_la_liste_n_a_aucune_importance()
	{
		var a = Leave(1, "2026-11-16", "2026-11-20", "2026-10-01");
		var b = Leave(2, "2026-11-02", "2026-11-02", "2026-10-05");

		var usage = LeaveBalance.ComputeUsage(5, [b, a]);

		Assert.Empty(usage[1].UnpaidDates);
		Assert.Equal([new DateOnly(2026, 11, 2)], usage[2].UnpaidDates);
	}

	[Fact]
	public void A_creation_simultanee_le_plus_petit_id_passe_en_premier()
	{
		var first = Leave(1, "2026-11-16", "2026-11-16", "2026-10-01T09:00:00");
		var second = Leave(2, "2026-11-17", "2026-11-17", "2026-10-01T09:00:00");

		var usage = LeaveBalance.ComputeUsage(1, [second, first]);

		Assert.Empty(usage[1].UnpaidDates);
		Assert.Equal([new DateOnly(2026, 11, 17)], usage[2].UnpaidDates);
	}

	// ─── Années civiles ─────────────────────────────────────────────────────

	[Fact]
	public void Un_conge_a_cheval_puise_dans_le_solde_de_chaque_annee()
	{
		// Solde de 6 jours par année.
		// Un congé plus ancien (mardi 1er au vendredi 4 décembre) consomme 4 jours de 2026 : il en reste 2.
		var earlier = Leave(1, "2026-12-01", "2026-12-04", "2026-10-01");
		// Du lundi 28 décembre 2026 au vendredi 8 janvier 2027 :
		// 4 jours en 2026 (28 au 31), 6 jours en 2027 (1er, puis 4 au 8)
		var leave = Leave(2, "2026-12-28", "2027-01-08", "2026-10-05");

		var usage = LeaveBalance.ComputeUsage(6, [earlier, leave]);

		Assert.Equal(10, usage[2].WorkingDays);
		// 2026 n'a plus que 2 jours : le 30 et le 31 dépassent.
		// Janvier puise dans le solde neuf de 2027 (6 jours) : tout est payé.
		Assert.Equal([new DateOnly(2026, 12, 30), new DateOnly(2026, 12, 31)], usage[2].UnpaidDates);
	}

	// ─── Cas limites ────────────────────────────────────────────────────────

	[Fact]
	public void Une_liste_vide_donne_un_resultat_vide()
	{
		var usage = LeaveBalance.ComputeUsage(20, []);

		Assert.Empty(usage);
	}

	[Fact]
	public void CountWorkingDays_ne_compte_que_les_jours_de_l_annee_demandee()
	{
		var from = new DateOnly(2026, 12, 28);
		var to = new DateOnly(2027, 1, 8);

		Assert.Equal(4, LeaveBalance.CountWorkingDays(from, to, 2026));
		Assert.Equal(6, LeaveBalance.CountWorkingDays(from, to, 2027));
	}

	private static LeaveBalance.BalanceLeave Leave(int id, string start, string end, string createdAt) =>
		new(id, DateTime.Parse(start), DateTime.Parse(end), DateTime.Parse(createdAt));
}
