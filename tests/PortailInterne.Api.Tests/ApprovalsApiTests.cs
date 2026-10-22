using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// GET /api/approvals : ajustements et absences prévues en attente à traiter (étape 10).
/// « Maintenant » = mercredi 14 octobre 2026, midi à Montréal.
/// </summary>
public class ApprovalsApiTests
{
	[Fact]
	public async Task Le_manager_voit_les_demandes_de_son_equipe_triees_avec_ce_qu_il_peut_approuver()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.ScheduleAdjustments.AddRange(
				Adjustment(team.Sam, day: 20),
				Adjustment(team.Sam, day: 13)); // passé : à refuser seulement
			db.Absences.AddRange(
				Absence(team.Sam, day: 20, start: 9),
				Absence(team.Sam, day: 14, start: 9),   // aujourd'hui, terminée à 10 h
				Absence(team.Sam, day: 14, start: 13)); // aujourd'hui, pas encore commencée
		});
		var mark = await api.LoginAsync("mark@test.local");

		var queue = (await mark.GetFromJsonAsync<ApprovalQueueResponse>("/api/approvals"))!;

		Assert.Equal([new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 20)], queue.Adjustments.Select(a => a.Date));
		Assert.Equal([false, true], queue.Adjustments.Select(a => a.CanApprove));
		Assert.Equal("Sam", queue.Adjustments[0].EmployeeFirstName);

		Assert.Equal(
			[(14, 9, false), (14, 13, true), (20, 9, true)],
			queue.Absences.Select(a => (a.Date.Day, a.PlannedStart!.Value.Hour, a.CanApprove)));
	}

	[Fact]
	public async Task Seules_les_demandes_en_attente_sont_listees()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.ScheduleAdjustments.AddRange(
				Adjustment(team.Sam, day: 20, LeaveStatus.Approuve),
				Adjustment(team.Sam, day: 21, LeaveStatus.Refuse),
				Adjustment(team.Sam, day: 22, LeaveStatus.Annule));
			db.Absences.AddRange(
				Absence(team.Sam, day: 20, start: 9, LeaveStatus.Approuve),
				Absence(team.Sam, day: 21, start: 9, LeaveStatus.Annule));
		});
		var mark = await api.LoginAsync("mark@test.local");

		var queue = (await mark.GetFromJsonAsync<ApprovalQueueResponse>("/api/approvals"))!;

		Assert.Empty(queue.Adjustments);
		Assert.Empty(queue.Absences);
	}

	[Fact]
	public async Task On_ne_voit_jamais_ses_propres_demandes()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.ScheduleAdjustments.Add(Adjustment(team.Alex, day: 20));
			db.Absences.Add(Absence(team.Alex, day: 20, start: 9));
		});
		var alex = await api.LoginAsync("alex@test.local");
		var gaby = await api.LoginAsync("gaby@test.local");

		var alexQueue = (await alex.GetFromJsonAsync<ApprovalQueueResponse>("/api/approvals"))!;
		var gabyQueue = (await gaby.GetFromJsonAsync<ApprovalQueueResponse>("/api/approvals"))!;

		Assert.Empty(alexQueue.Adjustments);
		Assert.Empty(alexQueue.Absences);
		Assert.Single(gabyQueue.Adjustments); // un autre Admin, lui, la voit
		Assert.Single(gabyQueue.Absences);
	}

	[Fact]
	public async Task Un_employe_sans_equipe_ne_voit_rien()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.ScheduleAdjustments.Add(Adjustment(team.Sam, day: 20));
			db.Absences.Add(Absence(team.Sam, day: 20, start: 9));
		});
		var jane = await api.LoginAsync("jane@test.local");

		var queue = (await jane.GetFromJsonAsync<ApprovalQueueResponse>("/api/approvals"))!;

		Assert.Empty(queue.Adjustments);
		Assert.Empty(queue.Absences);
	}

	[Fact]
	public async Task L_admin_voit_son_site_et_l_admin_global_voit_tout()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			// Jane passe au site de Paris
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Find(team.Jane.Id)!.SiteId = paris.Id;
			db.ScheduleAdjustments.AddRange(Adjustment(team.Sam, day: 20), Adjustment(team.Jane, day: 20));
			db.Absences.AddRange(Absence(team.Sam, day: 20, start: 9), Absence(team.Jane, day: 20, start: 9));
		});
		var alex = await api.LoginAsync("alex@test.local");
		var gaby = await api.LoginAsync("gaby@test.local");

		var alexQueue = (await alex.GetFromJsonAsync<ApprovalQueueResponse>("/api/approvals"))!;
		var gabyQueue = (await gaby.GetFromJsonAsync<ApprovalQueueResponse>("/api/approvals"))!;

		Assert.Equal(["Sam"], alexQueue.Adjustments.Select(a => a.EmployeeFirstName));
		Assert.Equal(["Sam"], alexQueue.Absences.Select(a => a.EmployeeFirstName));
		Assert.Equal(["Jane", "Sam"], gabyQueue.Adjustments.Select(a => a.EmployeeFirstName).Order());
		Assert.Equal(["Jane", "Sam"], gabyQueue.Absences.Select(a => a.EmployeeFirstName).Order());
	}

	[Fact]
	public async Task Les_demandes_d_un_employe_desactive_ne_sont_pas_listees()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db =>
		{
			db.ScheduleAdjustments.Add(Adjustment(team.Sam, day: 20));
			db.Absences.Add(Absence(team.Sam, day: 20, start: 9));
			db.Employees.Find(team.Sam.Id)!.IsActive = false;
		});
		var mark = await api.LoginAsync("mark@test.local");

		var queue = (await mark.GetFromJsonAsync<ApprovalQueueResponse>("/api/approvals"))!;

		Assert.Empty(queue.Adjustments);
		Assert.Empty(queue.Absences);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	/// <summary>Un ajustement de 8 h à 16 h en octobre 2026, demandé par l'employé lui-même.</summary>
	private static ScheduleAdjustment Adjustment(Employee employee, int day, LeaveStatus status = LeaveStatus.EnAttente) => new()
	{
		EmployeeId = employee.Id,
		Date = new DateOnly(2026, 10, day),
		StartTime = new TimeOnly(8, 0),
		EndTime = new TimeOnly(16, 0),
		Status = status,
		CreatedById = employee.Id,
	};

	/// <summary>Une absence prévue d'une heure en octobre 2026, demandée par l'employé lui-même.</summary>
	private static Absence Absence(Employee employee, int day, int start, LeaveStatus status = LeaveStatus.EnAttente) => new()
	{
		EmployeeId = employee.Id,
		Date = new DateOnly(2026, 10, day),
		PlannedStart = new TimeOnly(start, 0),
		PlannedEnd = new TimeOnly(start + 1, 0),
		Status = status,
		CreatedById = employee.Id,
	};
}
