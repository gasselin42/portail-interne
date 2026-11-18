using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortailInterne.Api.Data;
using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Phase 3.1, étape 1b : un seul service pour les numéros (E.164, validés selon le pays),
/// le numéro principal du site, et la conversion des numéros existants au démarrage.
/// </summary>
public class PhoneNumbersTests
{
	// ─── PhoneNumberRules.Normalize ─────────────────────────────────────────

	[Theory]
	[InlineData("514 555-1234", "CA", "+15145551234")]
	[InlineData("(514) 555-1234", "CA", "+15145551234")]
	[InlineData("06 12 34 56 78", "FR", "+33612345678")]
	[InlineData("+33 6 12 34 56 78", "CA", "+33612345678")] // l'indicatif l'emporte sur le pays par défaut
	public void Un_numero_valide_devient_E164(string input, string country, string expected)
	{
		Assert.Equal((expected, null), PhoneNumberRules.Normalize(input, country));
	}

	[Theory]
	[InlineData("123 456-7890", "CA")] // aucun indicatif régional ne commence par 1
	[InlineData("555-12", "CA")]
	[InlineData("bonjour", "CA")]
	[InlineData("514 555-1234", "FR")] // un numéro canadien n'est pas un numéro français
	public void Un_numero_invalide_donne_un_message(string input, string country)
	{
		Assert.Equal((null, "Numéro de téléphone invalide pour ce pays."), PhoneNumberRules.Normalize(input, country));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void Sans_numero_ni_numero_ni_erreur(string? input)
	{
		Assert.Equal((null, null), PhoneNumberRules.Normalize(input, "CA"));
	}

	// ─── Numéro principal du site ──────────────────────────────────────────

	[Fact]
	public async Task Le_numero_principal_du_site_est_enregistre_en_E164_selon_son_pays()
	{
		using var api = new ApiFactory().Start();
		var gaby = await api.LoginAsync("gaby@test.local");

		var created = await gaby.PostAsJsonAsync("/api/sites", new { name = "Lyon", timeZoneId = "Europe/Paris", countryCode = "FR", mainPhoneNumber = "04 72 00 00 00" });
		var invalid = await gaby.PostAsJsonAsync("/api/sites", new { name = "Québec", timeZoneId = "America/Toronto", countryCode = "CA", mainPhoneNumber = "123 456-7890" });
		var site = (await created.Content.ReadFromJsonAsync<Site>())!;
		var modified = await gaby.PutAsJsonAsync($"/api/sites/{site.Id}", new { name = "Lyon", timeZoneId = "Europe/Paris", countryCode = "FR", mainPhoneNumber = "06 12 34 56 78" });
		var modifiedInvalid = await gaby.PutAsJsonAsync($"/api/sites/{site.Id}", new { name = "Lyon", timeZoneId = "Europe/Paris", countryCode = "FR", mainPhoneNumber = "12" });

		Assert.Equal("+33472000000", site.MainPhoneNumber);
		Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, modified.StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, modifiedInvalid.StatusCode);
		api.WithDb(db =>
		{
			Assert.Equal("+33612345678", db.Sites.Single(s => s.Id == site.Id).MainPhoneNumber);
			Assert.False(db.Sites.Any(s => s.Name == "Québec"));
		});
	}

	// ─── Conversion des numéros existants ──────────────────────────────────

