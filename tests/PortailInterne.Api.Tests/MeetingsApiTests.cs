using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// POST /api/meetings : blocs personnels (étape 3, R21, R22) meetings de groupe (étape 4, R7)
/// et PUT /api/meetings/{id}/color : couleur par personne (étape 4b, R22) ; congé approuvé = refus (étape 5, R6) ;
/// conflits d'horaire : 409 puis confirmation (étape 6, R7, R17, R21) ; hors horaire effectif d'un participant (étape 6b, R15, R16).
/// « Maintenant » = mercredi 14 octobre 2026, 16 h UTC (midi à Montréal, UTC−4).
/// </summary>
public class MeetingsApiTests
{
	[Fact]
	public async Task Un_bloc_personnel_est_cree_avec_son_proprietaire_pour_seul_participant()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Block(isFlexible: true, color: "teal"));
		var created = (await res.Content.ReadFromJsonAsync<MeetingResponse>())!;

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		Assert.Equal(MeetingKind.PersonalBlock, created.Kind);
		Assert.Equal("Projet X", created.Title);
		Assert.Equal(new DateTime(2026, 10, 15, 14, 0, 0, DateTimeKind.Utc), created.StartAt);
		Assert.Equal(DateTimeKind.Utc, created.StartAt.Kind);
		Assert.Equal(api.Team.Sam.Id, created.OrganizerId);
		Assert.True(created.IsFlexible);
		Assert.False(created.IsTeamMeeting);
		Assert.Equal("teal", created.Color);

		api.WithDb(db =>
		{
			var participant = Assert.Single(db.MeetingParticipants.Where(p => p.MeetingId == created.Id));
			Assert.Equal(api.Team.Sam.Id, participant.EmployeeId);
			Assert.Equal("teal", participant.Color);
		});
	}

	[Fact]
	public async Task Sans_couleur_la_reponse_donne_la_couleur_par_defaut_mais_la_base_garde_null()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Block());
		var created = (await res.Content.ReadFromJsonAsync<MeetingResponse>())!;

		Assert.Equal(MeetingColors.Default, created.Color);
		api.WithDb(db => Assert.Null(db.MeetingParticipants.Single(p => p.MeetingId == created.Id).Color));
	}

	[Fact]
	public async Task Le_titre_est_nettoye_et_une_description_vide_devient_null()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Block(title: "  Projet X  ", description: "   "));
		var created = (await res.Content.ReadFromJsonAsync<MeetingResponse>())!;

		Assert.Equal("Projet X", created.Title);
		Assert.Null(created.Description);
	}

	[Fact]
	public async Task Un_bloc_peut_finir_a_minuit_heure_du_site()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		// 23 h → minuit à Montréal = 3 h → 4 h UTC le lendemain
		var res = await sam.PostAsJsonAsync("/api/meetings", Block(start: "2026-10-16T03:00:00Z", end: "2026-10-16T04:00:00Z"));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	[Fact]
	public async Task La_tranche_de_15_minutes_en_cours_est_acceptee_la_precedente_non()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var current = await sam.PostAsJsonAsync("/api/meetings", Block(start: "2026-10-14T16:00:00Z", end: "2026-10-14T17:00:00Z"));
		var previous = await sam.PostAsJsonAsync("/api/meetings", Block(start: "2026-10-14T15:45:00Z", end: "2026-10-14T17:00:00Z"));

		Assert.Equal(HttpStatusCode.Created, current.StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, previous.StatusCode);
	}

	[Theory]
	[InlineData("titre vide", """{ "kind": 1, "title": "   ", "startAt": "2026-10-15T14:00:00Z", "endAt": "2026-10-15T16:00:00Z" }""")]
	[InlineData("type absent", """{ "title": "Projet X", "startAt": "2026-10-15T14:00:00Z", "endAt": "2026-10-15T16:00:00Z" }""")]
	[InlineData("début absent", """{ "kind": 1, "title": "Projet X", "endAt": "2026-10-15T16:00:00Z" }""")]
	[InlineData("participants", """{ "kind": 1, "title": "Projet X", "startAt": "2026-10-15T14:00:00Z", "endAt": "2026-10-15T16:00:00Z", "participantIds": [3] }""")]
	[InlineData("couleur inconnue", """{ "kind": 1, "title": "Projet X", "startAt": "2026-10-15T14:00:00Z", "endAt": "2026-10-15T16:00:00Z", "color": "red" }""")]
	[InlineData("heure sans fuseau", """{ "kind": 1, "title": "Projet X", "startAt": "2026-10-15T14:00:00", "endAt": "2026-10-15T16:00:00" }""")]
	[InlineData("fin avant le début", """{ "kind": 1, "title": "Projet X", "startAt": "2026-10-15T16:00:00Z", "endAt": "2026-10-15T14:00:00Z" }""")]
	[InlineData("fin égale au début", """{ "kind": 1, "title": "Projet X", "startAt": "2026-10-15T14:00:00Z", "endAt": "2026-10-15T14:00:00Z" }""")]
	[InlineData("hors tranche de 15 min", """{ "kind": 1, "title": "Projet X", "startAt": "2026-10-15T14:10:00Z", "endAt": "2026-10-15T16:00:00Z" }""")]
	// 22 h → 1 h à Montréal : même jour en UTC (2 h → 5 h), mais deux jours pour le site
	[InlineData("deux jours pour le site", """{ "kind": 1, "title": "Projet X", "startAt": "2026-10-16T02:00:00Z", "endAt": "2026-10-16T05:00:00Z" }""")]
	[InlineData("passé", """{ "kind": 1, "title": "Projet X", "startAt": "2026-10-13T14:00:00Z", "endAt": "2026-10-13T16:00:00Z" }""")]
	public async Task Requete_invalide_refusee(string _, string json)
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsync("/api/meetings", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		api.WithDb(db => Assert.Empty(db.Meetings));
	}

	[Fact]
	public async Task Un_meeting_de_groupe_enregistre_chaque_participant_et_les_invite()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var sam = await api.LoginAsync("sam@test.local");

		// Doublon et soi-même : retirés sans erreur
		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id, team.Mark.Id, team.Jane.Id, team.Sam.Id], color: "pink"));
		var created = (await res.Content.ReadFromJsonAsync<MeetingResponse>())!;

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		Assert.Equal(MeetingKind.Meeting, created.Kind);
		Assert.Equal("pink", created.Color);

		api.WithDb(db =>
		{
			var participants = db.MeetingParticipants.Where(p => p.MeetingId == created.Id).ToList();
			Assert.Equal(new[] { team.Sam.Id, team.Mark.Id, team.Jane.Id }.Order(), participants.Select(p => p.EmployeeId).Order());
			Assert.Equal("pink", participants.Single(p => p.EmployeeId == team.Sam.Id).Color);
			Assert.All(participants.Where(p => p.EmployeeId != team.Sam.Id), p => Assert.Null(p.Color));

			var notifications = db.Notifications.ToList();
			Assert.Equal(new[] { team.Mark.Id, team.Jane.Id }.Order(), notifications.Select(n => n.RecipientId).Order());
			Assert.All(notifications, n =>
			{
				Assert.Equal(NotificationType.MeetingInvitation, n.Type);
				Assert.Equal("Sam Test t'invite à « Revue », jeudi 15 octobre à 10 h.", n.Message);
				Assert.Equal("/calendar?vue=jour&date=2026-10-15", n.Link);
				Assert.Null(n.ReadAt);
			});
		});
	}

	[Fact]
	public async Task L_invitation_est_dans_le_fuseau_du_participant()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Jane.Id)!.SiteId = paris.Id;
		});
		var sam = await api.LoginAsync("sam@test.local");

		// 18 h 30 à Montréal = 0 h 30 le lendemain à Paris
		await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id], start: "2026-10-15T22:30:00Z", end: "2026-10-15T23:00:00Z"));

		api.WithDb(db =>
		{
			var invitation = db.Notifications.Single();
			Assert.Equal("Sam Test t'invite à « Revue », vendredi 16 octobre à 0 h 30.", invitation.Message);
			Assert.Equal("/calendar?vue=jour&date=2026-10-16", invitation.Link);
		});
	}

	[Theory]
	[InlineData("mark@test.local", true)]   // manager de Sam
	[InlineData("alex@test.local", true)]   // Admin
	[InlineData("jane@test.local", false)]  // ni l'un ni l'autre
	public async Task Le_meeting_est_d_equipe_selon_l_organisateur(string email, bool expected)
	{
		using var api = new ApiFactory().Start();
		var client = await api.LoginAsync(email);

		var res = await client.PostAsJsonAsync("/api/meetings", Group([api.Team.Sam.Id]));
		var created = (await res.Content.ReadFromJsonAsync<MeetingResponse>())!;

		Assert.Equal(expected, created.IsTeamMeeting);
	}

	[Fact]
	public async Task Un_bloc_personnel_n_est_jamais_d_equipe_meme_pour_un_admin()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var created = (await (await alex.PostAsJsonAsync("/api/meetings", Block())).Content.ReadFromJsonAsync<MeetingResponse>())!;

		Assert.False(created.IsTeamMeeting);
	}

	[Fact]
	public async Task Meeting_de_groupe_invalide_refuse()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Employees.Find(team.Jane.Id)!.IsActive = false);
		var sam = await api.LoginAsync("sam@test.local");

		var nobody = await sam.PostAsJsonAsync("/api/meetings", Group([]));
		var onlyMe = await sam.PostAsJsonAsync("/api/meetings", Group([team.Sam.Id]));
		var inactive = await sam.PostAsJsonAsync("/api/meetings", Group([team.Mark.Id, team.Jane.Id]));
		var unknown = await sam.PostAsJsonAsync("/api/meetings", Group([999]));
		var flexible = await sam.PostAsJsonAsync("/api/meetings", Group([team.Mark.Id], isFlexible: true));

		Assert.All([nobody, onlyMe, inactive, unknown, flexible], r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
		api.WithDb(db =>
		{
			Assert.Empty(db.Meetings);
			Assert.Empty(db.Notifications);
		});
	}

	[Fact]
	public async Task Chacun_change_sa_couleur_sans_toucher_celle_des_autres()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		var created = (await (await sam.PostAsJsonAsync("/api/meetings", Group([team.Mark.Id], color: "pink"))).Content.ReadFromJsonAsync<MeetingResponse>())!;

		// Un participant (pas l'organisateur) choisit sa couleur
		var markRes = await mark.PutAsJsonAsync($"/api/meetings/{created.Id}/color", new { color = "orange" });
		// L'organisateur revient à la couleur par défaut
		var samRes = await sam.PutAsJsonAsync($"/api/meetings/{created.Id}/color", new { color = (string?)null });

		Assert.Equal(HttpStatusCode.NoContent, markRes.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, samRes.StatusCode);
		api.WithDb(db =>
		{
			var rows = db.MeetingParticipants.Where(p => p.MeetingId == created.Id).ToList();
			Assert.Equal("orange", rows.Single(p => p.EmployeeId == team.Mark.Id).Color);
			Assert.Null(rows.Single(p => p.EmployeeId == team.Sam.Id).Color);
		});
	}

	[Fact]
	public async Task Changer_la_couleur_non_participant_inconnu_ou_hors_palette_refuse()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var sam = await api.LoginAsync("sam@test.local");
		var jane = await api.LoginAsync("jane@test.local");
		var created = (await (await sam.PostAsJsonAsync("/api/meetings", Group([team.Mark.Id], color: "pink"))).Content.ReadFromJsonAsync<MeetingResponse>())!;

		var notParticipant = await jane.PutAsJsonAsync($"/api/meetings/{created.Id}/color", new { color = "orange" });
		var unknownMeeting = await sam.PutAsJsonAsync("/api/meetings/999/color", new { color = "orange" });
		var unknownColor = await sam.PutAsJsonAsync($"/api/meetings/{created.Id}/color", new { color = "red" });

		Assert.Equal(HttpStatusCode.NotFound, notParticipant.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, unknownMeeting.StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, unknownColor.StatusCode);
		api.WithDb(db => Assert.Equal("pink", db.MeetingParticipants.Single(p => p.EmployeeId == team.Sam.Id).Color));
	}

	[Fact]
	public async Task Un_participant_en_conge_approuve_bloque_sans_reveler_le_type()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.LeaveRequests.Add(Leave(team.Jane, day: 15, LeaveStatus.Approuve, LeaveType.Maladie)));
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id, team.Mark.Id]));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Equal("Jane Test est en congé ce jour-là.", await ErrorMessage(res));
		api.WithDb(db => Assert.Empty(db.Meetings));
	}

	[Fact]
	public async Task Un_conge_en_attente_ne_bloque_pas()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.LeaveRequests.Add(Leave(team.Jane, day: 15, LeaveStatus.EnAttente)));
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id]));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	[Fact]
	public async Task Plusieurs_participants_en_conge_sont_tous_nommes()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.LeaveRequests.AddRange(Leave(team.Jane, day: 15), Leave(team.Mark, day: 14, endDay: 16)));
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id, team.Mark.Id]));
		var message = await ErrorMessage(res);

		Assert.Contains("Jane Test", message);
		Assert.Contains("Mark Test", message);
		Assert.EndsWith(" sont en congé ce jour-là.", message);
	}

	[Fact]
	public async Task L_organisateur_en_conge_est_bloque_meme_pour_un_bloc_personnel()
	{
		using var api = new ApiFactory().Start();
		api.WithDb(db => db.LeaveRequests.Add(Leave(api.Team.Sam, day: 15)));
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Block());

		Assert.Equal("Tu es en congé ce jour-là.", await ErrorMessage(res));
	}

	[Fact]
	public async Task Le_conge_se_compare_dans_le_fuseau_du_participant()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Jane.Id)!.SiteId = paris.Id;
			db.LeaveRequests.Add(Leave(team.Jane, day: 16)); // congé le 16, à Paris
		});
		var sam = await api.LoginAsync("sam@test.local");

		// Le 15 à 18 h 30 à Montréal = le 16 à 0 h 30 à Paris
		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id], start: "2026-10-15T22:30:00Z", end: "2026-10-15T23:00:00Z"));

		Assert.Equal("Jane Test est en congé ce jour-là.", await ErrorMessage(res));
	}

	[Fact]
	public async Task Un_bloc_qui_finit_a_minuit_la_veille_d_un_conge_passe()
	{
		using var api = new ApiFactory().Start();
		api.WithDb(db => db.LeaveRequests.Add(Leave(api.Team.Sam, day: 16)));
		var sam = await api.LoginAsync("sam@test.local");

		// 23 h → minuit le 15 à Montréal
		var res = await sam.PostAsJsonAsync("/api/meetings", Block(start: "2026-10-16T03:00:00Z", end: "2026-10-16T04:00:00Z"));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	[Fact]
	public async Task Sans_chevauchement_aucun_conflit_et_deux_evenements_qui_se_touchent_non_plus()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		// Jane : 9 h → 10 h à Montréal (13 h → 14 h UTC), juste avant le meeting de 10 h
		api.WithDb(db => db.Meetings.Add(Existing(team.Jane, "2026-10-15T13:00:00Z", "2026-10-15T14:00:00Z")));
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id]));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	[Fact]
	public async Task Un_chevauchement_donne_409_sans_rien_creer_puis_la_confirmation_cree_le_meeting()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Meetings.Add(Existing(team.Jane, "2026-10-15T14:30:00Z", "2026-10-15T15:30:00Z", title: "Secret")));
		var sam = await api.LoginAsync("sam@test.local");

		var first = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id, team.Mark.Id]));
		var body = await first.Content.ReadAsStringAsync();

		Assert.Equal(HttpStatusCode.Conflict, first.StatusCode);
		Assert.DoesNotContain("Secret", body); // R17 : jamais le titre
		var conflict = System.Text.Json.JsonSerializer.Deserialize<MeetingConflictResponse>(body, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
		Assert.Equal("Conflit d'horaire pour 1 personne.", conflict.Message);
		var jane = Assert.Single(conflict.Conflicts);
		Assert.Equal((team.Jane.Id, "Jane", "Test", false), (jane.EmployeeId, jane.FirstName, jane.LastName, jane.WithTeamMeeting));
		api.WithDb(db => Assert.Single(db.Meetings)); // seulement l'existant

		var confirmed = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id, team.Mark.Id], confirmConflicts: true));

		Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
		api.WithDb(db => Assert.Equal(2, db.Meetings.Count()));
	}

	[Fact]
	public async Task L_organisateur_peut_etre_lui_meme_en_conflit()
	{
		using var api = new ApiFactory().Start();
		api.WithDb(db => db.Meetings.Add(Existing(api.Team.Sam, "2026-10-15T14:00:00Z", "2026-10-15T15:00:00Z")));
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Block());
		var conflict = (await res.Content.ReadFromJsonAsync<MeetingConflictResponse>())!;

		Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
		Assert.Equal(api.Team.Sam.Id, Assert.Single(conflict.Conflicts).EmployeeId);
	}

	[Fact]
	public async Task Deux_conflits_pour_la_meme_personne_une_seule_ligne_marquee_d_equipe()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Meetings.AddRange(
			Existing(team.Jane, "2026-10-15T14:00:00Z", "2026-10-15T14:30:00Z"),
			Existing(team.Jane, "2026-10-15T14:30:00Z", "2026-10-15T15:00:00Z", isTeamMeeting: true)));
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id]));
		var conflict = (await res.Content.ReadFromJsonAsync<MeetingConflictResponse>())!;

		var jane = Assert.Single(conflict.Conflicts);
		Assert.True(jane.WithTeamMeeting);
	}

	[Fact]
	public async Task Un_meeting_d_equipe_passe_sur_un_bloc_souple_sans_conflit()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Meetings.Add(Existing(team.Sam, "2026-10-15T14:00:00Z", "2026-10-15T16:00:00Z", kind: MeetingKind.PersonalBlock, isFlexible: true)));
		var mark = await api.LoginAsync("mark@test.local"); // manager : meeting d'équipe

		var res = await mark.PostAsJsonAsync("/api/meetings", Group([team.Sam.Id]));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	[Fact]
	public async Task Un_meeting_ordinaire_sur_un_bloc_souple_est_un_conflit()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Meetings.Add(Existing(team.Sam, "2026-10-15T14:00:00Z", "2026-10-15T16:00:00Z", kind: MeetingKind.PersonalBlock, isFlexible: true)));
		var jane = await api.LoginAsync("jane@test.local"); // ni manager ni Admin

		var res = await jane.PostAsJsonAsync("/api/meetings", Group([team.Sam.Id]));

		Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
	}

	[Fact]
	public async Task Un_bloc_ferme_reste_un_conflit_meme_pour_un_meeting_d_equipe()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Meetings.Add(Existing(team.Sam, "2026-10-15T14:00:00Z", "2026-10-15T16:00:00Z", kind: MeetingKind.PersonalBlock)));
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync("/api/meetings", Group([team.Sam.Id]));

		Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
	}

	[Theory]
	[InlineData("2026-10-15T20:00:00Z", "2026-10-15T21:00:00Z", true)]   // 16 h → 17 h : finit pile à la fin
	[InlineData("2026-10-15T12:00:00Z", "2026-10-15T13:00:00Z", true)]   // 8 h → 9 h : commence pile au début
	[InlineData("2026-10-15T20:30:00Z", "2026-10-15T21:30:00Z", false)]  // 16 h 30 → 17 h 30
	[InlineData("2026-10-15T11:30:00Z", "2026-10-15T12:30:00Z", false)]  // 7 h 30 → 8 h 30
	public async Task Le_meeting_doit_tenir_dans_les_heures_du_participant(string start, string end, bool fits)
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.WorkSchedules.Add(WeekdaySchedule(team.Jane))); // 8 h → 17 h
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id], start: start, end: end));

		if (fits)
			Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		else
		{
			var conflict = (await res.Content.ReadFromJsonAsync<MeetingConflictResponse>())!;
			Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
			Assert.Equal("1 personne est hors de son horaire.", conflict.Message);
			Assert.Empty(conflict.Conflicts);
			var jane = Assert.Single(conflict.OutsideHours);
			Assert.Equal((team.Jane.Id, "Jane", "Test", OutsideHoursReason.OutsideHours), (jane.EmployeeId, jane.FirstName, jane.LastName, jane.Reason));
		}
	}

	[Fact]
	public async Task Un_jour_non_travaille_est_signale()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.WorkSchedules.Add(WeekdaySchedule(team.Jane)));
		var sam = await api.LoginAsync("sam@test.local");

		// Samedi 17 octobre, 10 h → 11 h
		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id], start: "2026-10-17T14:00:00Z", end: "2026-10-17T15:00:00Z"));
		var conflict = (await res.Content.ReadFromJsonAsync<MeetingConflictResponse>())!;

		Assert.Equal(OutsideHoursReason.NotWorking, Assert.Single(conflict.OutsideHours).Reason);
	}

	[Fact]
	public async Task Un_ferie_du_site_est_signale()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(team.Jane));
			db.Holidays.Add(new Holiday { SiteId = team.Jane.SiteId, Date = new DateOnly(2026, 10, 15), Name = "Férié test" });
		});
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id]));
		var conflict = (await res.Content.ReadFromJsonAsync<MeetingConflictResponse>())!;

		Assert.Equal(OutsideHoursReason.Holiday, Assert.Single(conflict.OutsideHours).Reason);
	}

	[Fact]
	public async Task Un_ajustement_approuve_remplace_les_heures_de_l_horaire()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(team.Jane)); // 8 h → 17 h
			db.ScheduleAdjustments.Add(new ScheduleAdjustment
			{
				EmployeeId = team.Jane.Id,
				Date = new DateOnly(2026, 10, 15),
				StartTime = new TimeOnly(8, 0),
				EndTime = new TimeOnly(18, 0),
				Status = LeaveStatus.Approuve,
				CreatedById = team.Jane.Id,
			});
		});
		var sam = await api.LoginAsync("sam@test.local");

		// 16 h 30 → 17 h 30 : dépasse l'horaire, mais pas l'ajustement
		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id], start: "2026-10-15T20:30:00Z", end: "2026-10-15T21:30:00Z"));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	[Theory]
	[InlineData(LeaveStatus.Approuve, false)]
	[InlineData(LeaveStatus.EnAttente, true)]  // pas encore effective
	public async Task Une_absence_approuvee_qui_chevauche_est_signalee(LeaveStatus status, bool created)
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(team.Jane));
			db.Absences.Add(Absence(team.Jane, start: new(10, 30), end: new(11, 30), status)); // meeting : 10 h → 11 h
		});
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id]));

		if (created)
			Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		else
		{
			var conflict = (await res.Content.ReadFromJsonAsync<MeetingConflictResponse>())!;
			Assert.Equal(OutsideHoursReason.Absence, Assert.Single(conflict.OutsideHours).Reason);
		}
	}

	[Fact]
	public async Task Une_absence_qui_touche_le_meeting_ne_le_chevauche_pas()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(team.Jane));
			db.Absences.Add(Absence(team.Jane, start: new(11, 0), end: new(12, 0))); // meeting : 10 h → 11 h
		});
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id]));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	[Fact]
	public async Task Un_je_pars_sans_retour_bloque_jusqu_a_la_fin_de_la_journee()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(WeekdaySchedule(team.Jane)); // 8 h → 17 h
			db.Absences.Add(new Absence
			{
				EmployeeId = team.Jane.Id,
				Date = new DateOnly(2026, 10, 15),
				StartedAt = new TimeOnly(9, 0), // « Je pars » à 9 h, aucune heure de retour
				Status = LeaveStatus.Approuve,
				CreatedById = team.Jane.Id,
			});
		});
		var sam = await api.LoginAsync("sam@test.local");

		// 15 h → 16 h
		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id], start: "2026-10-15T19:00:00Z", end: "2026-10-15T20:00:00Z"));
		var conflict = (await res.Content.ReadFromJsonAsync<MeetingConflictResponse>())!;

		Assert.Equal(OutsideHoursReason.Absence, Assert.Single(conflict.OutsideHours).Reason);
	}

	[Fact]
	public async Task Les_heures_se_comparent_dans_le_fuseau_du_participant()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Jane.Id)!.SiteId = paris.Id;
			db.WorkSchedules.Add(WeekdaySchedule(team.Jane)); // 8 h → 17 h à Paris
		});
		var sam = await api.LoginAsync("sam@test.local");

		// 10 h → 11 h à Montréal = 16 h → 17 h à Paris : OK
		var fits = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id]));
		// 11 h → midi à Montréal = 17 h → 18 h à Paris : dépasse
		var late = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id], start: "2026-10-15T15:00:00Z", end: "2026-10-15T16:00:00Z"));
		var conflict = (await late.Content.ReadFromJsonAsync<MeetingConflictResponse>())!;

		Assert.Equal(HttpStatusCode.Created, fits.StatusCode);
		Assert.Equal(OutsideHoursReason.OutsideHours, Assert.Single(conflict.OutsideHours).Reason);
	}

	[Fact]
	public async Task Un_meeting_sur_deux_jours_pour_le_participant_est_hors_horaire()
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

		// 17 h → 19 h à Montréal = 23 h → 1 h à Paris
		var res = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id], start: "2026-10-15T21:00:00Z", end: "2026-10-15T23:00:00Z"));
		var conflict = (await res.Content.ReadFromJsonAsync<MeetingConflictResponse>())!;

		Assert.Equal(OutsideHoursReason.OutsideHours, Assert.Single(conflict.OutsideHours).Reason);
	}

	[Fact]
	public async Task Ni_l_organisateur_ni_un_horaire_non_defini_ne_sont_signales()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.WorkSchedules.Add(WeekdaySchedule(team.Sam))); // Jane : aucun horaire
		var sam = await api.LoginAsync("sam@test.local");

		// Samedi : non travaillé pour Sam (organisateur), inconnu pour Jane
		var meeting = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id], start: "2026-10-17T14:00:00Z", end: "2026-10-17T15:00:00Z"));
		var block = await sam.PostAsJsonAsync("/api/meetings", Block(start: "2026-10-18T14:00:00Z", end: "2026-10-18T15:00:00Z"));

		Assert.Equal(HttpStatusCode.Created, meeting.StatusCode);
		Assert.Equal(HttpStatusCode.Created, block.StatusCode);
	}

	[Fact]
	public async Task Conflits_et_hors_horaire_dans_le_meme_409_puis_la_confirmation_cree_le_meeting()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.AddRange(WeekdaySchedule(team.Jane), WeekdaySchedule(team.Mark));
			db.Meetings.Add(Existing(team.Mark, "2026-10-17T14:00:00Z", "2026-10-17T15:00:00Z"));
		});
		var sam = await api.LoginAsync("sam@test.local");

		// Samedi, 10 h → 11 h
		var first = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id, team.Mark.Id], start: "2026-10-17T14:00:00Z", end: "2026-10-17T15:00:00Z"));
		var conflict = (await first.Content.ReadFromJsonAsync<MeetingConflictResponse>())!;

		Assert.Equal(HttpStatusCode.Conflict, first.StatusCode);
		Assert.Equal("Conflit d'horaire pour 1 personne. 2 personnes sont hors de leur horaire.", conflict.Message);
		Assert.Equal(team.Mark.Id, Assert.Single(conflict.Conflicts).EmployeeId);
		Assert.Equal(new[] { team.Mark.Id, team.Jane.Id }.Order(), conflict.OutsideHours.Select(o => o.EmployeeId).Order());

		var confirmed = await sam.PostAsJsonAsync("/api/meetings", Group([team.Jane.Id, team.Mark.Id], start: "2026-10-17T14:00:00Z", end: "2026-10-17T15:00:00Z", confirmConflicts: true));

		Assert.Equal(HttpStatusCode.Created, confirmed.StatusCode);
	}

	[Fact]
	public async Task Connexion_requise()
	{
		using var api = new ApiFactory().Start();

		var res = await api.CreateClient().PostAsJsonAsync("/api/meetings", Block());

		Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	/// <summary>Un événement déjà en base, dont la personne est l'organisatrice et la seule participante.</summary>
	private static Meeting Existing(
		Employee owner,
		string start,
		string end,
		string title = "Existant",
		MeetingKind kind = MeetingKind.Meeting,
		bool isTeamMeeting = false,
		bool isFlexible = false) => new()
	{
		Kind = kind,
		Title = title,
		StartAt = DateTime.Parse(start, null, System.Globalization.DateTimeStyles.AdjustToUniversal),
		EndAt = DateTime.Parse(end, null, System.Globalization.DateTimeStyles.AdjustToUniversal),
		OrganizerId = owner.Id,
		IsTeamMeeting = isTeamMeeting,
		IsFlexible = isFlexible,
		Participants = [new MeetingParticipant { EmployeeId = owner.Id }],
	};

	/// <summary>Un congé en octobre 2026, approuvé par défaut.</summary>
	private static LeaveRequest Leave(
		Employee employee,
		int day,
		LeaveStatus status = LeaveStatus.Approuve,
		LeaveType type = LeaveType.Conge,
		int? endDay = null) => new()
	{
		EmployeeId = employee.Id,
		StartDate = new DateTime(2026, 10, day),
		EndDate = new DateTime(2026, 10, endDay ?? day),
		Type = type,
		Status = status,
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

	/// <summary>Une absence prévue le jeudi 15 octobre 2026, approuvée par défaut.</summary>
	private static Absence Absence(Employee employee, TimeOnly start, TimeOnly end, LeaveStatus status = LeaveStatus.Approuve) => new()
	{
		EmployeeId = employee.Id,
		Date = new DateOnly(2026, 10, 15),
		PlannedStart = start,
		PlannedEnd = end,
		Status = status,
		CreatedById = employee.Id,
	};

	private static async Task<string> ErrorMessage(HttpResponseMessage res) =>
		(await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("message").GetString()!;

	/// <summary>Un meeting de groupe, demain de 10 h à 11 h à Montréal (14 h → 15 h UTC).</summary>
	private static object Group(
		int[] participantIds,
		string start = "2026-10-15T14:00:00Z",
		string end = "2026-10-15T15:00:00Z",
		bool isFlexible = false,
		string? color = null,
		bool confirmConflicts = false) => new
	{
		kind = MeetingKind.Meeting,
		title = "Revue",
		startAt = start,
		endAt = end,
		isFlexible,
		color,
		participantIds,
		confirmConflicts,
	};

	/// <summary>Un bloc personnel, demain de 10 h à midi à Montréal (14 h → 16 h UTC).</summary>
	private static object Block(
		string title = "Projet X",
		string? description = null,
		string start = "2026-10-15T14:00:00Z",
		string end = "2026-10-15T16:00:00Z",
		bool isFlexible = false,
		string? color = null) => new
	{
		kind = MeetingKind.PersonalBlock,
		title,
		description,
		startAt = start,
		endAt = end,
		isFlexible,
		color,
	};
}
