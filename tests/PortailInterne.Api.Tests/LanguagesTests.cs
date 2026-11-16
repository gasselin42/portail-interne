using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Phase 3.1, étape 3c : langues parlées (liste fixe de codes ISO 639-1), une ligne par langue,
/// et filtre <c>?language=</c> de l'annuaire, combinable avec la recherche et le site.
/// </summary>
public class LanguagesTests
{
	// ─── LanguageLabels.Normalize ──────────────────────────────────────────

	[Fact]
	public void Les_codes_sont_nettoyes_en_minuscules_sans_doublon_dans_l_ordre()
	{
		AssertNormalized([" FR", "en", "fr", "En "], ["fr", "en"], null);
	}

	[Fact]
	public void Les_codes_blancs_ou_absents_sont_ignores()
	{
		AssertNormalized(["", null, "  ", "ht"], ["ht"], null);
	}

	[Theory]
	[InlineData("xx")]
	[InlineData("fra")]
	[InlineData("français")]
	public void Un_code_inconnu_donne_un_message_qui_le_nomme(string unknown)
	{
		AssertNormalized(["fr", unknown], [], $"Langue inconnue : {unknown}.");
	}

	[Fact]
	public void Sans_liste_ni_codes_ni_erreur()
	{
		AssertNormalized(null, [], null);
		AssertNormalized([], [], null);
		AssertNormalized(["  "], [], null);
	}

	[Fact]
	public void La_liste_contient_les_douze_langues_proposees()
	{
		Assert.Equal(
			["ar", "de", "en", "es", "fr", "ht", "it", "ja", "pt", "ru", "vi", "zh"],
			LanguageLabels.All.Keys.Order());
		Assert.Equal("Créole haïtien", LanguageLabels.All["ht"]);
	}

	// ─── Table ─────────────────────────────────────────────────────────────

	[Fact]
	public void Une_langue_ne_compte_qu_une_fois_par_employe_et_part_avec_lui()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var noraId = 0;
		api.WithDb(db =>
		{
			var nora = new Employee
			{
				FirstName = "Nora",
				LastName = "Test",
				Email = "nora@test.local",
				Departement = Departement.TI,
				IsActive = true,
				CreatedAt = DateTime.UtcNow,
				SiteId = 1,
				Languages = [new() { LanguageCode = "fr" }, new() { LanguageCode = "es" }],
			};
			db.Employees.Add(nora);
			db.EmployeeLanguages.Add(new EmployeeLanguage { EmployeeId = team.Sam.Id, LanguageCode = "fr" }); // même langue, autre employé : permis
			db.SaveChanges();
			noraId = nora.Id;
		});

		Assert.Throws<DbUpdateException>(() => api.WithDb(db =>
			db.EmployeeLanguages.Add(new EmployeeLanguage { EmployeeId = noraId, LanguageCode = "fr" }))); // clé composée
		api.WithDb(db => db.Employees.Remove(db.Employees.Find(noraId)!));

		api.WithDb(db => Assert.Equal(team.Sam.Id, db.EmployeeLanguages.Single().EmployeeId));
	}

	// ─── Filtre de l'annuaire ──────────────────────────────────────────────

	[Theory]
	[InlineData("es")]
	[InlineData(" ES ")]
	public async Task Le_filtre_garde_ceux_qui_parlent_la_langue(string language)
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		GiveLanguages(api, (team.Sam.Id, ["fr", "es"]), (team.Jane.Id, ["es"]), (team.Mark.Id, ["fr"]));
		var alex = await api.LoginAsync("alex@test.local");

		var found = (await alex.GetFromJsonAsync<List<EmployeeListItemResponse>>($"/api/employees?language={Uri.EscapeDataString(language)}"))!;

		Assert.Equal([team.Jane.Id, team.Sam.Id], found.Select(e => e.Id));
	}

	[Fact]
	public async Task Le_filtre_se_combine_avec_la_recherche_et_le_site()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var parisId = 0;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris", CountryCode = "FR" };
			db.Sites.Add(paris);
			db.SaveChanges();
			parisId = paris.Id;
			db.Employees.Find(team.Jane.Id)!.SiteId = paris.Id;
		});
		GiveLanguages(api, (team.Sam.Id, ["es"]), (team.Jane.Id, ["es"]), (team.Mark.Id, ["es"]));
		var alex = await api.LoginAsync("alex@test.local");

		var bySearch = (await alex.GetFromJsonAsync<List<EmployeeListItemResponse>>("/api/employees?language=es&search=sam"))!;
		var bySite = (await alex.GetFromJsonAsync<List<EmployeeListItemResponse>>($"/api/employees?language=es&siteId={parisId}"))!;

		Assert.Equal(team.Sam.Id, Assert.Single(bySearch).Id);
		Assert.Equal(team.Jane.Id, Assert.Single(bySite).Id);
	}

	[Theory]
	[InlineData("")]
	[InlineData("%20%20")]
	public async Task Un_filtre_vide_montre_tout_le_monde(string language)
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var all = (await alex.GetFromJsonAsync<List<EmployeeListItemResponse>>("/api/employees"))!;
		var filtered = (await alex.GetFromJsonAsync<List<EmployeeListItemResponse>>($"/api/employees?language={language}"))!;

		Assert.Equal(all.Select(e => e.Id), filtered.Select(e => e.Id));
	}

	[Fact]
	public async Task Un_code_inconnu_est_refuse()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.GetAsync("/api/employees?language=xx");

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		var body = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
		Assert.Equal("Langue inconnue : xx.", body.GetProperty("message").GetString());
	}

	/// <summary>Vérifie la liste et l'erreur séparément : un tuple compare ses listes par référence.</summary>
	private static void AssertNormalized(string?[]? codes, string[] expectedCodes, string? expectedError)
	{
		var (list, error) = LanguageLabels.Normalize(codes!);
		Assert.Equal(expectedCodes, list);
		Assert.Equal(expectedError, error);
	}

	private static void GiveLanguages(ApiFactory api, params (int EmployeeId, string[] Codes)[] languages) =>
		api.WithDb(db =>
		{
			foreach (var (employeeId, codes) in languages)
				foreach (var code in codes)
					db.EmployeeLanguages.Add(new EmployeeLanguage { EmployeeId = employeeId, LanguageCode = code });
		});
}
