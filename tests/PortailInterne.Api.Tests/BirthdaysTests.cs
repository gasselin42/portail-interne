using System.Net.Http.Json;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Phase 3.1, étape 3d : anniversaire (mois + jour validés ensemble, 29 février permis)
/// et GET /api/employees/birthdays : la semaine (lundi → dimanche) dans le fuseau du site
/// de la personne connectée, employés actifs de son site qui ont choisi de l'afficher.
/// « Maintenant » par défaut : mercredi 14 octobre 2026 → semaine du 12 au 18 octobre.
/// </summary>
public class BirthdaysTests
{
	// ─── BirthdayRules.Validate ────────────────────────────────────────────

	[Theory]
	[InlineData(null, null)]
	[InlineData(1, 1)]
	[InlineData(2, 29)]
	[InlineData(4, 30)]
	[InlineData(12, 31)]
	public void Une_date_valide_ou_absente_passe(int? month, int? day)
	{
		Assert.Null(BirthdayRules.Validate(month, day));
	}

	[Theory]
	[InlineData(2, 30)]
	[InlineData(4, 31)]
	[InlineData(13, 1)]
	[InlineData(0, 10)]
	[InlineData(5, 0)]
	[InlineData(5, 32)]
	public void Une_date_impossible_est_refusee(int month, int day)
	{
		Assert.Equal("Date d'anniversaire invalide.", BirthdayRules.Validate(month, day));
	}

	[Theory]
	[InlineData(5, null)]
	[InlineData(null, 12)]
	public void Le_mois_et_le_jour_vont_ensemble(int? month, int? day)
	{
		Assert.Equal("Indique le mois et le jour, ou ni l'un ni l'autre.", BirthdayRules.Validate(month, day));
	}

	// ─── BirthdayRules.OccursOn ────────────────────────────────────────────

	[Theory]
	[InlineData(3, 15, "2027-03-15", true)]
	[InlineData(3, 15, "2027-03-16", false)]
	[InlineData(3, 15, "2027-04-15", false)]
	[InlineData(2, 29, "2027-02-28", true)]  // année non bissextile : fêté le 28
	[InlineData(2, 29, "2027-03-01", false)]
	[InlineData(2, 29, "2028-02-28", false)] // année bissextile : on attend le 29
	[InlineData(2, 29, "2028-02-29", true)]
	[InlineData(2, 28, "2027-02-28", true)]
	[InlineData(2, 28, "2028-02-29", false)]
	public void L_anniversaire_tombe_le_bon_jour(int month, int day, string date, bool expected)
	{
		Assert.Equal(expected, BirthdayRules.OccursOn(month, day, DateOnly.Parse(date)));
	}

	// ─── GET /api/employees/birthdays ──────────────────────────────────────

