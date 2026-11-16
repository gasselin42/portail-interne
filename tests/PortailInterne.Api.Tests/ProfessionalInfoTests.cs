using System.Net.Http.Json;
using System.Text.Json;
using PortailInterne.Api.Data;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Phase 3.1, étape 4 : l'annuaire, la fiche, la fiche admin, la recherche de personnes et /me
/// donnent les coordonnées professionnelles (R28), et jamais un champ personnel.
/// </summary>
public class ProfessionalInfoTests
{
	/// <summary>Noms (camelCase) qui ne doivent jamais sortir des endpoints publics.</summary>
	private static readonly string[] PrivateFields =
	[
		"personalMobilePhone", "personalEmail", "addressLine1", "addressLine2", "city", "region",
		"postalCode", "country", "birthMonth", "birthDay", "showBirthday", "emergencyContacts", "privateInfo",
	];

	[Fact]
	public async Task Chaque_endpoint_donne_les_coordonnees_professionnelles()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => FillSam(db, team.Sam.Id));
		var sam = await api.LoginAsync("sam@test.local");
		var alex = await api.LoginAsync("alex@test.local"); // Admin de Montréal

		var directory = await sam.GetFromJsonAsync<JsonElement>("/api/employees?search=sam");
		var card = await sam.GetFromJsonAsync<JsonElement>($"/api/employees/{team.Sam.Id}");
		var adminCard = await alex.GetFromJsonAsync<JsonElement>($"/api/admin/employees/{team.Sam.Id}");
		var me = await sam.GetFromJsonAsync<JsonElement>("/api/auth/me");

		foreach (var json in new[] { directory.EnumerateArray().Single(), card, adminCard, me })
		{
			Assert.Equal("Sammy", json.GetProperty("preferredName").GetString());
			Assert.Equal("Sam", json.GetProperty("firstName").GetString());
			Assert.Equal("+15145550100", json.GetProperty("siteMainPhoneNumber").GetString());
			Assert.Equal("2045", json.GetProperty("phoneExtension").GetString());
			Assert.Equal("+15145551234", json.GetProperty("workPhone").GetString());
			Assert.Equal("B-204", json.GetProperty("officeLocation").GetString());
			Assert.Equal("2024-03-03", json.GetProperty("hireDate").GetString());
			Assert.Equal(["en", "es", "fr"], json.GetProperty("languages").EnumerateArray().Select(l => l.GetString())); // triées par code
		}
	}

	[Fact]
	public async Task La_recherche_de_personnes_donne_le_prenom_d_usage()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Employees.Find(team.Sam.Id)!.PreferredName = "Sammy");
		var jane = await api.LoginAsync("jane@test.local");

		var lookup = await jane.GetFromJsonAsync<JsonElement>("/api/employees/lookup?search=sammy");

		var item = lookup.GetProperty("items").EnumerateArray().Single();
		Assert.Equal(team.Sam.Id, item.GetProperty("id").GetInt32());
		Assert.Equal("Sammy", item.GetProperty("preferredName").GetString());
	}

	[Fact]
	public async Task Sans_coordonnees_les_valeurs_sont_vides_et_propres()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Employees.Find(team.Mark.Id)!.HireDate = new DateOnly(2025, 1, 6));
		var mark = await api.LoginAsync("mark@test.local");

		var directory = await mark.GetFromJsonAsync<JsonElement>("/api/employees?search=mark");
		var card = await mark.GetFromJsonAsync<JsonElement>($"/api/employees/{team.Mark.Id}");
		var me = await mark.GetFromJsonAsync<JsonElement>("/api/auth/me");

		foreach (var json in new[] { directory.EnumerateArray().Single(), card, me })
		{
			foreach (var field in new[] { "preferredName", "workPhone", "phoneExtension", "officeLocation", "siteMainPhoneNumber" })
				Assert.Equal(JsonValueKind.Null, json.GetProperty(field).ValueKind);
			Assert.Equal(0, json.GetProperty("languages").GetArrayLength());
			Assert.Equal("2025-01-06", json.GetProperty("hireDate").GetString());
		}
	}

	[Fact]
	public async Task Aucun_champ_personnel_ne_sort_meme_rempli()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			FillSam(db, team.Sam.Id);
			db.EmployeePrivateInfos.Add(new EmployeePrivateInfo
			{
				EmployeeId = team.Sam.Id,
				PersonalMobilePhone = "+15145559876",
				PersonalEmail = "sam.perso@example.com",
				AddressLine1 = "123 rue Secrète",
				City = "Montréal",
				PostalCode = "H2X 1Y4",
				Country = "CA",
				BirthMonth = 5,
				BirthDay = 12,
				ShowBirthday = true,
				UpdatedAt = DateTime.UtcNow,
			});
			db.EmergencyContacts.Add(new EmergencyContact { EmployeeId = team.Sam.Id, Name = "Contact Secret", Relationship = "Conjoint", PhoneNumber = "+15145550000", SortOrder = 1 });
		});
		var sam = await api.LoginAsync("sam@test.local");
		var alex = await api.LoginAsync("alex@test.local");

		var bodies = new[]
		{
			await sam.GetStringAsync("/api/employees"),
			await sam.GetStringAsync($"/api/employees/{team.Sam.Id}"),
			await sam.GetStringAsync("/api/employees/lookup?search=sam"),
			await sam.GetStringAsync("/api/auth/me"),
			await alex.GetStringAsync($"/api/admin/employees/{team.Sam.Id}"),
		};

		foreach (var body in bodies)
		{
			foreach (var field in PrivateFields)
				Assert.DoesNotContain($"\"{field}\"", body);
			foreach (var secret in new[] { "5559876", "sam.perso", "Secrète", "H2X", "Contact Secret" })
				Assert.DoesNotContain(secret, body);
		}
	}

	private static void FillSam(AppDbContext db, int samId)
	{
		db.Sites.Find(1)!.MainPhoneNumber = "+15145550100";
		var sam = db.Employees.Find(samId)!;
		sam.PreferredName = "Sammy";
		sam.WorkPhone = "+15145551234";
		sam.PhoneExtension = "2045";
		sam.OfficeLocation = "B-204";
		sam.HireDate = new DateOnly(2024, 3, 3);
		db.EmployeeLanguages.AddRange(
			new EmployeeLanguage { EmployeeId = samId, LanguageCode = "fr" },
			new EmployeeLanguage { EmployeeId = samId, LanguageCode = "es" },
			new EmployeeLanguage { EmployeeId = samId, LanguageCode = "en" });
	}
}