	[Fact]
	public void La_conversion_au_demarrage_convertit_vide_les_invalides_et_garde_les_E164()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris", CountryCode = "FR" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Sam.Id)!.WorkPhone = "514 555-1234";
			db.Employees.Find(team.Mark.Id)!.WorkPhone = "123 456-7890";
			db.Employees.Find(team.Alex.Id)!.WorkPhone = "+15145559999";
			var jane = db.Employees.Find(team.Jane.Id)!;
			jane.SiteId = paris.Id;
			jane.WorkPhone = "06 12 34 56 78"; // converti selon le pays de son site
		});
		var logger = new ListLogger();

		api.WithDb(db => DbSeeder.BackfillPhoneNumbers(db, logger));
		api.WithDb(db => DbSeeder.BackfillPhoneNumbers(db, logger)); // une 2e fois : rien ne change

		api.WithDb(db =>
		{
			var phones = db.Employees.AsNoTracking().ToDictionary(e => e.Id, e => e.WorkPhone);
			Assert.Equal("+15145551234", phones[team.Sam.Id]);
			Assert.Null(phones[team.Mark.Id]);
			Assert.Equal("+15145559999", phones[team.Alex.Id]);
			Assert.Equal("+33612345678", phones[team.Jane.Id]);
		});
		var warning = Assert.Single(logger.Messages);
		Assert.Contains("Mark", warning);
		Assert.Contains("123 456-7890", warning);
	}

	// ─── Téléphone pro et poste d'un employé (étape 2) ─────────────────────

	[Fact]
	public async Task Le_telephone_pro_et_le_poste_sont_valides_a_la_creation()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local"); // Admin de Montréal

		var created = await alex.PostAsync("/api/admin/employees", EmployeeForm("Nora", workPhone: "514 555-1234", extension: " 2045 "));
		var invalidPhone = await alex.PostAsync("/api/admin/employees", EmployeeForm("Omar", workPhone: "123 456-7890"));
		var lettersExtension = await alex.PostAsync("/api/admin/employees", EmployeeForm("Pia", extension: "20a"));
		var longExtension = await alex.PostAsync("/api/admin/employees", EmployeeForm("Rémi", extension: "1234567"));

		Assert.Equal(HttpStatusCode.Created, created.StatusCode);
		Assert.Equal("Numéro de téléphone invalide pour ce pays.", await Message(invalidPhone));
		Assert.Equal("Le poste doit contenir de 1 à 6 chiffres.", await Message(lettersExtension));
		Assert.Equal(HttpStatusCode.BadRequest, longExtension.StatusCode);
		api.WithDb(db =>
		{
			var nora = db.Employees.Single(e => e.FirstName == "Nora");
			Assert.Equal("+15145551234", nora.WorkPhone);
			Assert.Equal("2045", nora.PhoneExtension);
			Assert.False(db.Employees.Any(e => e.FirstName == "Omar" || e.FirstName == "Pia" || e.FirstName == "Rémi"));
		});
	}

	[Fact]
	public async Task La_modification_valide_le_telephone_selon_le_pays_du_nouveau_site()
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
		});
		var gaby = await api.LoginAsync("gaby@test.local"); // Admin global : peut changer le site

		var french = await gaby.PatchAsync($"/api/admin/employees/{team.Jane.Id}", EmployeeForm("Jane", siteId: parisId, workPhone: "06 12 34 56 78", extension: "12"));
		string? frenchPhone = null;
		api.WithDb(db => frenchPhone = db.Employees.AsNoTracking().Single(e => e.Id == team.Jane.Id).WorkPhone);
		var emptied = await gaby.PatchAsync($"/api/admin/employees/{team.Jane.Id}", EmployeeForm("Jane", siteId: parisId, workPhone: "  ", extension: ""));

		Assert.Equal(HttpStatusCode.NoContent, french.StatusCode);
		Assert.Equal("+33612345678", frenchPhone); // lu avec le pays de Paris, le nouveau site
		Assert.Equal(HttpStatusCode.NoContent, emptied.StatusCode);
		api.WithDb(db =>
		{
			var jane = db.Employees.AsNoTracking().Single(e => e.Id == team.Jane.Id);
			Assert.Null(jane.WorkPhone);
			Assert.Null(jane.PhoneExtension);
		});
	}

	[Fact]
	public async Task Me_et_l_annuaire_donnent_le_telephone_pro_et_le_poste()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			var sam = db.Employees.Find(team.Sam.Id)!;
			sam.WorkPhone = "+15145551234";
			sam.PhoneExtension = "2045";
		});
		var sam = await api.LoginAsync("sam@test.local");

		var me = await sam.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/auth/me");
		var card = await sam.GetFromJsonAsync<System.Text.Json.JsonElement>($"/api/employees/{team.Sam.Id}");

		Assert.Equal("+15145551234", me.GetProperty("workPhone").GetString());
		Assert.Equal("2045", me.GetProperty("phoneExtension").GetString());
		Assert.Equal("+15145551234", card.GetProperty("workPhone").GetString());
		Assert.Equal("2045", card.GetProperty("phoneExtension").GetString());
	}

	private static MultipartFormDataContent EmployeeForm(string firstName, int siteId = 1, string? workPhone = null, string? extension = null)
	{
		var form = new MultipartFormDataContent
		{
			{ new StringContent(firstName), "firstName" },
			{ new StringContent("Test"), "lastName" },
			{ new StringContent($"{firstName.ToLowerInvariant()}.tel@test.local"), "email" },
			{ new StringContent(((int)Departement.TI).ToString()), "departement" },
			{ new StringContent(((int)Role.Employee).ToString()), "role" },
			{ new StringContent("20"), "annualLeaveDays" },
			{ new StringContent(siteId.ToString()), "siteId" },
			{ new StringContent("2024-03-03"), "hireDate" },
		};
		if (workPhone is not null)
			form.Add(new StringContent(workPhone), "workPhone");
		if (extension is not null)
			form.Add(new StringContent(extension), "phoneExtension");
		return form;
	}

	private static async Task<string?> Message(HttpResponseMessage res) =>
		(await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("message").GetString();

	/// <summary>Un journal qui garde ses messages, pour les vérifier.</summary>
	private sealed class ListLogger : ILogger
	{
		public List<string> Messages { get; } = [];

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
		public bool IsEnabled(LogLevel logLevel) => true;
		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
			Messages.Add(formatter(state, exception));
	}
}
