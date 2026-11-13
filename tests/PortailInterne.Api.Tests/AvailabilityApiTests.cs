using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Tests;

/// <summary>
/// GET /api/availability : disponibilités de plusieurs employés (phase 3, étape 10b) — horaire effectif en UTC,
/// absences approuvées et événements, sans jamais de titre ni de motif.
/// « Maintenant » = mercredi 14 octobre 2026, 16 h UTC (midi à Montréal, UTC−4).
/// Fenêtre demandée : jeudi 15 octobre à Montréal = 04:00Z → 04:00Z le lendemain.
/// </summary>
public class AvailabilityApiTests
{
	private static string Url(DateOnly from, DateOnly to, params int[] ids) =>
		$"/api/availability?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}" + string.Concat(ids.Select(id => $"&employeeIds={id}"));

	private static readonly DateOnly Thursday = new(2026, 10, 15);

	[Fact]
	public async Task Un_jour_travaille_donne_ses_heures_et_ses_creneaux_occupes_tries_en_UTC()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(team.Sam));
			db.Meetings.Add(Existing(team.Sam, "2026-10-15T18:00:00Z", "2026-10-15T19:00:00Z", kind: MeetingKind.PersonalBlock, isFlexible: true));
			db.Meetings.Add(Existing(team.Sam, "2026-10-15T15:00:00Z", "2026-10-15T16:00:00Z"));
			db.Absences.Add(Absence(team.Sam, new(10, 0), new(10, 30)));
			db.Absences.Add(Absence(team.Sam, new(13, 0), new(14, 0), LeaveStatus.EnAttente)); // en attente : ignorée
		});
		var sam = await api.LoginAsync("sam@test.local");

		var result = (await sam.GetFromJsonAsync<List<EmployeeAvailability>>(Url(Thursday, Thursday, team.Sam.Id)))!;

		var availability = Assert.Single(result);
		Assert.Equal("Sam", availability.FirstName);

		var day = Assert.Single(availability.Days);
		Assert.Equal(Thursday, day.Day);
		Assert.Equal(DayStatus.Working, day.Status);
		Assert.Equal(Utc(2026, 10, 15, 12, 0), day.StartAt); // 8 h à Montréal
		Assert.Equal(Utc(2026, 10, 15, 21, 0), day.EndAt);   // 17 h

		Assert.Collection(availability.Busy,
			b => AssertSlot(b, BusyKind.Absent, Utc(2026, 10, 15, 14, 0), Utc(2026, 10, 15, 14, 30)),
			b => AssertSlot(b, BusyKind.Busy, Utc(2026, 10, 15, 15, 0), Utc(2026, 10, 15, 16, 0)),
			b => AssertSlot(b, BusyKind.FlexibleBusy, Utc(2026, 10, 15, 18, 0), Utc(2026, 10, 15, 19, 0)));
	}

	[Fact]
	public async Task Une_absence_commencee_sans_retour_occupe_jusqu_a_la_fin_de_la_journee()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(team.Sam));
			db.Absences.Add(new Absence
			{
				EmployeeId = team.Sam.Id,
				Date = Thursday,
				StartedAt = new(15, 0),
				Status = LeaveStatus.Approuve,
				CreatedById = team.Sam.Id,
			});
		});
		var sam = await api.LoginAsync("sam@test.local");

		var result = (await sam.GetFromJsonAsync<List<EmployeeAvailability>>(Url(Thursday, Thursday, team.Sam.Id)))!;

		AssertSlot(Assert.Single(result[0].Busy), BusyKind.Absent, Utc(2026, 10, 15, 19, 0), Utc(2026, 10, 15, 21, 0));
	}

	[Fact]
	public async Task Chaque_employe_a_son_statut_et_seulement_ses_evenements_tries_par_nom()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(team.Sam));
			db.WorkSchedules.Add(WeekdaySchedule(team.Mark));
			db.LeaveRequests.Add(new LeaveRequest
			{
				EmployeeId = team.Mark.Id,
				StartDate = new DateTime(2026, 10, 15),
				EndDate = new DateTime(2026, 10, 15),
				Type = LeaveType.Conge,
				Status = LeaveStatus.Approuve,
			});
			db.Absences.Add(Absence(team.Mark, new(10, 0), new(11, 0))); // effacée par le congé
			db.Meetings.Add(Existing(team.Sam, "2026-10-15T15:00:00Z", "2026-10-15T16:00:00Z"));
		});
		var jane = await api.LoginAsync("jane@test.local"); // hors de l'équipe : peut quand même consulter

		var result = (await jane.GetFromJsonAsync<List<EmployeeAvailability>>(
			Url(Thursday, Thursday, team.Sam.Id, team.Jane.Id, team.Mark.Id, team.Sam.Id)))!; // Sam en double

		Assert.Equal(["Jane", "Mark", "Sam"], result.Select(r => r.FirstName));

		var (janeDay, markDay, samDay) = (result[0].Days.Single(), result[1].Days.Single(), result[2].Days.Single());
		Assert.Equal(DayStatus.Undefined, janeDay.Status); // aucun horaire
		Assert.Equal(DayStatus.NotWorking, markDay.Status); // en congé
		Assert.Null(markDay.StartAt);
		Assert.Null(markDay.EndAt);
		Assert.Equal(DayStatus.Working, samDay.Status);

		Assert.Empty(result[0].Busy);
		AssertSlot(Assert.Single(result[1].Busy), BusyKind.Absent, Utc(2026, 10, 15, 4, 0), Utc(2026, 10, 16, 4, 0)); // congé : toute la journée
		Assert.Single(result[2].Busy);
	}

	[Fact]
	public async Task Un_conge_en_attente_n_occupe_rien()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(team.Mark));
			db.LeaveRequests.Add(Leave(team.Mark, Thursday, LeaveStatus.EnAttente));
		});
		var sam = await api.LoginAsync("sam@test.local");

		var result = (await sam.GetFromJsonAsync<List<EmployeeAvailability>>(Url(Thursday, Thursday, team.Mark.Id)))!;

		Assert.Equal(DayStatus.Working, result[0].Days.Single().Status);
		Assert.Empty(result[0].Busy);
	}

	[Fact]
	public async Task Un_conge_occupe_la_journee_dans_le_fuseau_de_l_employe()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Jane.Id)!.SiteId = paris.Id;
			db.WorkSchedules.Add(WeekdaySchedule(team.Jane));
			db.LeaveRequests.Add(Leave(team.Jane, new DateOnly(2026, 10, 16), LeaveStatus.Approuve));
		});
		var sam = await api.LoginAsync("sam@test.local");

		// Jeudi à Montréal = jeudi 6 h → vendredi 6 h à Paris ; congé le vendredi à Paris (UTC+2)
		var result = (await sam.GetFromJsonAsync<List<EmployeeAvailability>>(Url(Thursday, Thursday, team.Jane.Id)))!;

		var leave = Assert.Single(result[0].Busy);
		AssertSlot(leave, BusyKind.Absent, Utc(2026, 10, 15, 22, 0), Utc(2026, 10, 16, 22, 0));
	}

	[Fact]
	public async Task Un_jour_hors_horaire_ou_ferie_n_est_pas_travaille()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(team.Sam));
			db.Holidays.Add(new Holiday { SiteId = team.Sam.SiteId, Date = new DateOnly(2026, 10, 16), Name = "Férié test" });
		});
		var sam = await api.LoginAsync("sam@test.local");

		var result = (await sam.GetFromJsonAsync<List<EmployeeAvailability>>(
			Url(Thursday, new DateOnly(2026, 10, 17), team.Sam.Id)))!;

		Assert.Equal(
			[DayStatus.Working, DayStatus.NotWorking, DayStatus.NotWorking], // jeudi, vendredi férié, samedi
			result[0].Days.Select(d => d.Status));
	}

	[Fact]
	public async Task Les_evenements_qui_touchent_la_fenetre_n_y_entrent_pas()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Meetings.Add(Existing(team.Sam, "2026-10-15T03:00:00Z", "2026-10-15T04:00:00Z")); // finit au début de la fenêtre
			db.Meetings.Add(Existing(team.Sam, "2026-10-16T04:00:00Z", "2026-10-16T05:00:00Z")); // commence à la fin
			db.Meetings.Add(Existing(team.Sam, "2026-10-16T03:00:00Z", "2026-10-16T04:00:00Z")); // 23 h → minuit : dedans
		});
		var sam = await api.LoginAsync("sam@test.local");

		var result = (await sam.GetFromJsonAsync<List<EmployeeAvailability>>(Url(Thursday, Thursday, team.Sam.Id)))!;

		AssertSlot(Assert.Single(result[0].Busy), BusyKind.Busy, Utc(2026, 10, 16, 3, 0), Utc(2026, 10, 16, 4, 0));
	}

	[Fact]
	public async Task La_fenetre_suit_le_fuseau_de_l_appelant_et_les_jours_celui_de_l_employe()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Jane.Id)!.SiteId = paris.Id;
			db.WorkSchedules.Add(WeekdaySchedule(team.Jane));
		});
		var sam = await api.LoginAsync("sam@test.local");

		// Jeudi à Montréal (04:00Z → 04:00Z) = jeudi 6 h → vendredi 6 h à Paris (UTC+2)
		var result = (await sam.GetFromJsonAsync<List<EmployeeAvailability>>(Url(Thursday, Thursday, team.Jane.Id)))!;

		var days = result[0].Days;
		Assert.Equal([Thursday, new DateOnly(2026, 10, 16)], days.Select(d => d.Day));
		Assert.Equal(Utc(2026, 10, 15, 6, 0), days[0].StartAt); // 8 h à Paris
		Assert.Equal(Utc(2026, 10, 15, 15, 0), days[0].EndAt);  // 17 h
	}

	[Fact]
	public async Task La_reponse_ne_contient_ni_titre_ni_motif()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(team.Sam));
			db.Meetings.Add(Existing(team.Sam, "2026-10-15T15:00:00Z", "2026-10-15T16:00:00Z", title: "Titre secret"));
			var absence = Absence(team.Sam, new(10, 0), new(10, 30));
			absence.Reason = "Motif secret";
			db.Absences.Add(absence);
		});
		var jane = await api.LoginAsync("jane@test.local");

		var json = await jane.GetStringAsync(Url(Thursday, Thursday, team.Sam.Id));

		Assert.DoesNotContain("secret", json);
		Assert.DoesNotContain("title", json, StringComparison.OrdinalIgnoreCase);
	}

	[Theory]
	[InlineData("aucun", "Choisis au moins un employé.")]
	[InlineData("trop", "Choisis 20 employés au maximum.")]
	[InlineData("inconnu", "Un des employés est introuvable ou inactif.")]
	[InlineData("inactif", "Un des employés est introuvable ou inactif.")]
	[InlineData("fin avant début", "La date de fin doit être après la date de début.")]
	[InlineData("plus de 62 jours", "Sélectionne une plage de 62 jours au maximum.")]
	public async Task Une_requete_invalide_est_refusee(string cas, string message)
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Employees.Find(team.Jane.Id)!.IsActive = false);
		var sam = await api.LoginAsync("sam@test.local");

		var url = cas switch
		{
			"aucun" => Url(Thursday, Thursday),
			"trop" => Url(Thursday, Thursday, [.. Enumerable.Range(1, 21)]),
			"inconnu" => Url(Thursday, Thursday, team.Sam.Id, 9999),
			"inactif" => Url(Thursday, Thursday, team.Sam.Id, team.Jane.Id),
			"fin avant début" => Url(Thursday, Thursday.AddDays(-1), team.Sam.Id),
			_ => Url(Thursday, Thursday.AddDays(62), team.Sam.Id),
		};
		var res = await sam.GetAsync(url);

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Equal(message, (await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("message").GetString());
	}

	[Fact]
	public async Task Une_plage_de_62_jours_est_permise()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var result = (await sam.GetFromJsonAsync<List<EmployeeAvailability>>(Url(Thursday, Thursday.AddDays(61), api.Team.Sam.Id)))!;

		Assert.Equal(62, result[0].Days.Count);
	}

	[Fact]
	public async Task La_connexion_est_requise()
	{
		using var api = new ApiFactory().Start();

		var res = await api.CreateClient().GetAsync(Url(Thursday, Thursday, api.Team.Sam.Id));

		Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
	}

	// ─── MeetingTime.ToUtc ──────────────────────────────────────────────────

	[Fact]
	public void ToUtc_avance_une_heure_qui_n_existe_pas_au_passage_a_l_heure_d_ete()
	{
		var montreal = TimeZoneInfo.FindSystemTimeZoneById("America/Toronto");

		// 8 mars 2026 : 2 h → 3 h à Montréal ; 2 h 30 n'existe pas et devient 3 h 30 (UTC−4)
		var utc = MeetingTime.ToUtc(new DateOnly(2026, 3, 8), new TimeOnly(2, 30), montreal);

		Assert.Equal(Utc(2026, 3, 8, 7, 30), utc);
		Assert.Equal(DateTimeKind.Utc, utc.Kind);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	private static DateTime Utc(int year, int month, int day, int hour, int minute) =>
		new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

	private static void AssertSlot(BusySlot slot, BusyKind kind, DateTime start, DateTime end)
	{
		Assert.Equal(kind, slot.Kind);
		Assert.Equal(start, slot.StartAt);
		Assert.Equal(end, slot.EndAt);
	}

	/// <summary>Un événement déjà en base, dont la personne est l'organisatrice et la seule participante.</summary>
	private static Meeting Existing(
		Employee owner,
		string start,
		string end,
		string title = "Existant",
		MeetingKind kind = MeetingKind.Meeting,
		bool isFlexible = false) => new()
	{
		Kind = kind,
		Title = title,
		StartAt = DateTime.Parse(start, null, System.Globalization.DateTimeStyles.AdjustToUniversal),
		EndAt = DateTime.Parse(end, null, System.Globalization.DateTimeStyles.AdjustToUniversal),
		OrganizerId = owner.Id,
		IsFlexible = isFlexible,
		Participants = [new MeetingParticipant { EmployeeId = owner.Id }],
	};

	/// <summary>Un horaire du lundi au vendredi, 8 h → 17 h, en vigueur depuis le 1er octobre 2026.</summary>
	private static WorkSchedule WeekdaySchedule(Employee employee) => new()
	{
		EmployeeId = employee.Id,
		EffectiveFrom = new DateOnly(2026, 10, 1),
		CreatedById = employee.Id,
		Days =
		[
			.. new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
				.Select(d => new WorkScheduleDay { DayOfWeek = d, StartTime = new(8, 0), EndTime = new(17, 0), BreakMinutes = 30 }),
		],
	};

	/// <summary>Un congé d'une journée.</summary>
	private static LeaveRequest Leave(Employee employee, DateOnly date, LeaveStatus status) => new()
	{
		EmployeeId = employee.Id,
		StartDate = date.ToDateTime(TimeOnly.MinValue),
		EndDate = date.ToDateTime(TimeOnly.MinValue),
		Type = LeaveType.Conge,
		Status = status,
	};

	/// <summary>Une absence prévue le jeudi 15 octobre 2026, approuvée par défaut.</summary>
	private static Absence Absence(Employee employee, TimeOnly start, TimeOnly end, LeaveStatus status = LeaveStatus.Approuve) => new()
	{
		EmployeeId = employee.Id,
		Date = Thursday,
		PlannedStart = start,
		PlannedEnd = end,
		Status = status,
		CreatedById = employee.Id,
	};
}
