using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Tests;

/// <summary>
/// GET /api/calendar : horaire effectif de chaque jour (R16) et congés, de bout en bout.
/// Semaine de Noël 2026 (lundi 21 au dimanche 27 décembre) ; Noël (vendredi 25) est un férié du site.
/// </summary>
public class CalendarApiTests
{
	private const string ChristmasWeek = "/api/calendar?from=2026-12-21&to=2026-12-27";

	[Fact]
	public async Task Chaque_couche_de_l_horaire_effectif_s_applique()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			// Horaire : lundi au jeudi, 8 h à 17 h
			db.WorkSchedules.Add(new WorkSchedule
			{
				EmployeeId = team.Sam.Id,
				EffectiveFrom = new DateOnly(2026, 10, 1),
				CreatedById = team.Mark.Id,
				Days =
				[
					.. new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday }
						.Select(d => new WorkScheduleDay { DayOfWeek = d, StartTime = new(8, 0), EndTime = new(17, 0), BreakMinutes = 30 }),
				],
			});
			// Mardi : ajustement approuvé ; mercredi : ajustement en attente
			db.ScheduleAdjustments.Add(Adjustment(team.Sam.Id, team.Mark.Id, 22, 9, 15, LeaveStatus.Approuve));
			db.ScheduleAdjustments.Add(Adjustment(team.Sam.Id, team.Sam.Id, 23, 10, 14, LeaveStatus.EnAttente));
			// Jeudi : congé approuvé
			db.LeaveRequests.Add(new LeaveRequest
			{
				EmployeeId = team.Sam.Id,
				StartDate = new DateTime(2026, 12, 24),
				EndDate = new DateTime(2026, 12, 24),
				Type = LeaveType.Conge,
				Status = LeaveStatus.Approuve,
			});
		});
		var sam = await api.LoginAsync("sam@test.local");

		var calendar = await sam.GetFromJsonAsync<CalendarResponse>(ChristmasWeek);
		var days = calendar!.Days;

		Assert.Equal(7, days.Count);
		AssertDay(days[0], DaySource.Horaire, 8, 17);      // lundi : l'horaire
		AssertDay(days[1], DaySource.Ajustement, 9, 15);   // mardi : l'ajustement approuvé
		AssertDay(days[2], DaySource.Horaire, 8, 17);      // mercredi : l'ajustement en attente n'est pas appliqué…
		Assert.Equal(new TimeOnly(10, 0), days[2].PendingAdjustment!.StartTime); // … mais il est affiché
		AssertDay(days[3], DaySource.Conge, null, null);   // jeudi : le congé
		AssertDay(days[4], DaySource.Ferie, null, null);   // vendredi : Noël
		Assert.Equal("Noël", days[4].HolidayName);
		AssertDay(days[5], DaySource.Horaire, null, null); // samedi : hors de l'horaire

		var leave = Assert.Single(calendar.Leaves);
		Assert.Equal(1, leave.WorkingDays); // les congés gardent leur solde
	}

	[Fact]
	public async Task Sans_horaire_les_jours_sont_non_definis_sauf_les_feries()
	{
		using var api = new ApiFactory().Start();
		var jane = await api.LoginAsync("jane@test.local");

		var days = (await jane.GetFromJsonAsync<CalendarResponse>(ChristmasWeek))!.Days;

		Assert.Equal(DaySource.NonDefini, days[0].Source);
		Assert.Equal(DaySource.Ferie, days[4].Source);
	}

	[Fact]
	public async Task Un_nouvel_horaire_s_applique_a_partir_de_sa_date()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(Schedule(team, new DateOnly(2026, 10, 1), new TimeOnly(8, 0)));
			db.WorkSchedules.Add(Schedule(team, new DateOnly(2026, 12, 23), new TimeOnly(10, 0))); // à partir du mercredi
		});
		var sam = await api.LoginAsync("sam@test.local");

		var days = (await sam.GetFromJsonAsync<CalendarResponse>(ChristmasWeek))!.Days;

		Assert.Equal(new TimeOnly(8, 0), days[1].StartTime);  // mardi : l'ancien horaire
		Assert.Equal(new TimeOnly(10, 0), days[2].StartTime); // mercredi : le nouveau
	}

	[Fact]
	public async Task Un_employe_ne_voit_que_son_propre_calendrier()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var jane = await api.LoginAsync("jane@test.local");
		await sam.PostAsJsonAsync("/api/leaves", new { startDate = "2026-12-21", endDate = "2026-12-22", type = LeaveType.Conge });

		var janeCalendar = await jane.GetFromJsonAsync<CalendarResponse>(ChristmasWeek);

		Assert.Empty(janeCalendar!.Leaves);
	}

	[Theory]
	[InlineData("/api/calendar?from=2026-12-27&to=2026-12-21")] // fin avant début
	[InlineData("/api/calendar?from=2026-01-01&to=2026-12-31")] // plus de 62 jours
	[InlineData("/api/calendar?from=2026-12-21")]               // to manquant
	public async Task Une_periode_invalide_est_refusee(string url)
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.GetAsync(url);

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	// ─── Absences (R24) ─────────────────────────────────────────────────────

	[Fact]
	public async Task Les_absences_du_jour_sont_triees_avec_leur_etat()
	{
		// « Maintenant » = mercredi 14 octobre 2026, midi
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(Schedule(team, new DateOnly(2026, 10, 1), new TimeOnly(8, 0)));
			db.Absences.AddRange(
				Absence(team, start: "11:30", started: true, LeaveStatus.Approuve),  // urgence en cours
				Absence(team, start: "09:00", started: false, LeaveStatus.Approuve), // rendez-vous terminé
				Absence(team, start: "15:00", started: false, LeaveStatus.EnAttente), // demande
				Absence(team, start: "13:00", started: false, LeaveStatus.Annule));   // annulée
		});
		var sam = await api.LoginAsync("sam@test.local");

		var today = Assert.Single((await sam.GetFromJsonAsync<CalendarResponse>("/api/calendar?from=2026-10-14&to=2026-10-14"))!.Days);

		// Approuvées : triées par heure
		Assert.Equal([new TimeOnly(9, 0), new TimeOnly(11, 30)], today.Absences.Select(a => a.Start));
		Assert.False(today.Absences[0].InProgress);
		Assert.Equal(new TimeOnly(10, 0), today.Absences[0].End);
		Assert.True(today.Absences[1].InProgress);
		Assert.Null(today.Absences[1].End);
		// En attente : affichée à part ; annulée : absente
		var pending = Assert.Single(today.PendingAbsences);
		Assert.Equal(new TimeOnly(15, 0), pending.Start);
		Assert.DoesNotContain(today.Absences.Concat(today.PendingAbsences), a => a.Start == new TimeOnly(13, 0));
	}

	[Fact]
	public async Task Un_jour_de_conge_n_a_pas_d_absences()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.WorkSchedules.Add(Schedule(team, new DateOnly(2026, 10, 1), new TimeOnly(8, 0)));
			db.Absences.Add(Absence(team, start: "09:00", started: false, LeaveStatus.Approuve, date: new DateOnly(2026, 10, 15)));
			db.LeaveRequests.Add(new LeaveRequest
			{
				EmployeeId = team.Sam.Id,
				StartDate = new DateTime(2026, 10, 15),
				EndDate = new DateTime(2026, 10, 15),
				Type = LeaveType.Conge,
				Status = LeaveStatus.Approuve,
			});
		});
		var sam = await api.LoginAsync("sam@test.local");

		var day = Assert.Single((await sam.GetFromJsonAsync<CalendarResponse>("/api/calendar?from=2026-10-15&to=2026-10-15"))!.Days);

		Assert.Equal(DaySource.Conge, day.Source);
		Assert.Empty(day.Absences);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	private static void AssertDay(EffectiveDayResponse day, DaySource source, int? startHour, int? endHour)
	{
		Assert.Equal(source, day.Source);
		Assert.Equal(startHour is null ? null : new TimeOnly(startHour.Value, 0), day.StartTime);
		Assert.Equal(endHour is null ? null : new TimeOnly(endHour.Value, 0), day.EndTime);
	}

	private static ScheduleAdjustment Adjustment(int employeeId, int createdById, int day, int start, int end, LeaveStatus status) => new()
	{
		EmployeeId = employeeId,
		Date = new DateOnly(2026, 12, day),
		StartTime = new TimeOnly(start, 0),
		EndTime = new TimeOnly(end, 0),
		Status = status,
		CreatedById = createdById,
	};

	/// <summary>Une absence d'une heure pour Sam ; « started » = « Je pars » cliqué (urgence, sans heures prévues).</summary>
	private static Absence Absence(TestTeam team, string start, bool started, LeaveStatus status, DateOnly? date = null)
	{
		var startTime = TimeOnly.Parse(start);
		return new Absence
		{
			EmployeeId = team.Sam.Id,
			Date = date ?? new DateOnly(2026, 10, 14),
			PlannedStart = started ? null : startTime,
			PlannedEnd = started ? null : startTime.AddHours(1),
			StartedAt = started ? startTime : null,
			Status = status,
			CreatedById = team.Sam.Id,
		};
	}

	private static WorkSchedule Schedule(TestTeam team, DateOnly effectiveFrom, TimeOnly start) => new()
	{
		EmployeeId = team.Sam.Id,
		EffectiveFrom = effectiveFrom,
		CreatedById = team.Mark.Id,
		Days =
		[
			.. Enum.GetValues<DayOfWeek>()
				.Select(d => new WorkScheduleDay { DayOfWeek = d, StartTime = start, EndTime = new(17, 0), BreakMinutes = 0 }),
		],
	};
}
