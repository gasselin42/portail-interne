using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// « Je pars » et « Je suis de retour » (R24).
/// « Maintenant » = mercredi 14 octobre 2026, midi à Montréal.
/// Sam travaille du lundi au vendredi, 8 h à 17 h ; Mark est son manager.
/// </summary>
public class PresenceApiTests
{
	private const string Today = "2026-10-14";

	[Fact]
	public async Task Je_pars_puis_je_suis_de_retour()
	{
		using var api = StartWithSchedule(new ApiFactory());
		var sam = await api.LoginAsync("sam@test.local");

		var depart = await sam.PostAsJsonAsync(Url(api.Team.Sam, "depart"), new { reason = "  Urgence familiale  " });
		var gone = await depart.Content.ReadFromJsonAsync<AbsenceResponse>();
		var back = await sam.PostAsync(Url(api.Team.Sam, "return"), null);
		var returned = await back.Content.ReadFromJsonAsync<AbsenceResponse>();

		Assert.Equal(HttpStatusCode.OK, depart.StatusCode);
		Assert.True(gone!.InProgress);
		Assert.Equal(new TimeOnly(12, 0), gone.Start);
		Assert.Null(gone.PlannedStart);              // une urgence : rien de prévu
		Assert.Equal(LeaveStatus.Approuve, gone.Status); // approuvée d'office
		Assert.Equal("Urgence familiale", gone.Reason);

		Assert.Equal(HttpStatusCode.OK, back.StatusCode);
		Assert.False(returned!.InProgress);
		Assert.True(returned.ReturnConfirmed);
		Assert.Equal(new TimeOnly(12, 0), returned.End);
	}

	[Fact]
	public async Task On_ne_part_pas_deux_fois()
	{
		using var api = StartWithSchedule(new ApiFactory());
		var sam = await api.LoginAsync("sam@test.local");
		await sam.PostAsJsonAsync(Url(api.Team.Sam, "depart"), new { });

		var again = await sam.PostAsJsonAsync(Url(api.Team.Sam, "depart"), new { });

		Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
	}

	[Fact]
	public async Task On_ne_revient_pas_sans_etre_parti()
	{
		using var api = StartWithSchedule(new ApiFactory());
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsync(Url(api.Team.Sam, "return"), null);

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Je_pars_demarre_le_rendez_vous_prevu_proche()
	{
		using var api = StartWithSchedule(new ApiFactory());
		var mark = await api.LoginAsync("mark@test.local");
		var sam = await api.LoginAsync("sam@test.local");
		// Rendez-vous approuvé à 12 h 20 : Sam part à midi, 20 minutes plus tôt
		var planned = await CreateAsync(mark, api.Team.Sam, "12:20:00", "13:00:00");

		var gone = await (await sam.PostAsJsonAsync(Url(api.Team.Sam, "depart"), new { }))
			.Content.ReadFromJsonAsync<AbsenceResponse>();

		Assert.Equal(planned.Id, gone!.Id);           // c'est le rendez-vous, pas une nouvelle absence
		Assert.Equal(new TimeOnly(12, 0), gone.Start); // l'heure réelle de départ
		Assert.Equal("Mark", gone.CreatedByFirstName); // son auteur reste Mark
		Assert.True(gone.InProgress);
	}

	[Fact]
	public async Task Je_pars_loin_d_un_rendez_vous_cree_une_urgence()
	{
		using var api = StartWithSchedule(new ApiFactory());
		var mark = await api.LoginAsync("mark@test.local");
		var sam = await api.LoginAsync("sam@test.local");
		var planned = await CreateAsync(mark, api.Team.Sam, "15:00:00", "16:00:00");

		var gone = await (await sam.PostAsJsonAsync(Url(api.Team.Sam, "depart"), new { }))
			.Content.ReadFromJsonAsync<AbsenceResponse>();

		Assert.NotEqual(planned.Id, gone!.Id);
		Assert.Null(gone.PlannedStart);
	}

	[Fact]
	public async Task Une_demande_en_attente_n_empeche_pas_de_partir()
	{
		using var api = StartWithSchedule(new ApiFactory());
		var sam = await api.LoginAsync("sam@test.local");
		// Demande de 11 h à 13 h, pas encore approuvée : Sam n'est donc pas absent à midi
		await CreateAsync(sam, api.Team.Sam, "11:00:00", "13:00:00");

		var res = await sam.PostAsJsonAsync(Url(api.Team.Sam, "depart"), new { });

		Assert.Equal(HttpStatusCode.OK, res.StatusCode);
	}

	[Fact]
	public async Task On_ne_part_pas_en_dehors_des_heures_de_travail()
	{
		// 18 h à Montréal : la journée de Sam a fini à 17 h
		using var api = StartWithSchedule(new ApiFactory(new DateTimeOffset(2026, 10, 14, 22, 0, 0, TimeSpan.Zero)));
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync(Url(api.Team.Sam, "depart"), new { });

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task On_ne_part_pas_un_jour_non_travaille()
	{
		using var api = new ApiFactory().Start(); // Jane n'a aucun horaire
		var jane = await api.LoginAsync("jane@test.local");

		var res = await jane.PostAsJsonAsync(Url(api.Team.Jane, "depart"), new { });

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Le_manager_constate_le_depart_et_le_retour_mais_pas_un_collegue()
	{
		using var api = StartWithSchedule(new ApiFactory());
		var mark = await api.LoginAsync("mark@test.local");
		var jane = await api.LoginAsync("jane@test.local");

		var byJane = await jane.PostAsJsonAsync(Url(api.Team.Sam, "depart"), new { });
		var byMark = await mark.PostAsJsonAsync(Url(api.Team.Sam, "depart"), new { });
		var backByMark = await mark.PostAsync(Url(api.Team.Sam, "return"), null);

		Assert.Equal(HttpStatusCode.NotFound, byJane.StatusCode);
		Assert.Equal(HttpStatusCode.OK, byMark.StatusCode);
		Assert.Equal("Mark", (await byMark.Content.ReadFromJsonAsync<AbsenceResponse>())!.CreatedByFirstName);
		Assert.Equal(HttpStatusCode.OK, backByMark.StatusCode);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	/// <summary>Démarre l'API et donne à Sam un horaire du lundi au vendredi, 8 h à 17 h.</summary>
	private static ApiFactory StartWithSchedule(ApiFactory factory)
	{
		var api = factory.Start();
		api.WithDb(db => db.WorkSchedules.Add(new WorkSchedule
		{
			EmployeeId = api.Team.Sam.Id,
			EffectiveFrom = new DateOnly(2026, 10, 1),
			CreatedById = api.Team.Mark.Id,
			Days =
			[
				.. new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
					.Select(d => new WorkScheduleDay { DayOfWeek = d, StartTime = new(8, 0), EndTime = new(17, 0), BreakMinutes = 30 }),
			],
		}));
		return api;
	}

	private static string Url(Employee employee, string action = "") =>
		$"/api/employees/{employee.Id}/absences{(action == "" ? "" : "/" + action)}";

	private static async Task<AbsenceResponse> CreateAsync(HttpClient client, Employee employee, string start, string end)
	{
		var res = await client.PostAsJsonAsync(Url(employee), new { date = Today, plannedStart = start, plannedEnd = end });
		res.EnsureSuccessStatusCode();
		return (await res.Content.ReadFromJsonAsync<AbsenceResponse>())!;
	}
}
