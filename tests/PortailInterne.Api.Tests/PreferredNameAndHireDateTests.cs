using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Phase 3.1, étape 3b : prénom d'usage (recherche par les deux prénoms, tri par le nom affiché),
/// local, et date d'embauche des employés existants (jour de création dans le fuseau du site).
/// Toute l'équipe de test s'appelle « Test » : le tri se joue donc sur le prénom affiché.
/// </summary>
public class PreferredNameAndHireDateTests
{
	// ─── Clé du prénom affiché ─────────────────────────────────────────────

	[Theory]
	[InlineData("Jojo", "jojo")]
	[InlineData("  Élodie ", "elodie")]
	[InlineData(null, "jane")]
	[InlineData("   ", "jane")] // un prénom d'usage blanc compte comme absent
	public void La_cle_affichee_suit_le_prenom_d_usage_sinon_le_prenom(string? preferred, string expected)
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;

		api.WithDb(db => db.Employees.Find(team.Jane.Id)!.PreferredName = preferred);

		api.WithDb(db => Assert.Equal(expected, db.Employees.AsNoTracking().Single(e => e.Id == team.Jane.Id).DisplayFirstNameKey));
	}

	[Fact]
	public void Retirer_le_prenom_d_usage_remet_le_prenom()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Employees.Find(team.Jane.Id)!.PreferredName = "Jojo");

		api.WithDb(db => db.Employees.Find(team.Jane.Id)!.PreferredName = null);

		api.WithDb(db => Assert.Equal("jane", db.Employees.AsNoTracking().Single(e => e.Id == team.Jane.Id).DisplayFirstNameKey));
	}

	// ─── Recherche et tri ──────────────────────────────────────────────────

	[Theory]
	[InlineData("jojo")]
	[InlineData("Jane")]
	[InlineData("Jojo Test")]
	[InlineData("Test Jojo")]
	[InlineData("Jane Test")]
	public async Task La_recherche_trouve_le_prenom_et_le_prenom_d_usage(string search)
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Employees.Find(team.Jane.Id)!.PreferredName = "Jojo");
		var sam = await api.LoginAsync("sam@test.local");

		var found = (await sam.GetFromJsonAsync<List<EmployeeListItemResponse>>($"/api/employees?search={Uri.EscapeDataString(search)}"))!;

		Assert.Equal(team.Jane.Id, Assert.Single(found).Id);
	}

	[Fact]
	public async Task L_annuaire_et_la_recherche_de_personnes_trient_par_le_nom_affiche()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Employees.Find(team.Sam.Id)!.PreferredName = "Aaron"); // Sam passe en tête
		var jane = await api.LoginAsync("jane@test.local");

		var directory = (await jane.GetFromJsonAsync<List<EmployeeListItemResponse>>("/api/employees"))!;
		var lookup = await jane.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/employees/lookup?search=test");

		int[] expected = [team.Sam.Id, team.Alex.Id, team.Gaby.Id, team.Jane.Id, team.Mark.Id];
		Assert.Equal(expected, directory.Select(e => e.Id).Where(expected.Contains));
		Assert.Equal(expected, LookupIds(lookup).Where(expected.Contains));
	}

	[Fact]
	public async Task Les_comptes_de_l_admin_trient_par_le_nom_affiche()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.Employees.Find(team.Mark.Id)!.PreferredName = "Aaron");
		var gaby = await api.LoginAsync("gaby@test.local");

		var accounts = await gaby.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/admin/accounts");

		var names = Items(accounts).Select(a => a.GetProperty("employeeId").GetInt32()).ToList();
		Assert.Equal(team.Mark.Id, names.First());
	}

	// ─── Date d'embauche des employés existants ────────────────────────────

	[Fact]
	public void La_date_d_embauche_est_le_jour_de_creation_dans_le_fuseau_du_site()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris", CountryCode = "FR" };
			db.Sites.Add(paris);
			db.SaveChanges();
			foreach (var e in db.Employees)
				e.HireDate = default;

			db.Employees.Find(team.Sam.Id)!.CreatedAt = new DateTime(2026, 10, 2, 1, 30, 0, DateTimeKind.Utc); // 1er oct., 21 h 30 à Montréal
			db.Employees.Find(team.Mark.Id)!.CreatedAt = new DateTime(2026, 10, 1, 15, 0, 0, DateTimeKind.Utc);
			var jane = db.Employees.Find(team.Jane.Id)!;
			jane.SiteId = paris.Id;
			jane.CreatedAt = new DateTime(2026, 10, 1, 23, 30, 0, DateTimeKind.Utc); // 2 oct., 1 h 30 à Paris
			var alex = db.Employees.Find(team.Alex.Id)!;
			alex.CreatedAt = new DateTime(2026, 10, 1, 15, 0, 0, DateTimeKind.Utc);
			alex.HireDate = new DateOnly(2020, 3, 3); // déjà connue : on n'y touche pas
		});

		api.WithDb(DbSeeder.BackfillHireDates);
		api.WithDb(db =>
		{
			foreach (var e in db.Employees)
				e.CreatedAt = new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		});
		api.WithDb(DbSeeder.BackfillHireDates); // une 2e fois : rien ne change

		api.WithDb(db =>
		{
			var dates = db.Employees.AsNoTracking().ToDictionary(e => e.Id, e => e.HireDate);
			Assert.Equal(new DateOnly(2026, 10, 1), dates[team.Sam.Id]);
			Assert.Equal(new DateOnly(2026, 10, 1), dates[team.Mark.Id]);
			Assert.Equal(new DateOnly(2026, 10, 2), dates[team.Jane.Id]);
			Assert.Equal(new DateOnly(2020, 3, 3), dates[team.Alex.Id]);
			Assert.DoesNotContain(default, dates.Values);
		});
	}

	private static IEnumerable<int> LookupIds(System.Text.Json.JsonElement lookup) =>
		Items(lookup).Select(i => i.GetProperty("id").GetInt32());

	/// <summary>Les éléments d'une réponse, qu'elle soit une liste ou un objet avec une liste « items ».</summary>
	private static IEnumerable<System.Text.Json.JsonElement> Items(System.Text.Json.JsonElement json) =>
		json.ValueKind == System.Text.Json.JsonValueKind.Array
			? json.EnumerateArray()
			: json.GetProperty("items").EnumerateArray();
}
