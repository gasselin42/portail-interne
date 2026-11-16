using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Phase 3.1, étape 6 : PUT /api/employees/{id}/profile remplace l'ensemble
/// (prénom d'usage, langues, coordonnées personnelles, anniversaire, contacts d'urgence).
/// Permis à l'employé lui-même et à un Admin de son site ; jamais au manager. Rien n'est écrit si une validation échoue.
/// Équipe de test (Montréal) : Alex Admin, Mark manager de Sam, Jane dans aucune équipe, Gaby Admin global.
/// </summary>
public class ProfileUpdateTests
{
	// ─── Enregistrement ────────────────────────────────────────────────────

	[Fact]
	public async Task Tout_est_enregistre_nettoye_et_mis_en_forme()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", Profile(
			preferredName: "  Sammy ",
			languages: [" FR", "es", "fr"],
			details: Details(
				mobile: "514 555-9876",
				email: "  sam.perso@example.com ",
				line1: " 123 rue des Érables ",
				line2: "  ",
				city: "Montréal",
				region: " QC",
				postal: "h2x1y4",
				country: "ca",
				month: 5,
				day: 12,
				show: true),
			contacts: [Contact(" Robin ", " Conjoint ", "514 555-0001"), Contact("Claire", "Mère", "+33 6 12 34 56 78")]));

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		api.WithDb(db =>
		{
			var employee = db.Employees.AsNoTracking().Include(e => e.Languages).Single(e => e.Id == team.Sam.Id);
			Assert.Equal("Sammy", employee.PreferredName);
			Assert.Equal("sammy", employee.DisplayFirstNameKey);
			Assert.Equal(["es", "fr"], employee.Languages.Select(l => l.LanguageCode).Order());

			var info = db.EmployeePrivateInfos.AsNoTracking().Single(p => p.EmployeeId == team.Sam.Id);
			Assert.Equal("+15145559876", info.PersonalMobilePhone);
			Assert.Equal("sam.perso@example.com", info.PersonalEmail);
			Assert.Equal("123 rue des Érables", info.AddressLine1);
			Assert.Null(info.AddressLine2);
			Assert.Equal("Montréal", info.City);
			Assert.Equal("QC", info.Region);
			Assert.Equal("H2X 1Y4", info.PostalCode);
			Assert.Equal("CA", info.Country);
			Assert.Equal(5, info.BirthMonth);
			Assert.Equal(12, info.BirthDay);
			Assert.True(info.ShowBirthday);
			Assert.Equal(DateTimeKind.Utc, info.UpdatedAt.Kind);
			Assert.True(info.UpdatedAt > DateTime.UtcNow.AddMinutes(-5));

			var contacts = db.EmergencyContacts.AsNoTracking().Where(c => c.EmployeeId == team.Sam.Id).OrderBy(c => c.SortOrder).ToList();
			Assert.Equal(["Robin", "Claire"], contacts.Select(c => c.Name));
			Assert.Equal(["Conjoint", "Mère"], contacts.Select(c => c.Relationship));
			Assert.Equal(["+15145550001", "+33612345678"], contacts.Select(c => c.PhoneNumber));
			Assert.Equal([1, 2], contacts.Select(c => c.SortOrder));
		});
	}

	[Fact]
	public async Task Un_deuxieme_envoi_remplace_tout()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var sam = await api.LoginAsync("sam@test.local");
		await sam.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", Profile(
			preferredName: "Sammy",
			languages: ["fr", "en"],
			details: Details(line1: "123 rue A", city: "Montréal", country: "CA", mobile: "514 555-9876", month: 5, day: 12, show: true),
			contacts: [Contact("Robin", "Conjoint", "514 555-0001"), Contact("Claire", "Mère", "514 555-0002")]));

		// Contacts inversés (la position donne l'ordre), « fr » gardé, « en » retiré, « es » ajouté, tout le reste vidé.
		var res = await sam.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", Profile(
			preferredName: "",
			languages: ["es", "fr"],
			details: Details(),
			contacts: [Contact("Claire", "Mère", "514 555-0002"), Contact("Robin", "Conjoint", "514 555-0001")]));

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		api.WithDb(db =>
		{
			var employee = db.Employees.AsNoTracking().Include(e => e.Languages).Single(e => e.Id == team.Sam.Id);
			Assert.Null(employee.PreferredName);
			Assert.Equal("sam", employee.DisplayFirstNameKey);
			Assert.Equal(["es", "fr"], employee.Languages.Select(l => l.LanguageCode).Order());

			var info = db.EmployeePrivateInfos.AsNoTracking().Single(p => p.EmployeeId == team.Sam.Id); // toujours une seule ligne
			Assert.Null(info.AddressLine1);
			Assert.Null(info.City);
			Assert.Null(info.Country);
			Assert.Null(info.PersonalMobilePhone);
			Assert.Null(info.BirthMonth);
			Assert.False(info.ShowBirthday);

			var names = db.EmergencyContacts.AsNoTracking().Where(c => c.EmployeeId == team.Sam.Id).OrderBy(c => c.SortOrder).Select(c => c.Name).ToList();
			Assert.Equal(["Claire", "Robin"], names);
		});
	}

	[Fact]
	public async Task Une_liste_vide_retire_les_contacts_et_les_langues()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var sam = await api.LoginAsync("sam@test.local");
		await sam.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", Profile(languages: ["fr"], contacts: [Contact("Robin", "Conjoint", "514 555-0001")]));

		await sam.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", Profile());

		api.WithDb(db =>
		{
			Assert.False(db.EmergencyContacts.Any(c => c.EmployeeId == team.Sam.Id));
			Assert.False(db.EmployeeLanguages.Any(l => l.EmployeeId == team.Sam.Id));
		});
	}

	[Fact]
	public async Task Sans_date_d_anniversaire_l_affichage_est_force_a_faux()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var sam = await api.LoginAsync("sam@test.local");

		await sam.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", Profile(details: Details(show: true)));

		api.WithDb(db => Assert.False(db.EmployeePrivateInfos.Single(p => p.EmployeeId == team.Sam.Id).ShowBirthday));
	}

	[Fact]
	public async Task Le_cellulaire_suit_le_pays_de_l_adresse_sinon_celui_du_site()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var sam = await api.LoginAsync("sam@test.local");
		var jane = await api.LoginAsync("jane@test.local");

		var french = await sam.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", Profile(details: Details(
			line1: "1 rue de Rivoli", city: "Paris", postal: "75001", country: "FR", mobile: "06 12 34 56 78")));
		var withoutAddress = await jane.PutAsJsonAsync($"/api/employees/{team.Jane.Id}/profile", Profile(details: Details(mobile: "514 555-9876")));

		Assert.Equal(HttpStatusCode.NoContent, french.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, withoutAddress.StatusCode);
		api.WithDb(db =>
		{
			Assert.Equal("+33612345678", db.EmployeePrivateInfos.Single(p => p.EmployeeId == team.Sam.Id).PersonalMobilePhone);
			Assert.Equal("+15145559876", db.EmployeePrivateInfos.Single(p => p.EmployeeId == team.Jane.Id).PersonalMobilePhone);
		});
	}

	[Fact]
	public async Task Un_email_vide_est_accepte()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", Profile(details: Details(email: "")));

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		api.WithDb(db => Assert.Null(db.EmployeePrivateInfos.Single(p => p.EmployeeId == team.Sam.Id).PersonalEmail));
	}

	// ─── Droits ────────────────────────────────────────────────────────────

	[Theory]
	[InlineData("sam@test.local")]  // lui-même
	[InlineData("alex@test.local")] // Admin de son site
	[InlineData("gaby@test.local")] // Admin global
	public async Task Lui_meme_et_les_admins_peuvent_modifier(string email)
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var client = await api.LoginAsync(email);

		var res = await client.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", Profile(preferredName: "Sammy"));

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		api.WithDb(db => Assert.Equal("Sammy", db.Employees.Single(e => e.Id == team.Sam.Id).PreferredName));
	}

	[Fact]
	public async Task Le_manager_les_collegues_et_les_autres_sites_recoivent_404()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris", CountryCode = "FR" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Jane.Id)!.SiteId = paris.Id;
		});
		var mark = await api.LoginAsync("mark@test.local");   // manager direct de Sam
		var sam = await api.LoginAsync("sam@test.local");
		var alex = await api.LoginAsync("alex@test.local");   // Admin de Montréal

		var asManager = await mark.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", Profile(preferredName: "X"));
		var asColleague = await sam.PutAsJsonAsync($"/api/employees/{team.Mark.Id}/profile", Profile(preferredName: "X"));
		var otherSite = await alex.PutAsJsonAsync($"/api/employees/{team.Jane.Id}/profile", Profile(preferredName: "X"));
		var unknown = await alex.PutAsJsonAsync("/api/employees/99999/profile", Profile(preferredName: "X"));

		Assert.Equal(HttpStatusCode.NotFound, asManager.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, asColleague.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, otherSite.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
		api.WithDb(db => Assert.False(db.Employees.Any(e => e.PreferredName == "X")));
	}

	[Fact]
	public async Task Un_employe_inactif_ne_peut_plus_etre_modifie()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var alex = await api.LoginAsync("alex@test.local");
		api.WithDb(db => db.Employees.Find(team.Sam.Id)!.IsActive = false);

		var res = await alex.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", Profile(preferredName: "Sammy"));

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	// ─── Refus ─────────────────────────────────────────────────────────────

	public static TheoryData<object, string> Refused => new()
	{
		{ Profile(languages: ["fr", "xx"]), "Langue inconnue : xx." },
		{ Profile(details: Details(country: "ZZ")), "Pays inconnu. Utilise un code à deux lettres, par exemple « CA » ou « FR »." },
		{ Profile(details: Details(line1: "123 rue A")), "Adresse personnelle incomplète : ville, pays manquant." },
		{ Profile(details: Details(postal: "H2X 1Y4", country: "CA")), "Adresse personnelle incomplète : adresse, ville manquant." },
		{ Profile(details: Details(line1: "123 rue A", city: "Montréal", country: "CA", postal: "12345")), "Code postal invalide pour ce pays." },
		{ Profile(details: Details(mobile: "123 456-7890")), "Numéro de cellulaire personnel invalide pour ce pays." },
		{ Profile(details: Details(email: "pas-un-email")), "Email personnel invalide." },
		{ Profile(details: Details(month: 2, day: 30)), "Date d'anniversaire invalide." },
		{ Profile(details: Details(month: 5)), "Indique le mois et le jour, ou ni l'un ni l'autre." },
		{ Profile(contacts: [Contact("Robin", "Conjoint", "514 555-0001"), Contact("Claire", "Mère", "123")]), "Contact d'urgence 2 : Numéro de téléphone invalide pour ce pays." },
	};

	[Theory]
	[MemberData(nameof(Refused))]
	public async Task Une_valeur_invalide_est_refusee_et_rien_n_est_ecrit(object profile, string message)
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", WithPreferredName(profile, "Sammy"));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Equal(message, (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
		api.WithDb(db =>
		{
			Assert.Null(db.Employees.Single(e => e.Id == team.Sam.Id).PreferredName);
			Assert.False(db.EmployeePrivateInfos.Any());
			Assert.False(db.EmergencyContacts.Any());
			Assert.False(db.EmployeeLanguages.Any());
		});
	}

	[Fact]
	public async Task Un_troisieme_contact_est_refuse()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", Profile(contacts:
			[Contact("A", "Ami", "514 555-0001"), Contact("B", "Ami", "514 555-0002"), Contact("C", "Ami", "514 555-0003")]));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Contains("Seulement 2 contacts d'urgence sont permis.", await res.Content.ReadAsStringAsync());
		api.WithDb(db => Assert.False(db.EmergencyContacts.Any()));
	}

	[Theory]
	[InlineData("  ", "Conjoint", "514 555-0001", "Le nom du contact d'urgence est obligatoire.")]
	[InlineData("Robin", "", "514 555-0001", "La relation avec le contact d'urgence est obligatoire.")]
	[InlineData("Robin", "Conjoint", " ", "Le numéro de téléphone du contact d'urgence est obligatoire.")]
	public async Task Un_contact_incomplet_est_refuse(string name, string relationship, string phone, string message)
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", Profile(contacts: [Contact(name, relationship, phone)]));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Contains(message, await res.Content.ReadAsStringAsync());
	}

	[Fact]
	public async Task Sans_bloc_details_la_requete_est_refusee()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.EmployeePrivateInfos.Add(new EmployeePrivateInfo { EmployeeId = team.Sam.Id, City = "Montréal", UpdatedAt = DateTime.UtcNow }));
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PutAsJsonAsync($"/api/employees/{team.Sam.Id}/profile", new { preferredName = "Sammy", languages = Array.Empty<string>(), emergencyContacts = Array.Empty<object>() });

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		api.WithDb(db => Assert.Equal("Montréal", db.EmployeePrivateInfos.Single().City)); // rien d'effacé
	}

	// ─── Outils ────────────────────────────────────────────────────────────

	private static Dictionary<string, object?> Profile(string? preferredName = null, string[]? languages = null, Dictionary<string, object?>? details = null, object[]? contacts = null) => new()
	{
		["preferredName"] = preferredName,
		["languages"] = languages ?? [],
		["details"] = details ?? Details(),
		["emergencyContacts"] = contacts ?? [],
	};

	private static Dictionary<string, object?> WithPreferredName(object profile, string preferredName)
	{
		var copy = new Dictionary<string, object?>((Dictionary<string, object?>)profile) { ["preferredName"] = preferredName };
		return copy;
	}

	private static Dictionary<string, object?> Details(
		string? mobile = null, string? email = null, string? line1 = null, string? line2 = null, string? city = null,
		string? region = null, string? postal = null, string? country = null, int? month = null, int? day = null, bool show = false) => new()
	{
		["personalMobilePhone"] = mobile,
		["personalEmail"] = email,
		["addressLine1"] = line1,
		["addressLine2"] = line2,
		["city"] = city,
		["region"] = region,
		["postalCode"] = postal,
		["country"] = country,
		["birthMonth"] = month,
		["birthDay"] = day,
		["showBirthday"] = show,
	};

	private static object Contact(string name, string relationship, string phone) => new { name, relationship, phoneNumber = phone };
}
