using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Notifications de l'appelant (phase 3, étape 11) : liste page par page, nombre de non lues, marquer lu.
/// « Maintenant » = mercredi 14 octobre 2026, 16 h UTC.
/// </summary>
public class NotificationsApiTests
{
	[Fact]
	public async Task La_liste_ne_contient_que_les_siennes_des_plus_recentes_aux_plus_anciennes()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Notifications.Add(Notification(team.Sam, "Première"));
			db.Notifications.Add(Notification(team.Jane, "Pour Jane"));
			var read = Notification(team.Sam, "Deuxième", "/calendar?vue=jour&date=2026-10-15");
			read.ReadAt = new DateTime(2026, 10, 14, 15, 0, 0, DateTimeKind.Utc);
			db.Notifications.Add(read);
		});
		var sam = await api.LoginAsync("sam@test.local");

		var list = (await sam.GetFromJsonAsync<NotificationListResponse>("/api/notifications"))!;

		Assert.False(list.HasMore);
		Assert.Equal(["Deuxième", "Première"], list.Items.Select(n => n.Message));

		var latest = list.Items[0];
		Assert.True(latest.Id > 0);
		Assert.Equal(NotificationType.MeetingInvitation, latest.Type);
		Assert.Equal("/calendar?vue=jour&date=2026-10-15", latest.Link);
		Assert.Equal(new DateTime(2026, 10, 14, 15, 0, 0, DateTimeKind.Utc), latest.ReadAt);
		Assert.Null(list.Items[1].ReadAt);
	}

	[Fact]
	public async Task La_liste_se_lit_page_par_page_avec_beforeId()
	{
		using var api = new ApiFactory().Start();
		api.WithDb(db =>
		{
			foreach (var i in Enumerable.Range(1, 5))
				db.Notifications.Add(Notification(api.Team.Sam, $"N{i}"));
		});
		var sam = await api.LoginAsync("sam@test.local");

		var page1 = (await sam.GetFromJsonAsync<NotificationListResponse>("/api/notifications?limit=2"))!;
		var page2 = (await sam.GetFromJsonAsync<NotificationListResponse>($"/api/notifications?limit=2&beforeId={page1.Items[^1].Id}"))!;
		var page3 = (await sam.GetFromJsonAsync<NotificationListResponse>($"/api/notifications?limit=2&beforeId={page2.Items[^1].Id}"))!;

		Assert.Equal(["N5", "N4"], page1.Items.Select(n => n.Message));
		Assert.True(page1.HasMore);
		Assert.Equal(["N3", "N2"], page2.Items.Select(n => n.Message));
		Assert.True(page2.HasMore);
		Assert.Equal(["N1"], page3.Items.Select(n => n.Message));
		Assert.False(page3.HasMore);
	}

	[Fact]
	public async Task Par_defaut_une_page_contient_20_notifications()
	{
		using var api = new ApiFactory().Start();
		api.WithDb(db =>
		{
			foreach (var i in Enumerable.Range(1, 21))
				db.Notifications.Add(Notification(api.Team.Sam, $"N{i}"));
		});
		var sam = await api.LoginAsync("sam@test.local");

		var list = (await sam.GetFromJsonAsync<NotificationListResponse>("/api/notifications"))!;

		Assert.Equal(20, list.Items.Count);
		Assert.True(list.HasMore);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(51)]
	public async Task Une_taille_de_page_hors_limites_est_refusee(int limit)
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.GetAsync($"/api/notifications?limit={limit}");

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Le_nombre_de_non_lues_ne_compte_que_les_siennes()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Notifications.Add(Notification(team.Sam, "A"));
			db.Notifications.Add(Notification(team.Sam, "B"));
			var read = Notification(team.Sam, "Lue");
			read.ReadAt = DateTime.UtcNow;
			db.Notifications.Add(read);
			db.Notifications.Add(Notification(team.Jane, "Pour Jane"));
		});
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/notifications/unread-count");

		Assert.Equal(2, res.GetProperty("count").GetInt32());
	}

	[Fact]
	public async Task Marquer_lu_garde_la_premiere_date_de_lecture()
	{
		using var api = new ApiFactory().Start();
		var id = Seed(api, Notification(api.Team.Sam, "A"));
		var sam = await api.LoginAsync("sam@test.local");

		var first = await sam.PostAsync($"/api/notifications/{id}/read", null);
		api.WithDb(db => db.Notifications.Find(id)!.ReadAt = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
		var again = await sam.PostAsync($"/api/notifications/{id}/read", null);

		Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
		api.WithDb(db => Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), db.Notifications.Find(id)!.ReadAt));
	}

	[Fact]
	public async Task Marquer_lu_note_l_heure_actuelle()
	{
		using var api = new ApiFactory().Start();
		var id = Seed(api, Notification(api.Team.Sam, "A"));
		var sam = await api.LoginAsync("sam@test.local");

		await sam.PostAsync($"/api/notifications/{id}/read", null);

		api.WithDb(db => Assert.Equal(ApiFactory.Now.UtcDateTime, db.Notifications.Find(id)!.ReadAt));
	}

	[Fact]
	public async Task La_notification_d_un_autre_ou_inconnue_donne_404_sans_rien_changer()
	{
		using var api = new ApiFactory().Start();
		var janeId = Seed(api, Notification(api.Team.Jane, "Pour Jane"));
		var sam = await api.LoginAsync("sam@test.local");

		var other = await sam.PostAsync($"/api/notifications/{janeId}/read", null);
		var unknown = await sam.PostAsync("/api/notifications/9999/read", null);

		Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
		api.WithDb(db => Assert.Null(db.Notifications.Find(janeId)!.ReadAt));
	}

	[Fact]
	public async Task Tout_marquer_lu_ne_touche_que_les_siennes_non_lues()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var earlier = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
		var alreadyRead = Notification(team.Sam, "Déjà lue");
		alreadyRead.ReadAt = earlier;
		var readId = Seed(api, alreadyRead);
		Seed(api, Notification(team.Sam, "A"));
		Seed(api, Notification(team.Sam, "B"));
		var janeId = Seed(api, Notification(team.Jane, "Pour Jane"));
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsync("/api/notifications/read-all", null);
		var again = await sam.PostAsync("/api/notifications/read-all", null); // plus rien à marquer

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
		api.WithDb(db =>
		{
			Assert.Equal(earlier, db.Notifications.Find(readId)!.ReadAt);
			Assert.Equal(2, db.Notifications.Count(n => n.RecipientId == team.Sam.Id && n.ReadAt == ApiFactory.Now.UtcDateTime));
			Assert.Null(db.Notifications.Find(janeId)!.ReadAt);
		});
	}

	[Theory]
	[InlineData("GET", "/api/notifications")]
	[InlineData("GET", "/api/notifications/unread-count")]
	[InlineData("POST", "/api/notifications/1/read")]
	[InlineData("POST", "/api/notifications/read-all")]
	public async Task La_connexion_est_requise(string method, string url)
	{
		using var api = new ApiFactory().Start();

		var res = await api.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

		Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	private static Notification Notification(Employee recipient, string message, string? link = null) => new()
	{
		RecipientId = recipient.Id,
		Type = NotificationType.MeetingInvitation,
		Message = message,
		Link = link,
	};

	private static int Seed(ApiFactory api, Notification notification)
	{
		api.WithDb(db =>
		{
			db.Notifications.Add(notification);
			db.SaveChanges();
		});
		return notification.Id;
	}
}