	[Fact]
	public async Task La_semaine_va_du_lundi_au_dimanche_triee_par_date()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			Birthday(db, team.Mark.Id, 10, 18); // dimanche
			Birthday(db, team.Sam.Id, 10, 12);  // lundi (lui-même : il se voit aussi)
			Birthday(db, team.Jane.Id, 10, 19); // lundi suivant : non
			Birthday(db, team.Alex.Id, 10, 11); // dimanche précédent : non
		});
		var sam = await api.LoginAsync("sam@test.local");

		var birthdays = (await sam.GetFromJsonAsync<List<BirthdayResponse>>("/api/employees/birthdays"))!;

		Assert.Equal([team.Sam.Id, team.Mark.Id], birthdays.Select(b => b.EmployeeId));
		Assert.Equal([new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 18)], birthdays.Select(b => b.Date));
	}

	[Fact]
	public async Task Seulement_ceux_qui_l_affichent_actifs_et_du_meme_site()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris", CountryCode = "FR" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Jane.Id)!.SiteId = paris.Id;
			db.Employees.Find(team.Alex.Id)!.IsActive = false;

			Birthday(db, team.Sam.Id, 10, 14);
			Birthday(db, team.Mark.Id, 10, 14, show: false); // privé (le choix par défaut)
			Birthday(db, team.Alex.Id, 10, 14);              // inactif
			Birthday(db, team.Jane.Id, 10, 14);              // autre site
			db.EmployeePrivateInfos.Add(new EmployeePrivateInfo { EmployeeId = team.Gaby.Id, ShowBirthday = true, UpdatedAt = DateTime.UtcNow }); // aucune date
		});
		var sam = await api.LoginAsync("sam@test.local");
		var jane = await api.LoginAsync("jane@test.local");

		var fromMontreal = (await sam.GetFromJsonAsync<List<BirthdayResponse>>("/api/employees/birthdays"))!;
		var fromParis = (await jane.GetFromJsonAsync<List<BirthdayResponse>>("/api/employees/birthdays"))!;

		Assert.Equal(team.Sam.Id, Assert.Single(fromMontreal).EmployeeId);
		Assert.Equal(team.Jane.Id, Assert.Single(fromParis).EmployeeId);
	}

	[Fact]
	public void Le_choix_d_afficher_est_faux_par_defaut()
	{
		Assert.False(new EmployeePrivateInfo().ShowBirthday);
	}

	[Fact]
	public async Task Une_semaine_a_cheval_sur_deux_annees()
	{
		using var api = new ApiFactory(new DateTimeOffset(2026, 12, 30, 17, 0, 0, TimeSpan.Zero)).Start(); // mer. 30 déc. → 28 déc. au 3 janv.
		var team = api.Team;
		api.WithDb(db =>
		{
			Birthday(db, team.Mark.Id, 1, 2);
			Birthday(db, team.Sam.Id, 12, 28);
			Birthday(db, team.Jane.Id, 1, 4); // lundi suivant : non
		});
		var sam = await api.LoginAsync("sam@test.local");

		var birthdays = (await sam.GetFromJsonAsync<List<BirthdayResponse>>("/api/employees/birthdays"))!;

		Assert.Equal([new DateOnly(2026, 12, 28), new DateOnly(2027, 1, 2)], birthdays.Select(b => b.Date));
		Assert.Equal([team.Sam.Id, team.Mark.Id], birthdays.Select(b => b.EmployeeId));
	}

	[Theory]
	[InlineData("2027-02-24", "2027-02-28")] // 2027 : pas de 29 février, fêté le dimanche 28
	[InlineData("2028-03-01", "2028-02-29")] // 2028 : semaine du lundi 28 février, fêté le 29
	public async Task Le_29_fevrier_est_fete_une_seule_fois(string today, string expected)
	{
		var now = new DateTimeOffset(DateTime.Parse(today).AddHours(17), TimeSpan.Zero);
		using var api = new ApiFactory(now).Start();
		var team = api.Team;
		api.WithDb(db => Birthday(db, team.Mark.Id, 2, 29));
		var sam = await api.LoginAsync("sam@test.local");

		var birthdays = (await sam.GetFromJsonAsync<List<BirthdayResponse>>("/api/employees/birthdays"))!;

		Assert.Equal(DateOnly.Parse(expected), Assert.Single(birthdays).Date);
	}

	[Fact]
	public async Task La_semaine_suit_le_fuseau_du_site_de_la_personne_connectee()
	{
		// Dimanche 18 oct., 23 h 30 UTC : encore dimanche à Montréal, déjà lundi 19 à Paris.
		using var api = new ApiFactory(new DateTimeOffset(2026, 10, 18, 23, 30, 0, TimeSpan.Zero)).Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris", CountryCode = "FR" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Jane.Id)!.SiteId = paris.Id;
			Birthday(db, team.Jane.Id, 10, 19);
			Birthday(db, team.Sam.Id, 10, 19);
			Birthday(db, team.Mark.Id, 10, 13);
		});
		var sam = await api.LoginAsync("sam@test.local");
		var jane = await api.LoginAsync("jane@test.local");

		var fromMontreal = (await sam.GetFromJsonAsync<List<BirthdayResponse>>("/api/employees/birthdays"))!;
		var fromParis = (await jane.GetFromJsonAsync<List<BirthdayResponse>>("/api/employees/birthdays"))!;

		Assert.Equal(team.Mark.Id, Assert.Single(fromMontreal).EmployeeId); // semaine du 12 au 18
		Assert.Equal(new DateOnly(2026, 10, 19), Assert.Single(fromParis).Date); // semaine du 19 au 25
	}

	[Fact]
	public async Task La_reponse_donne_le_prenom_d_usage_et_aucune_photo()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.Employees.Find(team.Mark.Id)!.PreferredName = "Marco";
			Birthday(db, team.Mark.Id, 10, 15);
		});
		var sam = await api.LoginAsync("sam@test.local");

		var json = await sam.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/employees/birthdays");

		var mark = Assert.Single(json.EnumerateArray());
		Assert.Equal("Marco", mark.GetProperty("firstName").GetString());
		Assert.Equal("Test", mark.GetProperty("lastName").GetString());
		Assert.Equal(System.Text.Json.JsonValueKind.Null, mark.GetProperty("photoUrl").ValueKind);
		Assert.Equal("2026-10-15", mark.GetProperty("date").GetString());
		Assert.False(mark.TryGetProperty("birthMonth", out _)); // rien d'autre de privé
	}

	private static void Birthday(AppDbContext db, int employeeId, int month, int day, bool show = true) =>
		db.EmployeePrivateInfos.Add(new EmployeePrivateInfo
		{
			EmployeeId = employeeId,
			BirthMonth = month,
			BirthDay = day,
			ShowBirthday = show,
			UpdatedAt = DateTime.UtcNow,
		});
}
