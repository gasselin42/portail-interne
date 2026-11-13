using System.Net.Http.Json;
using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Absence qui chevauche un meeting (phase 3, étape 10c, R24) : le meeting est conservé, l'organisateur et l'employé
/// sont notifiés ; le manager est prévenu d'un départ imprévu et d'un retour non confirmé (tâche de fond).
/// « Maintenant » = mercredi 14 octobre 2026, midi à Montréal (16 h UTC).
/// Sam travaille du lundi au vendredi, 8 h à 17 h ; Mark est son manager ; Jane organise « Revue ».
/// </summary>
public class AbsenceNotificationsTests
{
	private const string Tomorrow = "2026-10-15";

	// ─── Approbation, création, annulation ──────────────────────────────────

	[Fact]
	public async Task Une_absence_creee_par_le_manager_previent_l_organisateur_et_l_employe()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-15T14:00:00Z", "2026-10-15T15:00:00Z")); // 10 h → 11 h
			db.Meetings.Add(Revue(team.Sam, null, "2026-10-15T13:00:00Z", "2026-10-15T14:00:00Z", MeetingKind.PersonalBlock)); // bloc : ignoré
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-15T15:00:00Z", "2026-10-15T16:00:00Z", title: "Après")); // touche : ignoré
		});
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync(Url(team.Sam), Planned(Tomorrow, "09:30:00", "10:30:00"));

		res.EnsureSuccessStatusCode();
		api.WithDb(db =>
		{
			var notifications = db.Notifications.OrderBy(n => n.RecipientId).ToList();
			Assert.Equal([team.Sam.Id, team.Jane.Id], notifications.Select(n => n.RecipientId));
			Assert.All(notifications, n =>
			{
				Assert.Equal(NotificationType.AbsenceOverlapsMeeting, n.Type);
				Assert.Equal("/calendar?vue=jour&date=2026-10-15", n.Link);
			});
			Assert.Equal("Tu manqueras le début de « Revue », jeudi 15 octobre à 10 h.", notifications[0].Message);
			Assert.Equal("Sam Test manquera le début de « Revue », jeudi 15 octobre à 10 h.", notifications[1].Message);
		});
	}

	[Fact]
	public async Task Une_demande_en_attente_ne_previent_personne_avant_l_approbation()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db => db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-15T14:00:00Z", "2026-10-15T15:00:00Z")));
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");

		var created = await sam.PostAsJsonAsync(Url(team.Sam), Planned(Tomorrow, "10:00:00", "11:00:00"));
		var id = (await created.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetInt32();
		api.WithDb(db => Assert.Empty(db.Notifications));

		await mark.PostAsJsonAsync($"{Url(team.Sam)}/{id}/review", new { status = LeaveStatus.Approuve });

		api.WithDb(db =>
		{
			Assert.Equal(
				["Sam Test manquera « Revue », jeudi 15 octobre à 10 h.", "Tu manqueras « Revue », jeudi 15 octobre à 10 h."],
				db.Notifications.OrderBy(n => n.Id).Select(n => n.Message).ToList().Order());
		});
	}

	[Fact]
	public async Task Une_demande_refusee_ne_previent_personne()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db => db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-15T14:00:00Z", "2026-10-15T15:00:00Z")));
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");

		var created = await sam.PostAsJsonAsync(Url(team.Sam), Planned(Tomorrow, "10:00:00", "11:00:00"));
		var id = (await created.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetInt32();
		await mark.PostAsJsonAsync($"{Url(team.Sam)}/{id}/review", new { status = LeaveStatus.Refuse });

		api.WithDb(db => Assert.Empty(db.Notifications));
	}

	[Theory]
	[InlineData("10:00:00", "11:00:00", "« Revue »")]                  // tout le meeting
	[InlineData("09:00:00", "10:30:00", "le début de « Revue »")]
	[InlineData("10:30:00", "12:00:00", "la fin de « Revue »")]
	[InlineData("10:15:00", "10:45:00", "une partie de « Revue »")]
	public async Task Le_message_dit_quelle_partie_du_meeting_est_manquee(string start, string end, string part)
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db => db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-15T14:00:00Z", "2026-10-15T15:00:00Z")));
		var mark = await api.LoginAsync("mark@test.local");

		await mark.PostAsJsonAsync(Url(team.Sam), Planned(Tomorrow, start, end));

		api.WithDb(db => Assert.Equal(
			$"Sam Test manquera {part}, jeudi 15 octobre à 10 h.",
			db.Notifications.Single(n => n.RecipientId == team.Jane.Id).Message));
	}

	[Fact]
	public async Task L_employe_organisateur_est_seul_prevenu_jamais_les_participants()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db => db.Meetings.Add(Revue(team.Sam, team.Jane, "2026-10-15T14:00:00Z", "2026-10-15T15:00:00Z")));
		var mark = await api.LoginAsync("mark@test.local");

		await mark.PostAsJsonAsync(Url(team.Sam), Planned(Tomorrow, "10:00:00", "11:00:00"));

		api.WithDb(db =>
		{
			var notification = Assert.Single(db.Notifications);
			Assert.Equal(team.Sam.Id, notification.RecipientId);
			Assert.Equal("Tu manqueras « Revue », jeudi 15 octobre à 10 h.", notification.Message);
		});
	}

	[Fact]
	public async Task L_heure_est_dans_le_fuseau_de_l_organisateur()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Jane.Id)!.SiteId = paris.Id;
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-15T14:00:00Z", "2026-10-15T15:00:00Z"));
		});
		var mark = await api.LoginAsync("mark@test.local");

		await mark.PostAsJsonAsync(Url(team.Sam), Planned(Tomorrow, "10:00:00", "11:00:00"));

		api.WithDb(db => Assert.Equal(
			"Sam Test manquera « Revue », jeudi 15 octobre à 16 h.", // 10 h à Montréal = 16 h à Paris
			db.Notifications.Single(n => n.RecipientId == team.Jane.Id).Message));
	}

	[Fact]
	public async Task Annuler_une_absence_approuvee_previent_que_l_employe_sera_la()
	{
		using var api = Start();
		var team = api.Team;
		var absenceId = 0;
		api.WithDb(db =>
		{
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-15T14:00:00Z", "2026-10-15T15:00:00Z"));
			var absence = Absence(team.Sam, new DateOnly(2026, 10, 15), new(10, 0), new(11, 0));
			db.Absences.Add(absence);
			db.SaveChanges();
			absenceId = absence.Id;
		});
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsync($"{Url(team.Sam)}/{absenceId}/cancel", null);

		res.EnsureSuccessStatusCode();
		api.WithDb(db =>
		{
			var notifications = db.Notifications.OrderBy(n => n.RecipientId).ToList();
			Assert.All(notifications, n => Assert.Equal(NotificationType.AbsenceCancelled, n.Type));
			Assert.Equal(
				["Ton absence pendant « Revue » est annulée, jeudi 15 octobre à 10 h.",
				 "L'absence de Sam Test pendant « Revue » est annulée, jeudi 15 octobre à 10 h."],
				notifications.Select(n => n.Message));
		});
	}

	[Fact]
	public async Task Annuler_une_demande_en_attente_ne_previent_personne()
	{
		using var api = Start();
		var team = api.Team;
		var absenceId = 0;
		api.WithDb(db =>
		{
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-15T14:00:00Z", "2026-10-15T15:00:00Z"));
			var absence = Absence(team.Sam, new DateOnly(2026, 10, 15), new(10, 0), new(11, 0), LeaveStatus.EnAttente);
			db.Absences.Add(absence);
			db.SaveChanges();
			absenceId = absence.Id;
		});
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsync($"{Url(team.Sam)}/{absenceId}/cancel", null);

		res.EnsureSuccessStatusCode();
		api.WithDb(db => Assert.Empty(db.Notifications));
	}

	// ─── « Je pars » ────────────────────────────────────────────────────────

	[Fact]
	public async Task Un_depart_imprevu_annonce_le_reste_de_la_journee_et_previent_le_manager()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-14T18:00:00Z", "2026-10-14T19:00:00Z"));                  // 14 h
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-14T14:00:00Z", "2026-10-14T15:00:00Z", title: "Matin")); // passé
		});
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync(Url(team.Sam, "depart"), new { });

		res.EnsureSuccessStatusCode();
		api.WithDb(db =>
		{
			var notifications = db.Notifications.ToList();
			Assert.Equal(2, notifications.Count); // Sam a agi lui-même : rien pour lui

			var jane = notifications.Single(n => n.RecipientId == team.Jane.Id);
			Assert.Equal(NotificationType.AbsenceOverlapsMeeting, jane.Type);
			Assert.Equal("Sam Test manquera « Revue », mercredi 14 octobre à 14 h.", jane.Message);

			var mark = notifications.Single(n => n.RecipientId == team.Mark.Id);
			Assert.Equal(NotificationType.AbsenceUnplanned, mark.Type);
			Assert.Equal("Sam Test a signalé un départ imprévu à 12 h.", mark.Message);
			Assert.Equal("/team", mark.Link);
		});
	}

	[Fact]
	public async Task Le_manager_qui_fait_partir_Sam_ne_se_previent_pas_lui_meme()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db => db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-14T18:00:00Z", "2026-10-14T19:00:00Z")));
		var mark = await api.LoginAsync("mark@test.local");

		await mark.PostAsJsonAsync(Url(team.Sam, "depart"), new { });

		api.WithDb(db =>
		{
			Assert.DoesNotContain(db.Notifications, n => n.RecipientId == team.Mark.Id);
			Assert.Equal("Tu manqueras « Revue », mercredi 14 octobre à 14 h.",
				db.Notifications.Single(n => n.RecipientId == team.Sam.Id).Message);
		});
	}

	[Fact]
	public async Task Sans_manager_les_Admins_du_site_sont_prevenus()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(team.Jane));
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			var pierre = new Employee
			{
				FirstName = "Pierre", LastName = "Test", Email = "pierre@test.local", SiteId = paris.Id, IsActive = true,
				UserAccount = new UserAccount { Email = "pierre@test.local", Role = Role.Admin, IsActive = true },
			};
			db.Employees.Add(pierre);
		});
		var jane = await api.LoginAsync("jane@test.local");

		await jane.PostAsJsonAsync(Url(team.Jane, "depart"), new { });

		api.WithDb(db =>
		{
			var notification = Assert.Single(db.Notifications); // Alex (Admin de Montréal), pas Pierre (Paris) ni Gaby (Admin global)
			Assert.Equal(team.Alex.Id, notification.RecipientId);
			Assert.Equal("Jane Test a signalé un départ imprévu à 12 h.", notification.Message);
		});
	}

	[Fact]
	public async Task Partir_en_avance_annonce_seulement_le_bout_avant_l_heure_prevue()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Absences.Add(Absence(team.Sam, ApiFactory.Today, new(12, 15), new(13, 0))); // déjà annoncée à l'approbation
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-14T16:00:00Z", "2026-10-14T16:30:00Z"));                   // 12 h → 12 h 30
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-14T16:30:00Z", "2026-10-14T17:00:00Z", title: "Pendant")); // 12 h 30 → 13 h
		});
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync(Url(team.Sam, "depart"), new { });

		res.EnsureSuccessStatusCode();
		api.WithDb(db =>
		{
			var notification = Assert.Single(db.Notifications); // ni « Pendant », ni le manager : l'absence était prévue
			Assert.Equal("Sam Test manquera le début de « Revue », mercredi 14 octobre à 12 h.", notification.Message);
		});
	}

	// ─── « Je suis de retour » ──────────────────────────────────────────────

	[Fact]
	public async Task Revenir_d_un_depart_imprevu_previent_que_l_employe_sera_la()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Absences.Add(Unplanned(team.Sam, ApiFactory.Today, new(11, 0)));
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-14T18:00:00Z", "2026-10-14T19:00:00Z")); // 14 h
		});
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsync(Url(team.Sam, "return"), null);

		res.EnsureSuccessStatusCode();
		api.WithDb(db =>
		{
			var notification = Assert.Single(db.Notifications);
			Assert.Equal(team.Jane.Id, notification.RecipientId);
			Assert.Equal(NotificationType.AbsenceCancelled, notification.Type);
			Assert.Equal("L'absence de Sam Test pendant « Revue » est annulée, mercredi 14 octobre à 14 h.", notification.Message);
		});
	}

	[Fact]
	public async Task Revenir_avant_la_fin_prevue_previent_pour_le_reste_de_l_absence()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var absence = Absence(team.Sam, ApiFactory.Today, new(11, 0), new(13, 0));
			absence.StartedAt = new(11, 0);
			db.Absences.Add(absence);
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-14T16:30:00Z", "2026-10-14T17:00:00Z"));               // 12 h 30 : dedans
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-14T18:00:00Z", "2026-10-14T19:00:00Z", title: "Après")); // jamais annoncé
		});
		var sam = await api.LoginAsync("sam@test.local");

		await sam.PostAsync(Url(team.Sam, "return"), null);

		api.WithDb(db => Assert.Equal(
			"L'absence de Sam Test pendant « Revue » est annulée, mercredi 14 octobre à 12 h 30.",
			Assert.Single(db.Notifications).Message));
	}

	// ─── Tâche de fond : retour tardif, retour non confirmé ─────────────────

	[Fact]
	public async Task Un_retour_tardif_annonce_les_meetings_qui_restent_une_seule_fois()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var absence = Absence(team.Sam, ApiFactory.Today, new(10, 0), new(11, 0));
			absence.StartedAt = new(10, 0); // parti à 10 h, pas revenu à 11 h ; il est midi
			db.Absences.Add(absence);
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-14T18:00:00Z", "2026-10-14T19:00:00Z"));                // 14 h
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-14T14:00:00Z", "2026-10-14T15:00:00Z", title: "Pendant")); // 10 h : déjà annoncé
		});

		await api.RunAbsenceWatcherAsync();
		await api.RunAbsenceWatcherAsync();

		api.WithDb(db =>
		{
			var notifications = db.Notifications.OrderBy(n => n.RecipientId).ToList();
			Assert.Equal(
				["Tu manqueras « Revue », mercredi 14 octobre à 14 h.", "Sam Test manquera « Revue », mercredi 14 octobre à 14 h."],
				notifications.Select(n => n.Message)); // le système a agi : Sam est prévenu aussi
			Assert.True(db.Absences.Single().LateReturnNotified);
			Assert.False(db.Absences.Single().UnconfirmedReturnNotified); // la journée n'est pas finie
		});
	}

	[Fact]
	public async Task Avant_l_heure_de_retour_prevue_rien_n_est_signale()
	{
		using var api = Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var absence = Absence(team.Sam, ApiFactory.Today, new(11, 0), new(13, 0));
			absence.StartedAt = new(11, 0);
			db.Absences.Add(absence);
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-14T18:00:00Z", "2026-10-14T19:00:00Z"));
		});

		await api.RunAbsenceWatcherAsync();

		api.WithDb(db =>
		{
			Assert.Empty(db.Notifications);
			Assert.False(db.Absences.Single().LateReturnNotified);
		});
	}

	[Fact]
	public async Task En_fin_de_journee_le_manager_est_prevenu_une_seule_fois_du_retour_non_confirme()
	{
		// 17 h 30 à Montréal : la journée de Sam (8 h → 17 h) est finie
		using var api = Start(new DateTimeOffset(2026, 10, 14, 21, 30, 0, TimeSpan.Zero));
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Absences.Add(Unplanned(team.Sam, ApiFactory.Today, new(15, 0)));
			var planned = Absence(team.Sam, ApiFactory.Today, new(10, 0), new(11, 0)); // retard jamais signalé…
			planned.StartedAt = new(10, 0);
			db.Absences.Add(planned);
			db.Meetings.Add(Revue(team.Jane, team.Sam, "2026-10-14T18:00:00Z", "2026-10-14T19:00:00Z")); // … mais ce meeting est passé
		});

		await api.RunAbsenceWatcherAsync();
		await api.RunAbsenceWatcherAsync();

		api.WithDb(db =>
		{
			var notifications = db.Notifications.ToList();
			Assert.Equal(2, notifications.Count); // une par absence, et aucune pour le meeting passé
			Assert.All(notifications, n =>
			{
				Assert.Equal(team.Mark.Id, n.RecipientId);
				Assert.Equal(NotificationType.AbsenceReturnUnconfirmed, n.Type);
				Assert.Equal("Sam Test n'a pas confirmé son retour, mercredi 14 octobre.", n.Message);
				Assert.Equal("/team", n.Link);
			});
			Assert.All(db.Absences, a => Assert.True(a.LateReturnNotified || a.PlannedEnd is null));
			Assert.All(db.Absences, a => Assert.True(a.UnconfirmedReturnNotified));
		});
	}

	[Fact]
	public async Task Les_absences_revenues_en_attente_ou_anciennes_sont_ignorees()
	{
		using var api = Start(new DateTimeOffset(2026, 10, 14, 21, 30, 0, TimeSpan.Zero));
		var team = api.Team;
		api.WithDb(db =>
		{
			var returned = Unplanned(team.Sam, ApiFactory.Today, new(9, 0));
			returned.EndedAt = new(10, 0);
			returned.ReturnConfirmed = true;
			db.Absences.Add(returned);

			var pending = Unplanned(team.Sam, ApiFactory.Today, new(11, 0));
			pending.Status = LeaveStatus.EnAttente;
			db.Absences.Add(pending);

			db.Absences.Add(Unplanned(team.Sam, new DateOnly(2026, 10, 1), new(9, 0))); // avant le déploiement
		});

		await api.RunAbsenceWatcherAsync();

		api.WithDb(db => Assert.Empty(db.Notifications));
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	/// <summary>L'API avec l'horaire de Sam : lundi au vendredi, 8 h → 17 h.</summary>
	private static ApiFactory Start(DateTimeOffset? now = null)
	{
		var api = new ApiFactory(now).Start();
		api.WithDb(db => db.WorkSchedules.Add(WeekdaySchedule(api.Team.Sam)));
		return api;
	}

	private static string Url(Employee employee, string action = "") =>
		$"/api/employees/{employee.Id}/absences{(action == "" ? "" : "/" + action)}";

	private static object Planned(string date, string start, string end) =>
		new { date, plannedStart = start, plannedEnd = end };

	/// <summary>Un meeting (ou un bloc) organisé par <paramref name="organizer"/>, avec un participant au plus.</summary>
	private static Meeting Revue(
		Employee organizer,
		Employee? participant,
		string start,
		string end,
		MeetingKind kind = MeetingKind.Meeting,
		string title = "Revue") => new()
	{
		Kind = kind,
		Title = title,
		StartAt = DateTime.Parse(start, null, System.Globalization.DateTimeStyles.AdjustToUniversal),
		EndAt = DateTime.Parse(end, null, System.Globalization.DateTimeStyles.AdjustToUniversal),
		OrganizerId = organizer.Id,
		Participants =
		[
			new MeetingParticipant { EmployeeId = organizer.Id },
			.. participant is null ? [] : new[] { new MeetingParticipant { EmployeeId = participant.Id } },
		],
	};

	/// <summary>Une absence prévue, approuvée par défaut, déjà en base (aucune notification).</summary>
	private static Absence Absence(Employee employee, DateOnly date, TimeOnly start, TimeOnly end, LeaveStatus status = LeaveStatus.Approuve) => new()
	{
		EmployeeId = employee.Id,
		Date = date,
		PlannedStart = start,
		PlannedEnd = end,
		Status = status,
		CreatedById = employee.Id,
	};

	/// <summary>Un « Je pars » sans retour, déjà en base.</summary>
	private static Absence Unplanned(Employee employee, DateOnly date, TimeOnly startedAt) => new()
	{
		EmployeeId = employee.Id,
		Date = date,
		StartedAt = startedAt,
		Status = LeaveStatus.Approuve,
		CreatedById = employee.Id,
	};

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
