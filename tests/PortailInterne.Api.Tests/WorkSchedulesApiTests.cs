using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Tests d'intégration des horaires de travail (R11).
/// « Aujourd'hui » = mercredi 14 octobre 2026 ; Mark est le manager de Sam.
/// </summary>
public class WorkSchedulesApiTests
{
	// Lundi 2 novembre 2026 : une date d'entrée en vigueur future
	private const string Future = "2026-11-02";

	// ─── Création ───────────────────────────────────────────────────────────

	[Fact]
	public async Task Le_manager_cree_l_horaire_de_son_equipe()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync(Url(api.Team.Sam), MondayToThursday(Future));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		var schedule = await res.Content.ReadFromJsonAsync<WorkScheduleResponse>();
		Assert.True(schedule!.IsEditable);
		Assert.Equal("Mark", schedule.CreatedByFirstName);
		// Envoyés dans le désordre, renvoyés du lundi au jeudi
		Assert.Equal(
			[DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday],
			schedule.Days.Select(d => d.DayOfWeek));
	}

	[Fact]
	public async Task L_admin_cree_l_horaire_de_n_importe_qui()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PostAsJsonAsync(Url(api.Team.Jane), MondayToThursday(Future));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	[Fact]
	public async Task Un_horaire_ne_peut_pas_commencer_dans_le_passe()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync(Url(api.Team.Sam), MondayToThursday("2026-10-13"));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Un_horaire_peut_commencer_aujourd_hui()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync(Url(api.Team.Sam), MondayToThursday("2026-10-14"));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	[Fact]
	public async Task Un_horaire_sans_jour_travaille_est_refuse()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync(Url(api.Team.Sam), new { effectiveFrom = Future, days = Array.Empty<object>() });

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Contains("au moins un jour", await res.Content.ReadAsStringAsync());
	}

	[Fact]
	public async Task Une_date_d_entree_en_vigueur_manquante_est_refusee()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync(Url(api.Team.Sam), new { days = new[] { Day(DayOfWeek.Monday) } });

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Deux_horaires_ne_peuvent_pas_commencer_le_meme_jour()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");
		await mark.PostAsJsonAsync(Url(api.Team.Sam), MondayToThursday(Future));

		var res = await mark.PostAsJsonAsync(Url(api.Team.Sam), MondayToThursday(Future));

		Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
	}

	// ─── Lecture et droits ──────────────────────────────────────────────────

	[Fact]
	public async Task L_employe_lit_son_horaire_mais_ne_peut_pas_le_modifier()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");
		var sam = await api.LoginAsync("sam@test.local");
		await mark.PostAsJsonAsync(Url(api.Team.Sam), MondayToThursday(Future));

		var schedules = await sam.GetFromJsonAsync<List<WorkScheduleResponse>>(Url(api.Team.Sam));
		var create = await sam.PostAsJsonAsync(Url(api.Team.Sam), MondayToThursday("2026-12-07"));

		Assert.Single(schedules!);
		Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);
	}

	[Fact]
	public async Task Un_collegue_ne_voit_pas_l_horaire_d_un_autre()
	{
		using var api = new ApiFactory().Start();
		var jane = await api.LoginAsync("jane@test.local");

		var res = await jane.GetAsync(Url(api.Team.Sam));

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	[Fact]
	public async Task Un_manager_ne_gere_pas_l_horaire_hors_de_son_equipe()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync(Url(api.Team.Jane), MondayToThursday(Future));

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	[Fact]
	public async Task Les_horaires_sont_listes_du_plus_recent_au_plus_ancien()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");
		await mark.PostAsJsonAsync(Url(api.Team.Sam), MondayToThursday(Future));
		await mark.PostAsJsonAsync(Url(api.Team.Sam), MondayToThursday("2026-12-07"));

		var schedules = await mark.GetFromJsonAsync<List<WorkScheduleResponse>>(Url(api.Team.Sam));

		Assert.Equal([new DateOnly(2026, 12, 7), new DateOnly(2026, 11, 2)], schedules!.Select(s => s.EffectiveFrom));
	}

	// ─── Modification et suppression ────────────────────────────────────────

	[Fact]
	public async Task Un_horaire_futur_peut_etre_modifie()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");
		var created = await CreateAsync(mark, api.Team.Sam, MondayToThursday(Future));

		// Le lundi reste, avec de nouvelles heures ; le reste de la semaine change
		var res = await mark.PutAsJsonAsync($"{Url(api.Team.Sam)}/{created.Id}", new
		{
			effectiveFrom = Future,
			days = new[] { Day(DayOfWeek.Monday, "09:00:00", "15:00:00"), Day(DayOfWeek.Friday) },
		});

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		var schedule = Assert.Single((await mark.GetFromJsonAsync<List<WorkScheduleResponse>>(Url(api.Team.Sam)))!);
		Assert.Equal([DayOfWeek.Monday, DayOfWeek.Friday], schedule.Days.Select(d => d.DayOfWeek));
		Assert.Equal(new TimeOnly(9, 0), schedule.Days[0].StartTime);
	}

	[Fact]
	public async Task Un_horaire_futur_peut_etre_supprime()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");
		var created = await CreateAsync(mark, api.Team.Sam, MondayToThursday(Future));

		var res = await mark.DeleteAsync($"{Url(api.Team.Sam)}/{created.Id}");

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		Assert.Empty((await mark.GetFromJsonAsync<List<WorkScheduleResponse>>(Url(api.Team.Sam)))!);
	}

	[Fact]
	public async Task Un_horaire_deja_en_vigueur_ne_change_plus()
	{
		using var api = new ApiFactory().Start();
		var scheduleId = 0;
		// Un horaire en vigueur depuis septembre : préparé en base, l'API refuse les dates passées
		api.WithDb(db =>
		{
			var past = new WorkSchedule
			{
				EmployeeId = api.Team.Sam.Id,
				EffectiveFrom = new DateOnly(2026, 9, 1),
				CreatedById = api.Team.Mark.Id,
				Days = [new WorkScheduleDay { DayOfWeek = DayOfWeek.Monday, StartTime = new(8, 0), EndTime = new(17, 0), BreakMinutes = 30 }],
			};
			db.WorkSchedules.Add(past);
			db.SaveChanges();
			scheduleId = past.Id;
		});
		var mark = await api.LoginAsync("mark@test.local");

		// Même en lui donnant une date future, l'historique ne se réécrit pas
		var put = await mark.PutAsJsonAsync($"{Url(api.Team.Sam)}/{scheduleId}", MondayToThursday(Future));
		var delete = await mark.DeleteAsync($"{Url(api.Team.Sam)}/{scheduleId}");
		var schedule = Assert.Single((await mark.GetFromJsonAsync<List<WorkScheduleResponse>>(Url(api.Team.Sam)))!);

		Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, delete.StatusCode);
		Assert.False(schedule.IsEditable);
	}

	[Fact]
	public async Task On_ne_modifie_pas_l_horaire_d_un_autre_employe_par_l_url()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");
		var janeSchedule = await CreateAsync(alex, api.Team.Jane, MondayToThursday(Future));

		// L'id d'un horaire de Jane, sous l'URL de Sam
		var res = await alex.DeleteAsync($"{Url(api.Team.Sam)}/{janeSchedule.Id}");

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	private static string Url(Employee employee) => $"/api/employees/{employee.Id}/schedules";

	/// <summary>Lundi au jeudi, 8 h à 17 h, 30 min de pause — envoyés dans le désordre.</summary>
	private static object MondayToThursday(string effectiveFrom) => new
	{
		effectiveFrom,
		days = new[]
		{
			Day(DayOfWeek.Thursday),
			Day(DayOfWeek.Monday),
			Day(DayOfWeek.Wednesday),
			Day(DayOfWeek.Tuesday),
		},
	};

	private static object Day(DayOfWeek day, string start = "08:00:00", string end = "17:00:00") =>
		new { dayOfWeek = day, startTime = start, endTime = end, breakMinutes = 30 };

	private static async Task<WorkScheduleResponse> CreateAsync(HttpClient client, Employee employee, object request)
	{
		var res = await client.PostAsJsonAsync(Url(employee), request);
		res.EnsureSuccessStatusCode();
		return (await res.Content.ReadFromJsonAsync<WorkScheduleResponse>())!;
	}
}
