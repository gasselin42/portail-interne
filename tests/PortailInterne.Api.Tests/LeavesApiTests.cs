using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Tests d'intégration des congés : la vraie API, de la requête HTTP à la base.
/// « Aujourd'hui » = mercredi 14 octobre 2026 (voir ApiFactory).
/// </summary>
public class LeavesApiTests
{
	// ─── Création ───────────────────────────────────────────────────────────

	[Fact]
	public async Task Une_demande_de_conge_est_creee_en_attente()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/leaves", Conge("2026-11-16", "2026-11-20"));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		var leave = await res.Content.ReadFromJsonAsync<LeaveRequestResponse>();
		Assert.Equal(LeaveStatus.EnAttente, leave!.Status);
		Assert.Equal(5, leave.WorkingDays);
	}

	[Fact]
	public async Task Un_conge_ne_peut_pas_commencer_dans_le_passe()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/leaves", Conge("2026-10-13", "2026-10-15"));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Theory]
	[InlineData(14, HttpStatusCode.Created)]
	[InlineData(15, HttpStatusCode.BadRequest)]
	public async Task Un_conge_maladie_est_retroactif_de_14_jours_au_maximum(int daysAgo, HttpStatusCode expected)
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var start = ApiFactory.Today.AddDays(-daysAgo).ToString("yyyy-MM-dd");

		var res = await sam.PostAsJsonAsync("/api/leaves", Maladie(start, start));

		Assert.Equal(expected, res.StatusCode);
	}

	[Fact]
	public async Task Une_demande_qui_en_chevauche_une_autre_est_refusee()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		await sam.PostAsJsonAsync("/api/leaves", Conge("2026-11-16", "2026-11-18"));

		var res = await sam.PostAsJsonAsync("/api/leaves", Conge("2026-11-18", "2026-11-20"));

		Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
	}

	[Fact]
	public async Task Une_demande_annulee_ne_bloque_plus_ses_dates()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var first = await CreateAsync(sam, Conge("2026-11-16", "2026-11-18"));
		await sam.PostAsync($"/api/leaves/{first.Id}/cancel", null);

		var res = await sam.PostAsJsonAsync("/api/leaves", Conge("2026-11-18", "2026-11-20"));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	// ─── Isolation des données ──────────────────────────────────────────────

	[Fact]
	public async Task Un_employe_ne_voit_que_ses_propres_demandes()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var jane = await api.LoginAsync("jane@test.local");
		await CreateAsync(sam, Conge("2026-11-16", "2026-11-20"));

		var janeLeaves = await jane.GetFromJsonAsync<List<LeaveRequestResponse>>("/api/leaves");

		Assert.Empty(janeLeaves!);
	}

	[Fact]
	public async Task Un_employe_ne_peut_pas_annuler_la_demande_d_un_autre()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var jane = await api.LoginAsync("jane@test.local");
		var leave = await CreateAsync(sam, Conge("2026-11-16", "2026-11-20"));

		var res = await jane.PostAsync($"/api/leaves/{leave.Id}/cancel", null);

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	[Fact]
	public async Task Un_employe_sans_equipe_n_a_rien_a_approuver()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var jane = await api.LoginAsync("jane@test.local");
		await CreateAsync(sam, Conge("2026-11-16", "2026-11-20"));

		var pending = await jane.GetFromJsonAsync<List<LeaveRequestResponse>>("/api/leaves/pending");

		Assert.Empty(pending!);
	}

	[Fact]
	public async Task Le_calendrier_ne_montre_que_ses_propres_absences()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var jane = await api.LoginAsync("jane@test.local");
		await CreateAsync(sam, Conge("2026-11-16", "2026-11-20"));

		var calendar = await jane.GetFromJsonAsync<CalendarResponse>("/api/calendar?from=2026-11-16&to=2026-11-22");

		Assert.Empty(calendar!.Leaves);
	}

	// ─── Approbation ────────────────────────────────────────────────────────

	[Fact]
	public async Task Le_manager_voit_et_approuve_les_demandes_de_son_equipe()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		var leave = await CreateAsync(sam, Conge("2026-11-16", "2026-11-20"));

		var pending = await mark.GetFromJsonAsync<List<LeaveRequestResponse>>("/api/leaves/pending");
		Assert.Contains(pending!, l => l.Id == leave.Id);

		var res = await mark.PostAsJsonAsync($"/api/leaves/{leave.Id}/review", new { status = LeaveStatus.Approuve });
		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);

		var samLeaves = await sam.GetFromJsonAsync<List<LeaveRequestResponse>>("/api/leaves");
		Assert.Equal(LeaveStatus.Approuve, samLeaves!.Single(l => l.Id == leave.Id).Status);
	}

	[Fact]
	public async Task Un_manager_ne_traite_pas_une_demande_hors_de_son_equipe()
	{
		using var api = new ApiFactory().Start();
		var jane = await api.LoginAsync("jane@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		var leave = await CreateAsync(jane, Conge("2026-11-16", "2026-11-20"));

		var res = await mark.PostAsJsonAsync($"/api/leaves/{leave.Id}/review", new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	[Fact]
	public async Task L_admin_peut_approuver_toute_demande()
	{
		using var api = new ApiFactory().Start();
		var jane = await api.LoginAsync("jane@test.local");
		var alex = await api.LoginAsync("alex@test.local");
		var leave = await CreateAsync(jane, Conge("2026-11-16", "2026-11-20"));

		var res = await alex.PostAsJsonAsync($"/api/leaves/{leave.Id}/review", new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
	}

	[Fact]
	public async Task Personne_ne_traite_sa_propre_demande()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");
		var leave = await CreateAsync(alex, Conge("2026-11-16", "2026-11-20"));

		var res = await alex.PostAsJsonAsync($"/api/leaves/{leave.Id}/review", new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Une_approbation_qui_chevaucherait_un_conge_approuve_est_refusee()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var pendingId = 0;
		// Cas que l'API refuse de créer (Q16) : préparé directement en base, comme des données d'avant Q16
		api.WithDb(db =>
		{
			db.LeaveRequests.Add(Stored(team.Sam.Id, "2026-11-16", "2026-11-18", LeaveStatus.Approuve));
			var pending = Stored(team.Sam.Id, "2026-11-18", "2026-11-20", LeaveStatus.EnAttente);
			db.LeaveRequests.Add(pending);
			db.SaveChanges();
			pendingId = pending.Id;
		});
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync($"/api/leaves/{pendingId}/review", new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
	}

	// ─── Congé maladie déclaré pour un autre (R20) ──────────────────────────

	[Fact]
	public async Task Le_manager_declare_un_conge_maladie_approuve_pour_son_equipe()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");
		var sam = await api.LoginAsync("sam@test.local");

		var res = await mark.PostAsJsonAsync("/api/leaves/declare",
			new { employeeId = api.Team.Sam.Id, startDate = "2026-10-13", endDate = "2026-10-14" });

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		var samLeave = Assert.Single((await sam.GetFromJsonAsync<List<LeaveRequestResponse>>("/api/leaves"))!);
		Assert.Equal(LeaveType.Maladie, samLeave.Type);
		Assert.Equal(LeaveStatus.Approuve, samLeave.Status);
		Assert.Equal("Mark", samLeave.CreatedByFirstName);
	}

	[Fact]
	public async Task Un_manager_ne_declare_pas_hors_de_son_equipe_ni_pour_lui_meme()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");

		var forJane = await mark.PostAsJsonAsync("/api/leaves/declare",
			new { employeeId = api.Team.Jane.Id, startDate = "2026-10-14", endDate = "2026-10-14" });
		var forHimself = await mark.PostAsJsonAsync("/api/leaves/declare",
			new { employeeId = api.Team.Mark.Id, startDate = "2026-10-14", endDate = "2026-10-14" });

		Assert.Equal(HttpStatusCode.NotFound, forJane.StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, forHimself.StatusCode);
	}

	// ─── Solde (R19) ────────────────────────────────────────────────────────

	[Fact]
	public async Task Le_solde_compte_les_conges_en_attente_et_ignore_la_maladie()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		await CreateAsync(sam, Conge("2026-11-16", "2026-11-20"));
		await CreateAsync(sam, Maladie("2026-10-14", "2026-10-14"));

		var balance = await sam.GetFromJsonAsync<LeaveBalanceResponse>("/api/leaves/balance?year=2026");

		Assert.Equal(20, balance!.AnnualDays);
		Assert.Equal(0, balance.ApprovedDays);
		Assert.Equal(5, balance.PendingDays);
		Assert.Equal(15, balance.RemainingDays);
		Assert.Equal(0, balance.UnpaidDays);
	}

	[Fact]
	public async Task L_apercu_annonce_les_jours_sans_solde_sans_rien_creer()
	{
		using var api = new ApiFactory().Start();
		api.WithDb(db => db.Employees.Single(e => e.Id == api.Team.Sam.Id).AnnualLeaveDays = 3);
		var sam = await api.LoginAsync("sam@test.local");

		var preview = await sam.GetFromJsonAsync<LeavePreviewResponse>(
			"/api/leaves/balance/preview?startDate=2026-11-16&endDate=2026-11-20");

		Assert.Equal(5, preview!.WorkingDays);
		Assert.Equal([new DateOnly(2026, 11, 19), new DateOnly(2026, 11, 20)], preview.UnpaidDates);
		Assert.Empty((await sam.GetFromJsonAsync<List<LeaveRequestResponse>>("/api/leaves"))!);
	}

	// ─── Jours travaillés (R14) ─────────────────────────────────────────────
	// Semaine du lundi 16 au vendredi 20 novembre 2026

	[Fact]
	public async Task Seuls_les_jours_de_l_horaire_comptent()
	{
		using var api = new ApiFactory().Start();
		AddMondayToThursday(api);
		var sam = await api.LoginAsync("sam@test.local");

		var leave = await CreateAsync(sam, Conge("2026-11-16", "2026-11-20"));
		var balance = await sam.GetFromJsonAsync<LeaveBalanceResponse>("/api/leaves/balance?year=2026");

		Assert.Equal(4, leave.WorkingDays); // le vendredi n'est pas travaillé
		Assert.Equal(new DateOnly(2026, 11, 19), leave.WorkedDates!.Last()); // le LeaveBand s'en sert
		Assert.Equal(4, balance!.PendingDays);
	}

	[Fact]
	public async Task Un_conge_approuve_compte_toujours_ses_propres_jours()
	{
		using var api = new ApiFactory().Start();
		AddMondayToThursday(api);
		api.WithDb(db => db.LeaveRequests.Add(Stored(api.Team.Sam.Id, "2026-11-16", "2026-11-20", LeaveStatus.Approuve)));
		var sam = await api.LoginAsync("sam@test.local");

		var balance = await sam.GetFromJsonAsync<LeaveBalanceResponse>("/api/leaves/balance?year=2026");

		Assert.Equal(4, balance!.ApprovedDays);
	}

	[Fact]
	public async Task Un_ferie_ne_compte_pas_meme_sans_horaire()
	{
		using var api = new ApiFactory().Start();
		api.WithDb(db => db.Holidays.Add(new Holiday { SiteId = api.Team.Sam.SiteId, Date = new DateOnly(2026, 11, 18), Name = "Férié" }));
		var sam = await api.LoginAsync("sam@test.local");

		var leave = await CreateAsync(sam, Conge("2026-11-16", "2026-11-20"));

		Assert.Equal(4, leave.WorkingDays); // sans horaire : lun.–ven., moins le férié
	}

	[Fact]
	public async Task Une_journee_retiree_par_un_ajustement_ne_compte_pas()
	{
		using var api = new ApiFactory().Start();
		AddMondayToThursday(api);
		api.WithDb(db => db.ScheduleAdjustments.Add(new ScheduleAdjustment
		{
			EmployeeId = api.Team.Sam.Id,
			Date = new DateOnly(2026, 11, 17),
			Status = LeaveStatus.Approuve,
			CreatedById = api.Team.Mark.Id,
		}));
		var sam = await api.LoginAsync("sam@test.local");

		var leave = await CreateAsync(sam, Conge("2026-11-16", "2026-11-20"));

		Assert.Equal(3, leave.WorkingDays);
	}

	[Fact]
	public async Task L_apercu_suit_l_horaire()
	{
		using var api = new ApiFactory().Start();
		AddMondayToThursday(api);
		api.WithDb(db => db.Employees.Single(e => e.Id == api.Team.Sam.Id).AnnualLeaveDays = 3);
		var sam = await api.LoginAsync("sam@test.local");

		var preview = await sam.GetFromJsonAsync<LeavePreviewResponse>(
			"/api/leaves/balance/preview?startDate=2026-11-16&endDate=2026-11-20");

		Assert.Equal(4, preview!.WorkingDays);
		Assert.Equal([new DateOnly(2026, 11, 19)], preview.UnpaidDates);
	}

	[Theory]
	[InlineData("2026-11-20", "2026-11-22", HttpStatusCode.BadRequest)] // vendredi au dimanche : rien
	[InlineData("2026-11-19", "2026-11-22", HttpStatusCode.Created)]    // le jeudi suffit
	public async Task Une_demande_sans_aucun_jour_travaille_est_refusee(string start, string end, HttpStatusCode expected)
	{
		using var api = new ApiFactory().Start();
		AddMondayToThursday(api);
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/leaves", Conge(start, end));

		Assert.Equal(expected, res.StatusCode);
	}

	[Fact]
	public async Task Une_demande_sur_un_ferie_seul_est_refusee()
	{
		using var api = new ApiFactory().Start(); // Noël est un férié semé du site
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/leaves", Conge("2026-12-25", "2026-12-25"));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Une_maladie_declaree_sans_aucun_jour_travaille_est_refusee()
	{
		using var api = new ApiFactory().Start();
		AddMondayToThursday(api);
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync("/api/leaves/declare",
			new { employeeId = api.Team.Sam.Id, startDate = "2026-10-16", endDate = "2026-10-18" }); // vendredi au dimanche

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Contains("Sam", await res.Content.ReadAsStringAsync());
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	/// <summary>Donne à Sam un horaire du lundi au jeudi, 8 h à 17 h.</summary>
	private static void AddMondayToThursday(ApiFactory api) => api.WithDb(db => db.WorkSchedules.Add(new WorkSchedule
	{
		EmployeeId = api.Team.Sam.Id,
		EffectiveFrom = new DateOnly(2026, 10, 1),
		CreatedById = api.Team.Mark.Id,
		Days =
		[
			.. new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }
				.Select(d => new WorkScheduleDay { DayOfWeek = d, StartTime = new(8, 0), EndTime = new(17, 0), BreakMinutes = 30 }),
		],
	}));

	private static object Conge(string start, string end) =>
		new { startDate = start, endDate = end, type = LeaveType.Conge };

	private static object Maladie(string start, string end) =>
		new { startDate = start, endDate = end, type = LeaveType.Maladie };

	private static async Task<LeaveRequestResponse> CreateAsync(HttpClient client, object request)
	{
		var res = await client.PostAsJsonAsync("/api/leaves", request);
		res.EnsureSuccessStatusCode();
		return (await res.Content.ReadFromJsonAsync<LeaveRequestResponse>())!;
	}

	private static LeaveRequest Stored(int employeeId, string start, string end, LeaveStatus status) => new()
	{
		EmployeeId = employeeId,
		StartDate = DateTime.Parse(start),
		EndDate = DateTime.Parse(end),
		Type = LeaveType.Conge,
		Status = status,
		CreatedAt = DateTime.UtcNow,
	};
}
