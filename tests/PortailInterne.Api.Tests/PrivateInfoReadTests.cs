using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Phase 3.1, étape 5 : GET /api/employees/{id}/private (R28).
/// Lui-même, un Admin de son site, l'Admin global : tout. Son manager direct : contacts d'urgence seulement.
/// Tout autre : 404. Employé inactif : les Admins seulement.
/// Équipe de test (Montréal) : Alex Admin, Mark manager de Sam, Jane dans aucune équipe, Gaby Admin global.
/// </summary>
public class PrivateInfoReadTests
{
	[Theory]
	[InlineData("sam@test.local")]   // lui-même
	[InlineData("alex@test.local")]  // Admin de son site
	[InlineData("gaby@test.local")]  // Admin global
	public async Task Lui_meme_et_les_admins_voient_tout(string email)
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => FillSam(db, team.Sam.Id));
		var client = await api.LoginAsync(email);

		var info = (await client.GetFromJsonAsync<PrivateInfoResponse>($"/api/employees/{team.Sam.Id}/private"))!;

		Assert.NotNull(info.Details);
		Assert.Equal("+15145559876", info.Details.PersonalMobilePhone);
		Assert.Equal("sam.perso@example.com", info.Details.PersonalEmail);
		Assert.Equal("123 rue des Érables", info.Details.AddressLine1);
		Assert.Equal("app. 4", info.Details.AddressLine2);
		Assert.Equal("Montréal", info.Details.City);
		Assert.Equal("QC", info.Details.Region);
		Assert.Equal("H2X 1Y4", info.Details.PostalCode);
		Assert.Equal("CA", info.Details.Country);
		Assert.Equal(5, info.Details.BirthMonth);
		Assert.Equal(12, info.Details.BirthDay);
		Assert.True(info.Details.ShowBirthday);
		Assert.Equal(["Conjoint", "Mère"], info.EmergencyContacts.Select(c => c.Relationship));
	}

	[Fact]
	public async Task Le_manager_direct_voit_seulement_les_contacts_d_urgence()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => FillSam(db, team.Sam.Id));
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.GetAsync($"/api/employees/{team.Sam.Id}/private");
		var body = await res.Content.ReadAsStringAsync();
		var json = JsonDocument.Parse(body).RootElement;

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
		Assert.Equal(JsonValueKind.Null, json.GetProperty("details").ValueKind);
		Assert.Equal(2, json.GetProperty("emergencyContacts").GetArrayLength());
		foreach (var secret in new[] { "5559876", "sam.perso", "Érables", "H2X", "birthMonth", "city" })
			Assert.DoesNotContain(secret, body);
	}

	[Fact]
	public async Task Les_contacts_sont_dans_l_ordre_et_complets()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => FillSam(db, team.Sam.Id));
		var mark = await api.LoginAsync("mark@test.local");

		var info = (await mark.GetFromJsonAsync<PrivateInfoResponse>($"/api/employees/{team.Sam.Id}/private"))!;

		Assert.Collection(info.EmergencyContacts,
			c =>
			{
				Assert.Equal(1, c.SortOrder);
				Assert.Equal("Robin Test", c.Name);
				Assert.Equal("Conjoint", c.Relationship);
				Assert.Equal("+15145550001", c.PhoneNumber);
			},
			c =>
			{
				Assert.Equal(2, c.SortOrder);
				Assert.Equal("Claire Test", c.Name);
			});
	}

	[Fact]
	public async Task Les_autres_recoivent_404()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			FillSam(db, team.Sam.Id);
			db.Employees.Find(team.Mark.Id)!.ManagerId = team.Jane.Id; // Jane : manager de Mark, donc manager indirect de Sam
		});
		var jane = await api.LoginAsync("jane@test.local");
		var mark = await api.LoginAsync("mark@test.local");

		Assert.Equal(HttpStatusCode.NotFound, (await jane.GetAsync($"/api/employees/{team.Sam.Id}/private")).StatusCode);   // manager indirect
		Assert.Equal(HttpStatusCode.NotFound, (await mark.GetAsync($"/api/employees/{team.Jane.Id}/private")).StatusCode);  // collègue
		Assert.Equal(HttpStatusCode.NotFound, (await mark.GetAsync($"/api/employees/{team.Alex.Id}/private")).StatusCode);  // un Admin n'est pas son manager
		Assert.Equal(HttpStatusCode.NotFound, (await mark.GetAsync("/api/employees/99999/private")).StatusCode);            // inexistant
	}

	[Fact]
	public async Task Un_admin_d_un_autre_site_recoit_404()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			FillSam(db, team.Sam.Id);
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris", CountryCode = "FR" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Alex.Id)!.SiteId = paris.Id; // Alex : Admin de Paris
		});
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.GetAsync($"/api/employees/{team.Sam.Id}/private");

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	[Fact]
	public async Task Sans_coordonnees_les_details_sont_vides_mais_presents()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");

		var mine = (await sam.GetFromJsonAsync<PrivateInfoResponse>($"/api/employees/{team.Sam.Id}/private"))!;
		var asManager = (await mark.GetFromJsonAsync<PrivateInfoResponse>($"/api/employees/{team.Sam.Id}/private"))!;

		Assert.NotNull(mine.Details); // vide, mais Sam a le droit de le voir
		Assert.Null(mine.Details.City);
		Assert.Null(mine.Details.BirthMonth);
		Assert.False(mine.Details.ShowBirthday);
		Assert.Empty(mine.EmergencyContacts);
		Assert.Null(asManager.Details);
		Assert.Empty(asManager.EmergencyContacts);
	}

	[Fact]
	public async Task Un_employe_inactif_seulement_pour_les_admins()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => FillSam(db, team.Sam.Id));
		var mark = await api.LoginAsync("mark@test.local");
		var alex = await api.LoginAsync("alex@test.local");
		api.WithDb(db => db.Employees.Find(team.Sam.Id)!.IsActive = false);

		var asManager = await mark.GetAsync($"/api/employees/{team.Sam.Id}/private");
		var asAdmin = await alex.GetFromJsonAsync<PrivateInfoResponse>($"/api/employees/{team.Sam.Id}/private");

		Assert.Equal(HttpStatusCode.NotFound, asManager.StatusCode);
		Assert.Equal("Montréal", asAdmin!.Details!.City);
	}

	[Fact]
	public async Task Un_admin_qui_est_aussi_le_manager_direct_voit_tout()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			FillSam(db, team.Sam.Id);
			db.Employees.Find(team.Sam.Id)!.ManagerId = team.Alex.Id;
		});
		var alex = await api.LoginAsync("alex@test.local");

		var info = (await alex.GetFromJsonAsync<PrivateInfoResponse>($"/api/employees/{team.Sam.Id}/private"))!;

		Assert.Equal("Montréal", info.Details!.City);
	}

	private static void FillSam(AppDbContext db, int samId)
	{
		db.EmployeePrivateInfos.Add(new EmployeePrivateInfo
		{
			EmployeeId = samId,
			PersonalMobilePhone = "+15145559876",
			PersonalEmail = "sam.perso@example.com",
			AddressLine1 = "123 rue des Érables",
			AddressLine2 = "app. 4",
			City = "Montréal",
			Region = "QC",
			PostalCode = "H2X 1Y4",
			Country = "CA",
			BirthMonth = 5,
			BirthDay = 12,
			ShowBirthday = true,
			UpdatedAt = DateTime.UtcNow,
		});
		// Ajoutés dans le désordre : la réponse doit les trier.
		db.EmergencyContacts.Add(new EmergencyContact { EmployeeId = samId, Name = "Claire Test", Relationship = "Mère", PhoneNumber = "+15145550002", SortOrder = 2 });
		db.EmergencyContacts.Add(new EmergencyContact { EmployeeId = samId, Name = "Robin Test", Relationship = "Conjoint", PhoneNumber = "+15145550001", SortOrder = 1 });
	}
}
