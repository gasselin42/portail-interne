using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Phase 3.1, étape 7 : création et modification d'un employé par l'Admin.
/// Local et date d'embauche (obligatoire, peut être dans le futur, entre 1950 et dans moins d'un an) ;
/// coordonnées personnelles facultatives à la création, validées comme PUT /profile, dans le même enregistrement.
/// Qui modifie quoi : local et date d'embauche = Admin du site seulement.
/// Horloge des tests : 2026-10-14 16:00 UTC, soit le 14 octobre à Montréal.
/// </summary>
public class EmployeeCreationProfileTests
{
	// ─── Local et date d'embauche ──────────────────────────────────────────

	[Fact]
	public async Task La_creation_enregistre_le_local_et_la_date_d_embauche()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PostAsync("/api/admin/employees", Form("Nora", ("officeLocation", "  B-204 "), ("hireDate", "2026-12-01")));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		var card = await CardOf(alex, res);
		Assert.Equal("B-204", card.GetProperty("officeLocation").GetString());
		Assert.Equal("2026-12-01", card.GetProperty("hireDate").GetString()); // dans le futur : permis
	}

	[Fact]
	public async Task Un_local_vide_devient_null()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PostAsync("/api/admin/employees", Form("Nora", ("officeLocation", "   "), ("hireDate", "2024-03-03")));

		var card = await CardOf(alex, res);
		Assert.Equal(JsonValueKind.Null, card.GetProperty("officeLocation").ValueKind);
	}

	[Fact]
	public async Task La_date_d_embauche_est_obligatoire()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var created = await alex.PostAsync("/api/admin/employees", Form("Nora"));
		var updated = await alex.PatchAsync($"/api/admin/employees/{api.Team.Sam.Id}", Form("Sam"));

		Assert.Equal(HttpStatusCode.BadRequest, created.StatusCode);
		Assert.Contains("La date d'embauche est obligatoire.", await created.Content.ReadAsStringAsync());
		Assert.Equal(HttpStatusCode.BadRequest, updated.StatusCode);
		Assert.Contains("La date d'embauche est obligatoire.", await updated.Content.ReadAsStringAsync());
		Assert.False(EmailExists(api, "nora.form@test.local"));
	}

	[Theory]
	[InlineData("1950-01-01", HttpStatusCode.Created)]
	[InlineData("1949-12-31", HttpStatusCode.BadRequest)]
	[InlineData("2027-10-13", HttpStatusCode.Created)]    // dans un an moins un jour
	[InlineData("2027-10-14", HttpStatusCode.BadRequest)] // dans un an jour pour jour
	[InlineData("0202-03-03", HttpStatusCode.BadRequest)] // faute de frappe
	public async Task La_date_d_embauche_reste_dans_des_limites_raisonnables(string hireDate, HttpStatusCode expected)
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PostAsync("/api/admin/employees", Form("Nora", ("hireDate", hireDate)));

		Assert.Equal(expected, res.StatusCode);
		if (expected == HttpStatusCode.BadRequest)
			Assert.Equal("Date d'embauche invalide.", await Message(res));
	}

	[Fact]
	public async Task Les_limites_suivent_le_fuseau_du_site()
	{
		// 2026-10-15 02:00 UTC = encore le 14 octobre à Montréal : la limite reste le 14 octobre 2027.
		using var api = new ApiFactory(new DateTimeOffset(2026, 10, 15, 2, 0, 0, TimeSpan.Zero)).Start();
		var alex = await api.LoginAsync("alex@test.local");

		var ok = await alex.PostAsync("/api/admin/employees", Form("Nora", ("hireDate", "2027-10-13")));
		var refused = await alex.PostAsync("/api/admin/employees", Form("Omar", ("hireDate", "2027-10-14")));

		Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
	}

	[Fact]
	public async Task La_modification_change_le_local_et_la_date_d_embauche()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Employees.Find(team.Sam.Id)!.OfficeLocation = "A-100");
		var alex = await api.LoginAsync("alex@test.local");

		var moved = await alex.PatchAsync($"/api/admin/employees/{team.Sam.Id}", Form("Sam", ("officeLocation", " C-300 "), ("hireDate", "2020-06-15")));
		var movedCard = await alex.GetFromJsonAsync<JsonElement>($"/api/admin/employees/{team.Sam.Id}");
		var emptied = await alex.PatchAsync($"/api/admin/employees/{team.Sam.Id}", Form("Sam", ("hireDate", "2020-06-15")));
		var emptiedCard = await alex.GetFromJsonAsync<JsonElement>($"/api/admin/employees/{team.Sam.Id}");
		var invalid = await alex.PatchAsync($"/api/admin/employees/{team.Sam.Id}", Form("Sam", ("hireDate", "1900-01-01")));

		Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
		Assert.Equal("C-300", movedCard.GetProperty("officeLocation").GetString());
		Assert.Equal("2020-06-15", movedCard.GetProperty("hireDate").GetString());
		Assert.Equal(HttpStatusCode.NoContent, emptied.StatusCode);
		Assert.Equal(JsonValueKind.Null, emptiedCard.GetProperty("officeLocation").ValueKind);
		Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
		api.WithDb(db => Assert.Equal(new DateOnly(2020, 6, 15), db.Employees.Find(team.Sam.Id)!.HireDate));
	}

	[Fact]
	public async Task Un_local_trop_long_est_refuse()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PostAsync("/api/admin/employees", Form("Nora", ("officeLocation", new string('a', 101)), ("hireDate", "2024-03-03")));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Contains("100 caractères au maximum", await res.Content.ReadAsStringAsync());
	}

	// ─── Qui modifie quoi ──────────────────────────────────────────────────

	[Fact]
	public async Task L_employe_ne_change_ni_son_local_ni_sa_date_d_embauche()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var s = db.Employees.Find(team.Sam.Id)!;
			s.OfficeLocation = "A-100";
			s.HireDate = new DateOnly(2024, 3, 3);
		});
		var sam = await api.LoginAsync("sam@test.local");

		// PUT /profile ignore ces champs ; l'endpoint admin lui est interdit.
		var profile = await sam.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", new
		{
			officeLocation = "Z-999",
			hireDate = "2010-01-01",
			languages = Array.Empty<string>(),
			details = new { },
			emergencyContacts = Array.Empty<object>(),
		});
		var admin = await sam.PatchAsync($"/api/admin/employees/{team.Sam.Id}", Form("Sam", ("officeLocation", "Z-999"), ("hireDate", "2010-01-01")));

		Assert.Equal(HttpStatusCode.NoContent, profile.StatusCode);
		Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);
		api.WithDb(db =>
		{
			var s = db.Employees.Find(team.Sam.Id)!;
			Assert.Equal("A-100", s.OfficeLocation);
			Assert.Equal(new DateOnly(2024, 3, 3), s.HireDate);
		});
	}

	[Fact]
	public async Task L_admin_d_un_autre_site_ne_change_rien()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris", CountryCode = "FR" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Alex.Id)!.SiteId = paris.Id;
		});
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PatchAsync($"/api/admin/employees/{team.Sam.Id}", Form("Sam", ("officeLocation", "Z-999"), ("hireDate", "2010-01-01")));

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
		api.WithDb(db => Assert.Null(db.Employees.Find(team.Sam.Id)!.OfficeLocation));
	}

	// ─── Coordonnées personnelles à la création ────────────────────────────

	[Fact]
	public async Task Sans_profil_aucune_coordonnee_personnelle_n_est_creee()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PostAsync("/api/admin/employees", Form("Nora", ("hireDate", "2024-03-03")));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		var id = await EmployeeIdOf(res);
		api.WithDb(db =>
		{
			Assert.False(db.EmployeePrivateInfos.Any(p => p.EmployeeId == id));
			Assert.False(db.EmergencyContacts.Any(c => c.EmployeeId == id));
			Assert.False(db.EmployeeLanguages.Any(l => l.EmployeeId == id));
		});
	}

	[Fact]
	public async Task Le_profil_complet_est_enregistre_avec_l_employe()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PostAsync("/api/admin/employees", Form("Nora",
			("hireDate", "2024-03-03"),
			("profile.preferredName", " Nono "),
			("profile.languages", "FR"),
			("profile.languages", "es"),
			("profile.details.personalMobilePhone", "514 555-9876"),
			("profile.details.personalEmail", " nora.perso@example.com "),
			("profile.details.addressLine1", "123 rue des Érables"),
			("profile.details.city", "Montréal"),
			("profile.details.region", "QC"),
			("profile.details.postalCode", "h2x1y4"),
			("profile.details.country", "ca"),
			("profile.details.birthMonth", "5"),
			("profile.details.birthDay", "12"),
			("profile.details.showBirthday", "true"),
			("profile.emergencyContacts[0].name", "Robin Test"),
			("profile.emergencyContacts[0].relationship", "Conjoint"),
			("profile.emergencyContacts[0].phoneNumber", "514 555-0001"),
			("profile.emergencyContacts[1].name", "Claire Test"),
			("profile.emergencyContacts[1].relationship", "Mère"),
			("profile.emergencyContacts[1].phoneNumber", "514 555-0002")));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		var id = await EmployeeIdOf(res);
		api.WithDb(db =>
		{
			var nora = db.Employees.Include(e => e.Languages).Single(e => e.Id == id);
			Assert.Equal("Nono", nora.PreferredName);
			Assert.Equal(["es", "fr"], nora.Languages.Select(l => l.LanguageCode).Order());

			var info = db.EmployeePrivateInfos.Single(p => p.EmployeeId == id);
			Assert.Equal("+15145559876", info.PersonalMobilePhone);
			Assert.Equal("nora.perso@example.com", info.PersonalEmail);
			Assert.Equal("H2X 1Y4", info.PostalCode);
			Assert.Equal("CA", info.Country);
			Assert.Equal(5, info.BirthMonth);
			Assert.Equal(12, info.BirthDay);
			Assert.True(info.ShowBirthday);

			var contacts = db.EmergencyContacts.Where(c => c.EmployeeId == id).OrderBy(c => c.SortOrder).ToList();
			Assert.Equal(["Robin Test", "Claire Test"], contacts.Select(c => c.Name));
			Assert.Equal([1, 2], contacts.Select(c => c.SortOrder));
			Assert.Equal("+15145550002", contacts[1].PhoneNumber);
		});

		// L'endpoint de lecture les retrouve.
		var read = await alex.GetFromJsonAsync<JsonElement>($"/api/employees/{id}/private");
		Assert.Equal("Montréal", read.GetProperty("details").GetProperty("city").GetString());
		Assert.Equal(2, read.GetProperty("emergencyContacts").GetArrayLength());
	}

	[Theory]
	[InlineData("profile.details.city", "Montréal", "Adresse personnelle incomplète : adresse, pays manquant.")]
	[InlineData("profile.details.personalMobilePhone", "123", "Numéro de cellulaire personnel invalide pour ce pays.")]
	[InlineData("profile.details.birthMonth", "5", "Indique le mois et le jour, ou ni l'un ni l'autre.")]
	[InlineData("profile.languages", "klingon", "Langue inconnue : klingon.")]
	public async Task Un_profil_invalide_bloque_toute_la_creation(string key, string value, string message)
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");
		int before = 0;
		api.WithDb(db => before = db.Employees.Count());

		var res = await alex.PostAsync("/api/admin/employees", Form("Nora",
			("hireDate", "2024-03-03"),
			("profile.details.addressLine2", ""), // force la présence de details
			(key, value)));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Equal(message, await Message(res));
		Assert.False(EmailExists(api, "nora.form@test.local"));
		api.WithDb(db =>
		{
			Assert.Equal(before, db.Employees.Count());
			Assert.Equal(0, db.EmployeePrivateInfos.Count());
			Assert.Equal(0, db.EmergencyContacts.Count());
		});
	}

	[Fact]
	public async Task Un_contact_d_urgence_invalide_bloque_la_creation()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PostAsync("/api/admin/employees", Form("Nora",
			("hireDate", "2024-03-03"),
			("profile.details.city", ""),
			("profile.emergencyContacts[0].name", "Robin Test"),
			("profile.emergencyContacts[0].relationship", "Conjoint"),
			("profile.emergencyContacts[0].phoneNumber", "123")));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.StartsWith("Contact d'urgence 1 :", await Message(res));
		Assert.False(EmailExists(api, "nora.form@test.local"));
	}

	[Fact]
	public async Task Trois_contacts_d_urgence_sont_refuses_a_la_creation()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var fields = new List<(string, string)> { ("hireDate", "2024-03-03"), ("profile.details.city", "") };
		for (var i = 0; i < 3; i++)
		{
			fields.Add(($"profile.emergencyContacts[{i}].name", $"Contact {i}"));
			fields.Add(($"profile.emergencyContacts[{i}].relationship", "Ami"));
			fields.Add(($"profile.emergencyContacts[{i}].phoneNumber", $"514 555-000{i}"));
		}

		var res = await alex.PostAsync("/api/admin/employees", Form("Nora", [.. fields]));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Contains("Seulement 2 contacts d'urgence sont permis.", await res.Content.ReadAsStringAsync());
		Assert.False(EmailExists(api, "nora.form@test.local"));
	}

	[Fact]
	public async Task Le_pays_du_site_sert_pour_le_cellulaire_sans_adresse()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PostAsync("/api/admin/employees", Form("Nora",
			("hireDate", "2024-03-03"),
			("profile.details.personalMobilePhone", "514 555-9876")));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		var id = await EmployeeIdOf(res);
		api.WithDb(db => Assert.Equal("+15145559876", db.EmployeePrivateInfos.Single(p => p.EmployeeId == id).PersonalMobilePhone));
	}

	// ─── Outils ────────────────────────────────────────────────────────────

	/// <summary>Formulaire multipart minimal d'un employé, plus des champs en extra.</summary>
	private static MultipartFormDataContent Form(string firstName, params (string Key, string Value)[] extra)
	{
		var form = new MultipartFormDataContent
		{
			{ new StringContent(firstName), "firstName" },
			{ new StringContent("Test"), "lastName" },
			{ new StringContent($"{firstName.ToLowerInvariant()}.form@test.local"), "email" },
			{ new StringContent(((int)Departement.TI).ToString()), "departement" },
			{ new StringContent(((int)Role.Employee).ToString()), "role" },
			{ new StringContent("20"), "annualLeaveDays" },
			{ new StringContent("1"), "siteId" },
		};
		foreach (var (key, value) in extra)
			form.Add(new StringContent(value), key);
		return form;
	}

	private static async Task<int> EmployeeIdOf(HttpResponseMessage res) =>
		(await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("employeeId").GetInt32();

	private static async Task<JsonElement> CardOf(HttpClient admin, HttpResponseMessage created) =>
		await admin.GetFromJsonAsync<JsonElement>($"/api/admin/employees/{await EmployeeIdOf(created)}");

	private static bool EmailExists(ApiFactory api, string email)
	{
		var exists = false;
		api.WithDb(db => exists = db.Employees.Any(e => e.Email == email));
		return exists;
	}

	private static async Task<string?> Message(HttpResponseMessage res) =>
		(await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString();
}
