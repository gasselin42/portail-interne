using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Préférences personnelles, <c>api/me/preferences</c> (étape 16) :
/// toujours celles de l'appelant, valeurs par défaut tant que rien n'est enregistré.
/// </summary>
public class PreferencesApiTests
{
	private const string Url = "/api/me/preferences";

	[Fact]
	public async Task Valeurs_par_defaut_sans_rien_creer_en_base()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var prefs = await sam.GetFromJsonAsync<PreferencesDto>(Url);

		Assert.Equal(CalendarHourRange.WorkHours, prefs!.CalendarHourRange);
		api.WithDb(db => Assert.Empty(db.EmployeePreferences));
	}

	[Fact]
	public async Task Enregistrer_puis_modifier()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var created = await sam.PutAsJsonAsync(Url, new { calendarHourRange = CalendarHourRange.FullDay });
		var afterCreate = await sam.GetFromJsonAsync<PreferencesDto>(Url);
		var updated = await sam.PutAsJsonAsync(Url, new { calendarHourRange = CalendarHourRange.WorkHours });
		var afterUpdate = await sam.GetFromJsonAsync<PreferencesDto>(Url);

		Assert.Equal(HttpStatusCode.OK, created.StatusCode);
		Assert.Equal(CalendarHourRange.FullDay, (await created.Content.ReadFromJsonAsync<PreferencesDto>())!.CalendarHourRange);
		Assert.Equal(CalendarHourRange.FullDay, afterCreate!.CalendarHourRange);

		Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
		Assert.Equal(CalendarHourRange.WorkHours, afterUpdate!.CalendarHourRange);
		api.WithDb(db => Assert.Single(db.EmployeePreferences)); // mise à jour, pas une 2e ligne
	}

	[Fact]
	public async Task Les_preferences_de_l_un_ne_touchent_pas_l_autre()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var jane = await api.LoginAsync("jane@test.local");

		await sam.PutAsJsonAsync(Url, new { calendarHourRange = CalendarHourRange.FullDay });
		var janePrefs = await jane.GetFromJsonAsync<PreferencesDto>(Url);

		Assert.Equal(CalendarHourRange.WorkHours, janePrefs!.CalendarHourRange);
		api.WithDb(db => Assert.Equal(api.Team.Sam.Id, Assert.Single(db.EmployeePreferences).EmployeeId));
	}

	[Fact]
	public async Task Plage_d_heures_inconnue_refusee()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PutAsJsonAsync(Url, new { calendarHourRange = 7 });

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		api.WithDb(db => Assert.Empty(db.EmployeePreferences));
	}

	[Fact]
	public async Task Connexion_requise()
	{
		using var api = new ApiFactory().Start();
		var anonymous = api.CreateClient();

		var get = await anonymous.GetAsync(Url);
		var put = await anonymous.PutAsJsonAsync(Url, new { calendarHourRange = CalendarHourRange.FullDay });

		Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);
		Assert.Equal(HttpStatusCode.Unauthorized, put.StatusCode);
	}
}
