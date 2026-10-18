using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Tests d'intégration des jours fériés (R12, R23).
/// « Aujourd'hui » = mercredi 14 octobre 2026 ; Alex est Admin du site Montréal (site 1).
/// </summary>
public class HolidaysApiTests
{
	private const string Montreal = "/api/sites/1/holidays";

	// ─── Lecture ────────────────────────────────────────────────────────────

	[Fact]
	public async Task Un_employe_lit_les_feries_du_quebec()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var holidays = await sam.GetFromJsonAsync<List<HolidayResponse>>($"{Montreal}?year=2026");

		Assert.Equal(9, holidays!.Count);
		Assert.Equal(new DateOnly(2026, 1, 1), holidays[0].Date);    // triés par date
		Assert.Equal(new DateOnly(2026, 12, 25), holidays[^1].Date);
		Assert.False(holidays.Single(h => h.Name == "Action de grâce").IsEditable); // 12 oct. : passé
		Assert.True(holidays.Single(h => h.Name == "Noël").IsEditable);
	}

	[Fact]
	public async Task Sans_annee_on_obtient_l_annee_en_cours()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var holidays = await sam.GetFromJsonAsync<List<HolidayResponse>>(Montreal);

		Assert.All(holidays!, h => Assert.Equal(2026, h.Date.Year));
	}

	[Fact]
	public async Task Une_annee_invalide_est_refusee()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.GetAsync($"{Montreal}?year=1999");

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Un_site_inexistant_donne_404()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.GetAsync("/api/sites/99/holidays");

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	[Fact]
	public async Task Un_site_sans_employe_a_quand_meme_ses_feries()
	{
		using var api = new ApiFactory().Start();
		var parisId = AddParis(api);
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.GetAsync($"/api/sites/{parisId}/holidays");

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
	}

	// ─── Création ───────────────────────────────────────────────────────────

	[Fact]
	public async Task L_admin_du_site_ajoute_un_ferie()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PostAsJsonAsync(Montreal, new { date = "2026-12-26", name = "  Lendemain de Noël  " });

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		var holiday = await res.Content.ReadFromJsonAsync<HolidayResponse>();
		Assert.Equal("Lendemain de Noël", holiday!.Name);
	}

	[Fact]
	public async Task Un_manager_ne_gere_pas_les_feries()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync(Montreal, new { date = "2026-12-26", name = "Lendemain de Noël" });

		Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
	}

	[Fact]
	public async Task Un_admin_ne_gere_pas_les_feries_d_un_autre_site()
	{
		using var api = new ApiFactory().Start();
		var parisId = AddParis(api);
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PostAsJsonAsync($"/api/sites/{parisId}/holidays", new { date = "2026-11-11", name = "Armistice" });

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	[Fact]
	public async Task Un_ferie_ne_peut_pas_etre_ajoute_dans_le_passe()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PostAsJsonAsync(Montreal, new { date = "2026-10-13", name = "Trop tard" });

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Deux_feries_ne_tombent_pas_le_meme_jour_sur_un_site()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PostAsJsonAsync(Montreal, new { date = "2026-12-25", name = "Doublon de Noël" });

		Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
	}

	[Fact]
	public async Task La_meme_date_est_permise_sur_un_autre_site()
	{
		using var api = new ApiFactory().Start();
		var parisId = AddParis(api);
		// Jane devient Admin du site de Paris
		api.WithDb(db =>
		{
			var jane = db.Employees.Single(e => e.Id == api.Team.Jane.Id);
			jane.SiteId = parisId;
			db.UserAccounts.Single(u => u.EmployeeId == jane.Id).Role = Role.Admin;
		});
		var jane = await api.LoginAsync("jane@test.local");

		var res = await jane.PostAsJsonAsync($"/api/sites/{parisId}/holidays", new { date = "2026-12-25", name = "Noël" });

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	// ─── Modification et suppression ────────────────────────────────────────

	[Fact]
	public async Task Un_ferie_a_venir_peut_etre_modifie_puis_supprime()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");
		var christmas = await FindAsync(alex, "Noël");

		var put = await alex.PutAsJsonAsync($"{Montreal}/{christmas.Id}", new { date = "2026-12-24", name = "Veille de Noël" });
		var afterPut = await FindAsync(alex, "Veille de Noël");
		var delete = await alex.DeleteAsync($"{Montreal}/{christmas.Id}");
		var all = await alex.GetFromJsonAsync<List<HolidayResponse>>($"{Montreal}?year=2026");

		Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
		Assert.Equal(new DateOnly(2026, 12, 24), afterPut.Date);
		Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
		Assert.DoesNotContain(all!, h => h.Id == christmas.Id);
	}

	[Fact]
	public async Task Un_ferie_passe_ne_change_plus()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");
		var thanksgiving = await FindAsync(alex, "Action de grâce"); // 12 octobre 2026

		var put = await alex.PutAsJsonAsync($"{Montreal}/{thanksgiving.Id}", new { date = "2026-12-28", name = "Déplacé" });
		var delete = await alex.DeleteAsync($"{Montreal}/{thanksgiving.Id}");

		Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
	}

	[Fact]
	public async Task On_ne_modifie_pas_le_ferie_d_un_autre_site_par_l_url()
	{
		using var api = new ApiFactory().Start();
		var parisId = AddParis(api);
		var parisHolidayId = 0;
		api.WithDb(db =>
		{
			var armistice = new Holiday { SiteId = parisId, Date = new DateOnly(2026, 11, 11), Name = "Armistice" };
			db.Holidays.Add(armistice);
			db.SaveChanges();
			parisHolidayId = armistice.Id;
		});
		var alex = await api.LoginAsync("alex@test.local");

		// L'id d'un férié de Paris, sous l'URL de Montréal
		var res = await alex.DeleteAsync($"{Montreal}/{parisHolidayId}");

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	/// <summary>Ajoute un site Paris, sans employé, et renvoie son id.</summary>
	private static int AddParis(ApiFactory api)
	{
		var id = 0;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			id = paris.Id;
		});
		return id;
	}

	private static async Task<HolidayResponse> FindAsync(HttpClient client, string name)
	{
		var holidays = await client.GetFromJsonAsync<List<HolidayResponse>>($"{Montreal}?year=2026");
		return holidays!.Single(h => h.Name == name);
	}
}
