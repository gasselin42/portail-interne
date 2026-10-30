using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// GET /api/team/week : qui voit la semaine de qui, avec ou sans détails (étape 19).
/// Semaine du lundi 19 au dimanche 25 octobre 2026. Tous les employés de test sont au site de Montréal,
/// avec le nom de famille « Test » : le tri se fait donc sur le prénom.
/// </summary>
public class TeamApiTests
{
	private const string Week = "/api/team/week?from=2026-10-19&to=2026-10-25";

	[Fact]
	public async Task Reglage_Team_l_employe_voit_son_manager_et_ses_collegues_sans_details()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Employees.Find(team.Jane.Id)!.ManagerId = team.Mark.Id);
		var sam = await api.LoginAsync("sam@test.local");

		var members = (await sam.GetFromJsonAsync<List<TeamMemberResponse>>(Week))!;

		Assert.Equal(
			[("Sam", true), ("Jane", false), ("Mark", false)],
			members.Select(m => (m.FirstName, m.Details)));
		Assert.All(members, m => Assert.Equal(7, m.Days.Count));
	}

	[Fact]
	public async Task Sans_details_l_ajustement_en_attente_les_absences_en_attente_et_le_motif_sont_caches()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(team.Mark, team.Alex));
			db.ScheduleAdjustments.Add(new ScheduleAdjustment
			{
				EmployeeId = team.Mark.Id,
				Date = new DateOnly(2026, 10, 19),
				StartTime = new TimeOnly(10, 0),
				EndTime = new TimeOnly(15, 0),
				Status = LeaveStatus.EnAttente,
				CreatedById = team.Mark.Id,
			});
			db.Absences.AddRange(
				Absence(team.Mark, day: 20, start: 9, LeaveStatus.Approuve, "Dentiste"),
				Absence(team.Mark, day: 21, start: 9, LeaveStatus.EnAttente, "Garage"));
		});
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");

		var seenBySam = (await sam.GetFromJsonAsync<List<TeamMemberResponse>>(Week))!.Single(m => m.FirstName == "Mark");
		var seenByMark = (await mark.GetFromJsonAsync<List<TeamMemberResponse>>(Week))!.Single(m => m.FirstName == "Mark");

		// Sam : la présence seulement
		Assert.False(seenBySam.Details);
		Assert.Null(seenBySam.Days[0].PendingAdjustment);
		Assert.Null(Assert.Single(seenBySam.Days[1].Absences).Reason);
		Assert.Empty(seenBySam.Days[2].PendingAbsences);

		// Mark sur lui-même : tout
		Assert.True(seenByMark.Details);
		Assert.Equal(new TimeOnly(10, 0), seenByMark.Days[0].PendingAdjustment!.StartTime);
		Assert.Equal("Dentiste", Assert.Single(seenByMark.Days[1].Absences).Reason);
		Assert.Equal("Garage", Assert.Single(seenByMark.Days[2].PendingAbsences).Reason);
	}

	[Fact]
	public async Task Le_manager_voit_ses_subordonnes_avec_details_meme_avec_le_reglage_Nobody()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		SetVisibility(api, TeamVisibility.Nobody);
		var mark = await api.LoginAsync("mark@test.local");

		var members = (await mark.GetFromJsonAsync<List<TeamMemberResponse>>(Week))!;
		var me = (await mark.GetFromJsonAsync<MeResponse>("/api/auth/me"))!;

		Assert.Equal([("Mark", true), ("Sam", true)], members.Select(m => (m.FirstName, m.Details)));
		Assert.True(me.CanSeeTeam);
	}

	[Fact]
	public async Task Reglage_Nobody_l_employe_n_a_pas_de_vue_equipe()
	{
		using var api = new ApiFactory().Start();
		SetVisibility(api, TeamVisibility.Nobody);
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.GetAsync(Week);
		var me = (await sam.GetFromJsonAsync<MeResponse>("/api/auth/me"))!;

		Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
		Assert.False(me.CanSeeTeam);
	}

	[Fact]
	public async Task Reglage_Site_l_employe_voit_tout_le_site_lui_en_premier()
	{
		using var api = new ApiFactory().Start();
		SetVisibility(api, TeamVisibility.Site);
		var jane = await api.LoginAsync("jane@test.local");

		var members = (await jane.GetFromJsonAsync<List<TeamMemberResponse>>(Week))!;

		Assert.Equal(
			[("Jane", true), ("Alex", false), ("Gaby", false), ("Mark", false), ("Sam", false)],
			members.Select(m => (m.FirstName, m.Details)));
	}

	[Fact]
	public async Task Reglage_Team_sans_manager_ni_subordonne_pas_de_vue_equipe()
	{
		using var api = new ApiFactory().Start();
		var jane = await api.LoginAsync("jane@test.local");

		var res = await jane.GetAsync(Week);
		var me = (await jane.GetFromJsonAsync<MeResponse>("/api/auth/me"))!;

		Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
		Assert.False(me.CanSeeTeam);
	}

	[Fact]
	public async Task Reglage_Team_manager_desactive_sans_collegue_pas_de_vue_equipe()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Employees.Find(team.Mark.Id)!.IsActive = false);
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.GetAsync(Week);
		var me = (await sam.GetFromJsonAsync<MeResponse>("/api/auth/me"))!;

		Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
		Assert.False(me.CanSeeTeam);
	}

	[Fact]
	public async Task Reglage_Team_manager_desactive_les_collegues_actifs_restent_visibles()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Employees.Find(team.Mark.Id)!.IsActive = false;
			db.Employees.Find(team.Jane.Id)!.ManagerId = team.Mark.Id;
		});
		var sam = await api.LoginAsync("sam@test.local");

		var members = (await sam.GetFromJsonAsync<List<TeamMemberResponse>>(Week))!;

		Assert.Equal([("Sam", true), ("Jane", false)], members.Select(m => (m.FirstName, m.Details)));
	}

	[Fact]
	public async Task Un_subordonne_desactive_n_est_pas_liste()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		SetVisibility(api, TeamVisibility.Site);
		api.WithDb(db => db.Employees.Find(team.Sam.Id)!.IsActive = false);
		var mark = await api.LoginAsync("mark@test.local");

		var members = (await mark.GetFromJsonAsync<List<TeamMemberResponse>>(Week))!;

		Assert.DoesNotContain(members, m => m.FirstName == "Sam");
	}

	[Fact]
	public async Task L_admin_voit_tout_son_site_avec_details()
	{
		using var api = new ApiFactory().Start();
		SetVisibility(api, TeamVisibility.Nobody);
		var alex = await api.LoginAsync("alex@test.local");

		var members = (await alex.GetFromJsonAsync<List<TeamMemberResponse>>(Week))!;

		Assert.Equal(["Alex", "Gaby", "Jane", "Mark", "Sam"], members.Select(m => m.FirstName));
		Assert.All(members, m => Assert.True(m.Details));
	}

	[Fact]
	public async Task L_admin_global_choisit_le_site_les_autres_non()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var parisId = 0;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			parisId = paris.Id;
			db.Employees.Find(team.Jane.Id)!.SiteId = paris.Id;
		});
		var gaby = await api.LoginAsync("gaby@test.local");
		var alex = await api.LoginAsync("alex@test.local");
		var sam = await api.LoginAsync("sam@test.local");

		var parisMembers = (await gaby.GetFromJsonAsync<List<TeamMemberResponse>>($"{Week}&siteId={parisId}"))!;
		var ownSite = (await gaby.GetFromJsonAsync<List<TeamMemberResponse>>(Week))!;
		var unknown = await gaby.GetAsync($"{Week}&siteId=999");
		var alexOnParis = await alex.GetAsync($"{Week}&siteId={parisId}");
		var samOnOwnSite = await sam.GetAsync($"{Week}&siteId={team.Sam.SiteId}");

		Assert.Equal([("Jane", true)], parisMembers.Select(m => (m.FirstName, m.Details)));
		Assert.Equal(["Gaby", "Alex", "Mark", "Sam"], ownSite.Select(m => m.FirstName));
		Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
		Assert.Equal(HttpStatusCode.Forbidden, alexOnParis.StatusCode);
		Assert.Equal(HttpStatusCode.Forbidden, samOnOwnSite.StatusCode);
	}

	[Theory]
	[InlineData("2026-10-25", "2026-10-19")] // fin avant le début
	[InlineData("2026-10-19", "2026-10-26")] // 8 jours
	public async Task Periode_invalide_refusee(string from, string to)
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.GetAsync($"/api/team/week?from={from}&to={to}");

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Connexion_requise()
	{
		using var api = new ApiFactory().Start();

		var res = await api.CreateClient().GetAsync(Week);

		Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	private static void SetVisibility(ApiFactory api, TeamVisibility visibility) =>
		api.WithDb(db => db.Sites.Find(api.Team.Sam.SiteId)!.TeamVisibility = visibility);

	/// <summary>Lundi au vendredi, 8 h à 17 h, depuis le 1er octobre 2026.</summary>
	private static WorkSchedule WeekdaySchedule(Employee employee, Employee createdBy) => new()
	{
		EmployeeId = employee.Id,
		EffectiveFrom = new DateOnly(2026, 10, 1),
		CreatedById = createdBy.Id,
		Days =
		[
			.. new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
				.Select(d => new WorkScheduleDay { DayOfWeek = d, StartTime = new(8, 0), EndTime = new(17, 0), BreakMinutes = 30 }),
		],
	};

	/// <summary>Une absence prévue d'une heure en octobre 2026, demandée par l'employé lui-même.</summary>
	private static Absence Absence(Employee employee, int day, int start, LeaveStatus status, string reason) => new()
	{
		EmployeeId = employee.Id,
		Date = new DateOnly(2026, 10, day),
		PlannedStart = new TimeOnly(start, 0),
		PlannedEnd = new TimeOnly(start + 1, 0),
		Status = status,
		Reason = reason,
		CreatedById = employee.Id,
	};
}
