using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Congé approuvé qui chevauche un meeting (phase 3, étape 10d) : le meeting est conservé et les bonnes personnes
/// sont prévenues ; modifier un meeting sans changer l'heure ne revérifie que les participants ajoutés.
/// « Maintenant » = mercredi 14 octobre 2026, midi à Montréal (16 h UTC).
/// Sam et Mark travaillent du lundi au vendredi, 8 h à 17 h ; Mark est le manager de Sam, Alex celui de Mark.
/// </summary>
public class LeaveNotificationsTests
{
	// ─── Notifications ──────────────────────────────────────────────────────

	[Fact]
	public async Task A_l_approbation_l_organisateur_et_l_employe_sont_prevenus()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Meetings.Add(Meeting(team.Jane, [team.Sam], "2026-10-15T14:00:00Z"));                                      // jeudi 10 h
			db.Meetings.Add(Meeting(team.Jane, [team.Sam], "2026-10-16T14:00:00Z", title: "Vendredi"));                   // hors du congé
			db.Meetings.Add(Meeting(team.Sam, [], "2026-10-15T16:00:00Z", kind: MeetingKind.PersonalBlock, title: "Bloc")); // ignoré
		});
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");

		var id = await RequestLeaveAsync(sam, "2026-10-15", "2026-10-15");
		api.WithDb(db => Assert.Empty(db.Notifications)); // en attente : rien

		await mark.PostAsJsonAsync($"/api/leaves/{id}/review", new { status = LeaveStatus.Approuve });

		api.WithDb(db =>
		{
			var notifications = db.Notifications.OrderBy(n => n.RecipientId).ToList();
			Assert.Equal([team.Sam.Id, team.Jane.Id], notifications.Select(n => n.RecipientId));
			Assert.All(notifications, n =>
			{
				Assert.Equal(NotificationType.LeaveOverlapsMeeting, n.Type);
				Assert.Equal("/calendar?vue=jour&date=2026-10-15", n.Link);
			});
			Assert.Equal("Tu seras en congé pendant « Revue », jeudi 15 octobre à 10 h.", notifications[0].Message);
			Assert.Equal("Sam Test sera en congé pendant « Revue », jeudi 15 octobre à 10 h.", notifications[1].Message);
		});
	}

	[Fact]
	public async Task Un_conge_refuse_ne_previent_personne()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db => db.Meetings.Add(Meeting(team.Jane, [team.Sam], "2026-10-15T14:00:00Z")));
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");

		var id = await RequestLeaveAsync(sam, "2026-10-15", "2026-10-15");
		await mark.PostAsJsonAsync($"/api/leaves/{id}/review", new { status = LeaveStatus.Refuse });

		api.WithDb(db => Assert.Empty(db.Notifications));
	}

	[Fact]
	public async Task L_organisateur_lit_l_heure_dans_son_fuseau()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Jane.Id)!.SiteId = paris.Id;
			db.Meetings.Add(Meeting(team.Jane, [team.Sam], "2026-10-15T14:00:00Z"));
		});
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");

		var id = await RequestLeaveAsync(sam, "2026-10-15", "2026-10-15");
		await mark.PostAsJsonAsync($"/api/leaves/{id}/review", new { status = LeaveStatus.Approuve });

		api.WithDb(db =>
		{
			var jane = db.Notifications.Single(n => n.RecipientId == team.Jane.Id);
			Assert.Equal("Sam Test sera en congé pendant « Revue », jeudi 15 octobre à 16 h.", jane.Message);
		});
	}

	[Fact]
	public async Task L_organisateur_en_conge_previent_ses_participants_et_son_manager_pour_un_meeting_d_equipe()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Employees.Find(team.Mark.Id)!.ManagerId = team.Alex.Id;
			db.Meetings.Add(Meeting(team.Mark, [team.Sam, team.Jane], "2026-10-15T14:00:00Z", isTeamMeeting: true));
			db.Meetings.Add(Meeting(team.Mark, [team.Sam], "2026-10-15T16:00:00Z", title: "Point")); // ordinaire : pas le manager
		});
		var mark = await api.LoginAsync("mark@test.local");
		var alex = await api.LoginAsync("alex@test.local");

		var id = await RequestLeaveAsync(mark, "2026-10-15", "2026-10-15");
		await alex.PostAsJsonAsync($"/api/leaves/{id}/review", new { status = LeaveStatus.Approuve });

		api.WithDb(db =>
		{
			var revue = db.Notifications.Where(n => n.Message.Contains("« Revue »")).OrderBy(n => n.RecipientId).ToList();
			Assert.Equal([team.Alex.Id, team.Mark.Id, team.Sam.Id, team.Jane.Id], revue.Select(n => n.RecipientId));
			Assert.Equal("Mark Test, qui organise « Revue », sera en congé, jeudi 15 octobre à 10 h.", revue[0].Message);
			Assert.Equal("Tu seras en congé pendant « Revue », jeudi 15 octobre à 10 h.", revue[1].Message);
			Assert.Equal("Mark Test, qui organise « Revue », sera en congé, jeudi 15 octobre à 10 h.", revue[2].Message);

			var point = db.Notifications.Where(n => n.Message.Contains("« Point »")).Select(n => n.RecipientId).Order().ToList();
			Assert.Equal([team.Mark.Id, team.Sam.Id], point);
		});
	}

	[Fact]
	public async Task Le_manager_participant_ou_inactif_n_est_pas_prevenu_en_double()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Employees.Find(team.Mark.Id)!.ManagerId = team.Alex.Id;
			db.Meetings.Add(Meeting(team.Mark, [team.Alex], "2026-10-15T14:00:00Z", isTeamMeeting: true));
		});
		var mark = await api.LoginAsync("mark@test.local");
		var gaby = await api.LoginAsync("gaby@test.local");

		var id = await RequestLeaveAsync(mark, "2026-10-15", "2026-10-15");
		await gaby.PostAsJsonAsync($"/api/leaves/{id}/review", new { status = LeaveStatus.Approuve });

		api.WithDb(db => Assert.Single(db.Notifications, n => n.RecipientId == team.Alex.Id));
	}

	[Fact]
	public async Task Un_manager_inactif_n_est_pas_prevenu()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Employees.Find(team.Mark.Id)!.ManagerId = team.Alex.Id;
			db.Employees.Find(team.Alex.Id)!.IsActive = false;
			db.Meetings.Add(Meeting(team.Mark, [team.Sam], "2026-10-15T14:00:00Z", isTeamMeeting: true));
		});
		var mark = await api.LoginAsync("mark@test.local");
		var gaby = await api.LoginAsync("gaby@test.local");

		var id = await RequestLeaveAsync(mark, "2026-10-15", "2026-10-15");
		await gaby.PostAsJsonAsync($"/api/leaves/{id}/review", new { status = LeaveStatus.Approuve });

		api.WithDb(db => Assert.DoesNotContain(db.Notifications, n => n.RecipientId == team.Alex.Id));
	}

	[Fact]
	public async Task Un_conge_de_maladie_retroactif_n_annonce_que_les_meetings_a_venir()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Meetings.Add(Meeting(team.Jane, [team.Sam], "2026-10-13T14:00:00Z", title: "Mardi"));      // passé
			db.Meetings.Add(Meeting(team.Jane, [team.Sam], "2026-10-14T14:00:00Z", title: "Ce matin"));   // passé (10 h)
			db.Meetings.Add(Meeting(team.Jane, [team.Sam], "2026-10-14T18:00:00Z", title: "Cet après-midi"));
			db.Meetings.Add(Meeting(team.Jane, [team.Sam], "2026-10-15T14:00:00Z", title: "Demain"));
		});
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync("/api/leaves/declare",
			new { employeeId = team.Sam.Id, startDate = "2026-10-12", endDate = "2026-10-15" });

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		api.WithDb(db =>
		{
			var jane = db.Notifications.Where(n => n.RecipientId == team.Jane.Id).Select(n => n.Message).Order().ToList();
			Assert.Equal(
				["Sam Test sera en congé pendant « Cet après-midi », mercredi 14 octobre à 14 h.",
				 "Sam Test sera en congé pendant « Demain », jeudi 15 octobre à 10 h."],
				jane);
			Assert.Equal(2, db.Notifications.Count(n => n.RecipientId == team.Sam.Id)); // Mark a agi : Sam est prévenu
		});
	}

	[Fact]
	public async Task Un_conge_de_maladie_entierement_passe_ne_previent_personne()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db => db.Meetings.Add(Meeting(team.Jane, [team.Sam], "2026-10-13T14:00:00Z")));
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync("/api/leaves/declare",
			new { employeeId = team.Sam.Id, startDate = "2026-10-12", endDate = "2026-10-13" });

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		api.WithDb(db => Assert.Empty(db.Notifications));
	}

	// ─── Modifier un meeting dont un participant est en congé ───────────────

	[Fact]
	public async Task Corriger_le_titre_reste_permis_quand_un_participant_est_en_conge()
	{
		using var api = Start();
		var team = api.Team;
		var meetingId = SeedRevue(api, team.Jane, [team.Sam]);
		api.WithDb(db => db.LeaveRequests.Add(Leave(team.Sam, 15)));
		var jane = await api.LoginAsync("jane@test.local");

		var res = await jane.PutAsJsonAsync($"/api/meetings/{meetingId}", Edit([team.Sam.Id], title: "Revue corrigée"));

		Assert.Equal(HttpStatusCode.OK, res.StatusCode); // ni 400 (congé), ni 409 (hors horaire)
		Assert.Equal("Revue corrigée", (await res.Content.ReadFromJsonAsync<MeetingResponse>())!.Title);
	}

	[Fact]
	public async Task L_organisateur_en_conge_peut_corriger_le_titre_mais_pas_deplacer_sur_son_conge()
	{
		using var api = Start();
		var team = api.Team;
		var meetingId = SeedRevue(api, team.Mark, [team.Sam]);
		api.WithDb(db => db.LeaveRequests.Add(Leave(team.Mark, 15, endDay: 16)));
		var mark = await api.LoginAsync("mark@test.local");

		var rename = await mark.PutAsJsonAsync($"/api/meetings/{meetingId}", Edit([team.Sam.Id], title: "Revue corrigée"));
		var move = await mark.PutAsJsonAsync($"/api/meetings/{meetingId}",
			Edit([team.Sam.Id], start: "2026-10-16T14:00:00Z", end: "2026-10-16T15:00:00Z"));

		Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, move.StatusCode);
		Assert.Equal("Tu es en congé ce jour-là.", await ErrorMessage(move));
	}

	[Fact]
	public async Task Deplacer_sur_le_conge_d_un_participant_reste_refuse()
	{
		using var api = Start();
		var team = api.Team;
		var meetingId = SeedRevue(api, team.Jane, [team.Sam]);
		api.WithDb(db => db.LeaveRequests.Add(Leave(team.Sam, 15, endDay: 16)));
		var jane = await api.LoginAsync("jane@test.local");

		var res = await jane.PutAsJsonAsync($"/api/meetings/{meetingId}",
			Edit([team.Sam.Id], start: "2026-10-16T14:00:00Z", end: "2026-10-16T15:00:00Z"));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Equal("Sam Test est en congé ce jour-là.", await ErrorMessage(res));
	}

	[Fact]
	public async Task Ajouter_un_participant_en_conge_reste_refuse_meme_sans_changer_l_heure()
	{
		using var api = Start();
		var team = api.Team;
		var meetingId = SeedRevue(api, team.Jane, [team.Sam]);
		api.WithDb(db =>
		{
			db.LeaveRequests.Add(Leave(team.Sam, 15));
			db.LeaveRequests.Add(Leave(team.Mark, 15));
		});
		var jane = await api.LoginAsync("jane@test.local");

		var res = await jane.PutAsJsonAsync($"/api/meetings/{meetingId}", Edit([team.Sam.Id, team.Mark.Id]));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Equal("Mark Test est en congé ce jour-là.", await ErrorMessage(res)); // Sam, déjà invité, n'est pas nommé
	}

	[Fact]
	public async Task Sans_changer_l_heure_seul_un_participant_ajoute_hors_horaire_demande_confirmation()
	{
		using var api = Start();
		var team = api.Team;
		var meetingId = SeedRevue(api, team.Jane, [team.Sam], start: "2026-10-15T22:00:00Z", end: "2026-10-15T23:00:00Z"); // 18 h
		var jane = await api.LoginAsync("jane@test.local");

		var rename = await jane.PutAsJsonAsync($"/api/meetings/{meetingId}", Edit([team.Sam.Id], title: "Tard",
			start: "2026-10-15T22:00:00Z", end: "2026-10-15T23:00:00Z"));
		var addMark = await jane.PutAsJsonAsync($"/api/meetings/{meetingId}", Edit([team.Sam.Id, team.Mark.Id],
			start: "2026-10-15T22:00:00Z", end: "2026-10-15T23:00:00Z"));

		Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
		Assert.Equal(HttpStatusCode.Conflict, addMark.StatusCode);
		var conflict = (await addMark.Content.ReadFromJsonAsync<MeetingConflictResponse>())!;
		Assert.Equal([team.Mark.Id], conflict.OutsideHours.Select(o => o.EmployeeId));
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	/// <summary>L'API avec les horaires de Sam et de Mark : lundi au vendredi, 8 h → 17 h.</summary>
	private static ApiFactory Start()
	{
		var api = new ApiFactory().Start();
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(api.Team.Sam));
			db.WorkSchedules.Add(WeekdaySchedule(api.Team.Mark));
		});
		return api;
	}

	private static async Task<int> RequestLeaveAsync(HttpClient client, string start, string end)
	{
		var res = await client.PostAsJsonAsync("/api/leaves", new { startDate = start, endDate = end, type = LeaveType.Conge });
		res.EnsureSuccessStatusCode();
		return (await res.Content.ReadFromJsonAsync<LeaveRequestResponse>())!.Id;
	}

	/// <summary>Un meeting d'une heure déjà en base, organisé par <paramref name="organizer"/>.</summary>
	private static Meeting Meeting(
		Employee organizer,
		Employee[] participants,
		string start,
		string title = "Revue",
		MeetingKind kind = MeetingKind.Meeting,
		bool isTeamMeeting = false)
	{
		var startAt = DateTime.Parse(start, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
		return new()
		{
			Kind = kind,
			Title = title,
			StartAt = startAt,
			EndAt = startAt.AddHours(1),
			OrganizerId = organizer.Id,
			IsTeamMeeting = isTeamMeeting,
			Participants =
			[
				new MeetingParticipant { EmployeeId = organizer.Id },
				.. participants.Select(p => new MeetingParticipant { EmployeeId = p.Id }),
			],
		};
	}

	/// <summary>« Revue », jeudi 15 octobre de 10 h à 11 h à Montréal par défaut ; retourne son id.</summary>
	private static int SeedRevue(
		ApiFactory api,
		Employee organizer,
		Employee[] participants,
		string start = "2026-10-15T14:00:00Z",
		string end = "2026-10-15T15:00:00Z")
	{
		var id = 0;
		api.WithDb(db =>
		{
			var meeting = Meeting(organizer, participants, start);
			meeting.EndAt = DateTime.Parse(end, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
			db.Meetings.Add(meeting);
			db.SaveChanges();
			id = meeting.Id;
		});
		return id;
	}

	/// <summary>Le corps d'un PUT : par défaut, le même créneau que <see cref="SeedRevue"/>.</summary>
	private static object Edit(
		int[] participantIds,
		string title = "Revue",
		string start = "2026-10-15T14:00:00Z",
		string end = "2026-10-15T15:00:00Z") => new
	{
		title,
		startAt = start,
		endAt = end,
		isFlexible = false,
		participantIds,
		confirmConflicts = false,
	};

	/// <summary>Un congé approuvé en octobre 2026, déjà en base (aucune notification).</summary>
	private static LeaveRequest Leave(Employee employee, int day, int? endDay = null) => new()
	{
		EmployeeId = employee.Id,
		StartDate = new DateTime(2026, 10, day),
		EndDate = new DateTime(2026, 10, endDay ?? day),
		Type = LeaveType.Conge,
		Status = LeaveStatus.Approuve,
	};

	private static async Task<string> ErrorMessage(HttpResponseMessage res) =>
		(await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("message").GetString()!;

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
}
