using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Tests d'intégration des absences prévues (R24).
/// « Maintenant » = mercredi 14 octobre 2026, midi à Montréal.
/// Sam travaille du lundi au vendredi, 8 h à 17 h ; Mark est son manager.
/// </summary>
public class AbsencesApiTests
{
	private const string Tuesday = "2026-10-20";

	// ─── Création ───────────────────────────────────────────────────────────

	[Fact]
	public async Task La_demande_d_un_employe_est_en_attente()
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync(Url(api.Team.Sam), Planned(Tuesday, "09:00:00", "11:00:00"));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		var absence = await res.Content.ReadFromJsonAsync<AbsenceResponse>();
		Assert.Equal(LeaveStatus.EnAttente, absence!.Status);
		Assert.False(absence.InProgress);
		Assert.True(absence.CanCancel);
	}

	[Fact]
	public async Task Le_manager_cree_une_absence_approuvee()
	{
		using var api = StartWithSchedule();
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync(Url(api.Team.Sam), Planned(Tuesday, "09:00:00", "11:00:00"));

		Assert.Equal(LeaveStatus.Approuve, (await res.Content.ReadFromJsonAsync<AbsenceResponse>())!.Status);
	}

	[Theory]
	[InlineData(Tuesday, "07:00:00", "09:00:00")]     // commence avant la journée
	[InlineData(Tuesday, "16:00:00", "18:00:00")]     // finit après la journée
	[InlineData("2026-10-24", "09:00:00", "11:00:00")] // samedi : jour non travaillé
	public async Task Une_absence_hors_des_heures_travaillees_est_refusee(string date, string start, string end)
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync(Url(api.Team.Sam), Planned(date, start, end));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Sans_horaire_on_ne_peut_pas_s_absenter()
	{
		using var api = new ApiFactory().Start();
		var jane = await api.LoginAsync("jane@test.local");

		var res = await jane.PostAsJsonAsync(Url(api.Team.Jane), Planned(Tuesday, "09:00:00", "11:00:00"));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Deux_absences_ne_se_chevauchent_pas_mais_peuvent_se_toucher()
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");
		await sam.PostAsJsonAsync(Url(api.Team.Sam), Planned(Tuesday, "09:00:00", "11:00:00"));

		var overlapping = await sam.PostAsJsonAsync(Url(api.Team.Sam), Planned(Tuesday, "10:00:00", "12:00:00"));
		var touching = await sam.PostAsJsonAsync(Url(api.Team.Sam), Planned(Tuesday, "11:00:00", "12:00:00"));

		Assert.Equal(HttpStatusCode.Conflict, overlapping.StatusCode);
		Assert.Equal(HttpStatusCode.Created, touching.StatusCode);
	}

	[Fact]
	public async Task Une_absence_ne_peut_pas_etre_dans_le_passe()
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync(Url(api.Team.Sam), Planned("2026-10-13", "09:00:00", "11:00:00"));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	// ─── Lecture ────────────────────────────────────────────────────────────

	[Fact]
	public async Task La_liste_indique_une_absence_en_cours()
	{
		using var api = StartWithSchedule();
		var mark = await api.LoginAsync("mark@test.local");
		var sam = await api.LoginAsync("sam@test.local");
		// Aujourd'hui de 11 h à 13 h : il est midi
		await mark.PostAsJsonAsync(Url(api.Team.Sam), Planned("2026-10-14", "11:00:00", "13:00:00"));

		var absence = Assert.Single((await sam.GetFromJsonAsync<List<AbsenceResponse>>(Url(api.Team.Sam)))!);

		Assert.True(absence.InProgress);
		Assert.Null(absence.End);
		Assert.False(absence.CanCancel); // déjà commencée
	}

	[Fact]
	public async Task Un_collegue_ne_voit_pas_les_absences_d_un_autre()
	{
		using var api = StartWithSchedule();
		var jane = await api.LoginAsync("jane@test.local");

		var res = await jane.GetAsync(Url(api.Team.Sam));

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	// ─── Annulation ─────────────────────────────────────────────────────────

	[Fact]
	public async Task L_employe_annule_sa_demande_mais_seul_le_manager_annule_une_absence_approuvee()
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		var pending = await CreateAsync(sam, api.Team.Sam, Planned(Tuesday, "09:00:00", "10:00:00"));
		var approved = await CreateAsync(mark, api.Team.Sam, Planned(Tuesday, "14:00:00", "15:00:00"));

		var cancelPending = await sam.PostAsync($"{Url(api.Team.Sam)}/{pending.Id}/cancel", null);
		var samCancelApproved = await sam.PostAsync($"{Url(api.Team.Sam)}/{approved.Id}/cancel", null);
		var markCancelApproved = await mark.PostAsync($"{Url(api.Team.Sam)}/{approved.Id}/cancel", null);

		Assert.Equal(HttpStatusCode.NoContent, cancelPending.StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, samCancelApproved.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, markCancelApproved.StatusCode);
	}

	[Fact]
	public async Task Une_absence_deja_commencee_ne_s_annule_plus()
	{
		using var api = StartWithSchedule();
		var mark = await api.LoginAsync("mark@test.local");
		// Aujourd'hui de 10 h à 11 h : elle a commencé à 10 h, il est midi
		var started = await CreateAsync(mark, api.Team.Sam, Planned("2026-10-14", "10:00:00", "11:00:00"));

		var res = await mark.PostAsync($"{Url(api.Team.Sam)}/{started.Id}/cancel", null);

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Contains("déjà commencé", await res.Content.ReadAsStringAsync());
	}

	[Fact]
	public async Task Une_absence_annulee_libere_ses_heures()
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");
		var first = await CreateAsync(sam, api.Team.Sam, Planned(Tuesday, "09:00:00", "11:00:00"));
		await sam.PostAsync($"{Url(api.Team.Sam)}/{first.Id}/cancel", null);

		var res = await sam.PostAsJsonAsync(Url(api.Team.Sam), Planned(Tuesday, "10:00:00", "12:00:00"));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	[Fact]
	public async Task On_n_annule_pas_l_absence_d_un_autre_employe_par_l_url()
	{
		using var api = StartWithSchedule();
		var alex = await api.LoginAsync("alex@test.local");
		var samAbsence = await CreateAsync(alex, api.Team.Sam, Planned(Tuesday, "09:00:00", "11:00:00"));

		var res = await alex.PostAsync($"{Url(api.Team.Jane)}/{samAbsence.Id}/cancel", null);

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	// ─── Approbation ────────────────────────────────────────────────────────

	[Theory]
	[InlineData(LeaveStatus.Approuve)]
	[InlineData(LeaveStatus.Refuse)]
	public async Task Le_manager_traite_une_demande_en_attente(LeaveStatus decision)
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		var pending = await CreateAsync(sam, api.Team.Sam, Planned(Tuesday, "09:00:00", "11:00:00"));

		var res = await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = decision });

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		var absence = Assert.Single((await sam.GetFromJsonAsync<List<AbsenceResponse>>(Url(api.Team.Sam)))!);
		Assert.Equal(decision, absence.Status);
	}

	[Fact]
	public async Task On_ne_traite_pas_sa_propre_demande_ni_celle_d_un_collegue()
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");
		var jane = await api.LoginAsync("jane@test.local");
		var pending = await CreateAsync(sam, api.Team.Sam, Planned(Tuesday, "09:00:00", "11:00:00"));

		var bySam = await sam.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Approuve });
		var byJane = await jane.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.BadRequest, bySam.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, byJane.StatusCode);
	}

	[Fact]
	public async Task Une_demande_deja_traitee_ne_se_traite_plus()
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		var pending = await CreateAsync(sam, api.Team.Sam, Planned(Tuesday, "09:00:00", "11:00:00"));
		await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Refuse });

		var res = await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Une_demande_en_cours_s_approuve_et_devient_effective()
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		// Aujourd'hui de 11 h à 13 h : il est midi
		var pending = await CreateAsync(sam, api.Team.Sam, Planned("2026-10-14", "11:00:00", "13:00:00"));

		var res = await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		var absence = Assert.Single((await sam.GetFromJsonAsync<List<AbsenceResponse>>(Url(api.Team.Sam)))!);
		Assert.True(absence.InProgress);
	}

	[Fact]
	public async Task Une_demande_terminee_se_refuse_mais_ne_s_approuve_plus()
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		// Aujourd'hui de 9 h à 10 h : il est midi
		var pending = await CreateAsync(sam, api.Team.Sam, Planned("2026-10-14", "09:00:00", "10:00:00"));

		var approve = await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Approuve });
		var refuse = await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Refuse });

		Assert.Equal(HttpStatusCode.BadRequest, approve.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, refuse.StatusCode);
	}

	[Fact]
	public async Task Un_jour_devenu_conge_ne_s_approuve_plus_mais_se_refuse()
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		var pending = await CreateAsync(sam, api.Team.Sam, Planned(Tuesday, "09:00:00", "11:00:00"));
		// Entre-temps, Sam obtient un congé ce mardi-là
		api.WithDb(db => db.LeaveRequests.Add(new LeaveRequest
		{
			EmployeeId = api.Team.Sam.Id,
			StartDate = new DateTime(2026, 10, 20),
			EndDate = new DateTime(2026, 10, 20),
			Type = LeaveType.Conge,
			Status = LeaveStatus.Approuve,
		}));

		var approve = await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Approuve });
		var refuse = await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Refuse });

		Assert.Equal(HttpStatusCode.BadRequest, approve.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, refuse.StatusCode);
	}

	[Fact]
	public async Task Des_heures_changees_par_un_ajustement_sont_reverifiees()
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		var pending = await CreateAsync(sam, api.Team.Sam, Planned(Tuesday, "09:00:00", "11:00:00"));
		// Entre-temps, le mardi passe à 10 h – 17 h
		await mark.PostAsJsonAsync($"/api/employees/{api.Team.Sam.Id}/adjustments",
			new { date = Tuesday, startTime = "10:00:00", endTime = "17:00:00", breakMinutes = 30 });

		var res = await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Une_urgence_en_cours_bloque_l_approbation_d_une_demande_qui_la_chevauche()
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		// Demande de 13 h à 14 h ; à midi, Sam clique « Je pars » (urgence, toujours en cours)
		var pending = await CreateAsync(sam, api.Team.Sam, Planned("2026-10-14", "13:00:00", "14:00:00"));
		(await sam.PostAsJsonAsync($"{Url(api.Team.Sam)}/depart", new { })).EnsureSuccessStatusCode();

		var res = await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
	}

	[Fact]
	public async Task On_ne_traite_pas_l_absence_d_un_autre_employe_par_l_url()
	{
		using var api = StartWithSchedule();
		var sam = await api.LoginAsync("sam@test.local");
		var alex = await api.LoginAsync("alex@test.local");
		var samRequest = await CreateAsync(sam, api.Team.Sam, Planned(Tuesday, "09:00:00", "11:00:00"));

		var res = await alex.PostAsJsonAsync(Review(api.Team.Jane, samRequest.Id), new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	/// <summary>Démarre l'API et donne à Sam un horaire du lundi au vendredi, 8 h à 17 h.</summary>
	private static ApiFactory StartWithSchedule()
	{
		var api = new ApiFactory().Start();
		api.WithDb(db => db.WorkSchedules.Add(new WorkSchedule
		{
			EmployeeId = api.Team.Sam.Id,
			EffectiveFrom = new DateOnly(2026, 10, 1),
			CreatedById = api.Team.Mark.Id,
			Days =
			[
				.. new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
					.Select(d => new WorkScheduleDay { DayOfWeek = d, StartTime = new(8, 0), EndTime = new(17, 0), BreakMinutes = 30 }),
			],
		}));
		return api;
	}

	private static string Url(Employee employee) => $"/api/employees/{employee.Id}/absences";

	private static string Review(Employee employee, int id) => $"{Url(employee)}/{id}/review";

	private static object Planned(string date, string start, string end) =>
		new { date, plannedStart = start, plannedEnd = end, reason = "Rendez-vous" };

	private static async Task<AbsenceResponse> CreateAsync(HttpClient client, Employee employee, object request)
	{
		var res = await client.PostAsJsonAsync(Url(employee), request);
		res.EnsureSuccessStatusCode();
		return (await res.Content.ReadFromJsonAsync<AbsenceResponse>())!;
	}
}
