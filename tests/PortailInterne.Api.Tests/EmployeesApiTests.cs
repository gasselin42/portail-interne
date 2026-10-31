using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// GET /api/employees : l'annuaire, filtré par site, avec le site de chaque employé (étape 20b).
/// Jane passe au site de Paris ; les autres restent au site de départ.
/// </summary>
public class EmployeesApiTests
{
	[Fact]
	public async Task Sans_filtre_tous_les_sites_avec_le_nom_du_site()
	{
		var (api, _, homeName) = StartWithParis();
		using var _ = api;
		var sam = await api.LoginAsync("sam@test.local");

		var employees = (await sam.GetFromJsonAsync<List<EmployeeListItemResponse>>("/api/employees"))!;

		Assert.Equal("Paris", employees.Single(e => e.FirstName == "Jane").SiteName);
		Assert.Equal(homeName, employees.Single(e => e.FirstName == "Sam").SiteName);
		Assert.Equal(5, employees.Count);
	}

	[Fact]
	public async Task Le_filtre_par_site_garde_seulement_ce_site()
	{
		var (api, parisId, _) = StartWithParis();
		using var __ = api;
		var sam = await api.LoginAsync("sam@test.local");

		var paris = (await sam.GetFromJsonAsync<List<EmployeeListItemResponse>>($"/api/employees?siteId={parisId}"))!;
		var unknown = (await sam.GetFromJsonAsync<List<EmployeeListItemResponse>>("/api/employees?siteId=999"))!;

		Assert.Equal(["Jane"], paris.Select(e => e.FirstName));
		Assert.Empty(unknown);
	}

	[Fact]
	public async Task Le_filtre_par_site_se_combine_a_la_recherche()
	{
		var (api, _, _) = StartWithParis();
		using var __ = api;
		var sam = await api.LoginAsync("sam@test.local");
		var homeId = api.Team.Sam.SiteId;

		var found = (await sam.GetFromJsonAsync<List<EmployeeListItemResponse>>($"/api/employees?search=Mark&siteId={homeId}"))!;

		Assert.Equal(["Mark"], found.Select(e => e.FirstName));
	}

	[Fact]
	public async Task La_fiche_donne_le_nom_du_site()
	{
		var (api, _, _) = StartWithParis();
		using var __ = api;
		var sam = await api.LoginAsync("sam@test.local");

		var jane = (await sam.GetFromJsonAsync<EmployeeDetailResponse>($"/api/employees/{api.Team.Jane.Id}"))!;

		Assert.Equal("Paris", jane.SiteName);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	/// <summary>Démarre l'API, crée le site de Paris et y place Jane.</summary>
	private static (ApiFactory Api, int ParisId, string HomeName) StartWithParis()
	{
		var api = new ApiFactory().Start();
		var parisId = 0;
		var homeName = "";
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			parisId = paris.Id;
			db.Employees.Find(api.Team.Jane.Id)!.SiteId = paris.Id;
			homeName = db.Sites.Find(api.Team.Sam.SiteId)!.Name;
		});
		return (api, parisId, homeName);
	}
}
