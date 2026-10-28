using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Tests d'intégration des ajustements d'horaire (R13, R23).
/// « Aujourd'hui » = mercredi 14 octobre 2026 ; Mark est le manager de Sam ; Alex est Admin du site.
/// </summary>
public class AdjustmentsApiTests
{
	private const string Date = "2026-10-20";

	// ─── Création ───────────────────────────────────────────────────────────

	[Fact]
	public async Task La_demande_d_un_employe_est_en_attente()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync(Url(api.Team.Sam), Hours(Date, "08:00:00", "16:00:00"));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		var adjustment = await res.Content.ReadFromJsonAsync<AdjustmentResponse>();
		Assert.Equal(LeaveStatus.EnAttente, adjustment!.Status);
		Assert.True(adjustment.CanCancel);
		Assert.Equal("Sam", adjustment.CreatedByFirstName);
	}

	[Fact]
	public async Task Le_manager_cree_un_ajustement_approuve_pour_son_equipe()
	{
		using var api = new ApiFactory().Start();
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsJsonAsync(Url(api.Team.Sam), Hours(Date, "08:00:00", "16:00:00"));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		Assert.Equal(LeaveStatus.Approuve, (await res.Content.ReadFromJsonAsync<AdjustmentResponse>())!.Status);
	}

	[Fact]
	public async Task Seul_le_manager_retire_une_journee_de_travail()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");

		var bySam = await sam.PostAsJsonAsync(Url(api.Team.Sam), DayOff(Date));
		var byMark = await mark.PostAsJsonAsync(Url(api.Team.Sam), DayOff(Date));

		Assert.Equal(HttpStatusCode.BadRequest, bySam.StatusCode);
		Assert.Equal(HttpStatusCode.Created, byMark.StatusCode);
	}

	[Fact]
	public async Task Un_manager_n_approuve_pas_son_propre_ajustement()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");

		// Alex est Admin, mais c'est sa propre journée
		var res = await alex.PostAsJsonAsync(Url(api.Team.Alex), Hours(Date, "08:00:00", "16:00:00"));

		Assert.Equal(LeaveStatus.EnAttente, (await res.Content.ReadFromJsonAsync<AdjustmentResponse>())!.Status);
	}

	[Fact]
	public async Task Un_ajustement_ne_peut_pas_etre_dans_le_passe()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync(Url(api.Team.Sam), Hours("2026-10-13", "08:00:00", "16:00:00"));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Des_heures_invalides_sont_refusees()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var res = await sam.PostAsJsonAsync(Url(api.Team.Sam), Hours(Date, "16:00:00", "08:00:00"));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Contains("après le début", await res.Content.ReadAsStringAsync());
	}

	[Fact]
	public async Task Un_seul_ajustement_actif_par_date()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var first = await CreateAsync(sam, api.Team.Sam, Hours(Date, "08:00:00", "16:00:00"));

		var duplicate = await sam.PostAsJsonAsync(Url(api.Team.Sam), Hours(Date, "09:00:00", "17:00:00"));
		await sam.PostAsync($"{Url(api.Team.Sam)}/{first.Id}/cancel", null);
		var afterCancel = await sam.PostAsJsonAsync(Url(api.Team.Sam), Hours(Date, "09:00:00", "17:00:00"));

		Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
		Assert.Equal(HttpStatusCode.Created, afterCancel.StatusCode); // l'annulé libère la date
	}

	// ─── Droits ─────────────────────────────────────────────────────────────

	[Fact]
	public async Task Un_collegue_ne_cree_ni_ne_lit_les_ajustements_d_un_autre()
	{
		using var api = new ApiFactory().Start();
		var jane = await api.LoginAsync("jane@test.local");

		var create = await jane.PostAsJsonAsync(Url(api.Team.Sam), Hours(Date, "08:00:00", "16:00:00"));
		var read = await jane.GetAsync(Url(api.Team.Sam));

		Assert.Equal(HttpStatusCode.NotFound, create.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
	}

	[Fact]
	public async Task L_employe_et_son_manager_voient_les_ajustements_a_venir_avec_leur_auteur()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		await CreateAsync(sam, api.Team.Sam, Hours("2026-10-21", "08:00:00", "16:00:00"));
		await CreateAsync(mark, api.Team.Sam, DayOff("2026-10-20"));

		var seenBySam = await sam.GetFromJsonAsync<List<AdjustmentResponse>>(Url(api.Team.Sam));
		var seenByMark = await mark.GetFromJsonAsync<List<AdjustmentResponse>>(Url(api.Team.Sam));

		// Triés par date ; chacun garde son vrai auteur
		Assert.Equal(["Mark", "Sam"], seenBySam!.Select(a => a.CreatedByFirstName));
		Assert.Equal([api.Team.Mark.Id, api.Team.Sam.Id], seenBySam.Select(a => a.CreatedById));
		Assert.Equal(2, seenByMark!.Count);
		// Sam ne peut pas annuler le jour retiré par Mark (approuvé), mais Mark le peut
		Assert.False(seenBySam[0].CanCancel);
		Assert.True(seenByMark[0].CanCancel);
	}

	// ─── Annulation ─────────────────────────────────────────────────────────

	[Fact]
	public async Task L_employe_annule_sa_demande_en_attente()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var created = await CreateAsync(sam, api.Team.Sam, Hours(Date, "08:00:00", "16:00:00"));

		var res = await sam.PostAsync($"{Url(api.Team.Sam)}/{created.Id}/cancel", null);
		var again = await sam.PostAsync($"{Url(api.Team.Sam)}/{created.Id}/cancel", null);

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode); // n'est plus actif
	}

	[Fact]
	public async Task Un_ajustement_approuve_n_est_annule_que_par_le_manager()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		var created = await CreateAsync(mark, api.Team.Sam, Hours(Date, "08:00:00", "16:00:00"));

		var bySam = await sam.PostAsync($"{Url(api.Team.Sam)}/{created.Id}/cancel", null);
		var byMark = await mark.PostAsync($"{Url(api.Team.Sam)}/{created.Id}/cancel", null);

		Assert.Equal(HttpStatusCode.BadRequest, bySam.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, byMark.StatusCode);
	}

	[Fact]
	public async Task Un_ajustement_passe_ne_s_annule_plus()
	{
		using var api = new ApiFactory().Start();
		var adjustmentId = 0;
		api.WithDb(db =>
		{
			var past = new ScheduleAdjustment
			{
				EmployeeId = api.Team.Sam.Id,
				Date = new DateOnly(2026, 10, 13),
				StartTime = new(8, 0),
				EndTime = new(16, 0),
				Status = LeaveStatus.Approuve,
				CreatedById = api.Team.Mark.Id,
			};
			db.ScheduleAdjustments.Add(past);
			db.SaveChanges();
			adjustmentId = past.Id;
		});
		var mark = await api.LoginAsync("mark@test.local");

		var res = await mark.PostAsync($"{Url(api.Team.Sam)}/{adjustmentId}/cancel", null);

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task On_n_annule_pas_l_ajustement_d_un_autre_employe_par_l_url()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");
		var janeAdjustment = await CreateAsync(alex, api.Team.Jane, Hours(Date, "08:00:00", "16:00:00"));

		var res = await alex.PostAsync($"{Url(api.Team.Sam)}/{janeAdjustment.Id}/cancel", null);

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	// ─── Approbation ────────────────────────────────────────────────────────

	[Theory]
	[InlineData(LeaveStatus.Approuve)]
	[InlineData(LeaveStatus.Refuse)]
	public async Task Le_manager_traite_une_demande_en_attente(LeaveStatus decision)
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		var pending = await CreateAsync(sam, api.Team.Sam, Hours(Date, "08:00:00", "16:00:00"));

		var res = await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = decision });

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
		var adjustment = Assert.Single((await sam.GetFromJsonAsync<List<AdjustmentResponse>>(Url(api.Team.Sam)))!);
		Assert.Equal(decision, adjustment.Status);
	}

	[Fact]
	public async Task L_admin_du_site_traite_aussi_la_demande()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var alex = await api.LoginAsync("alex@test.local");
		var pending = await CreateAsync(sam, api.Team.Sam, Hours(Date, "08:00:00", "16:00:00"));

		var res = await alex.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
	}

	[Fact]
	public async Task On_ne_traite_pas_sa_propre_demande()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var pending = await CreateAsync(sam, api.Team.Sam, Hours(Date, "08:00:00", "16:00:00"));

		var res = await sam.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Un_collegue_ne_traite_pas_la_demande()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var jane = await api.LoginAsync("jane@test.local");
		var pending = await CreateAsync(sam, api.Team.Sam, Hours(Date, "08:00:00", "16:00:00"));

		var res = await jane.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	[Theory]
	[InlineData(LeaveStatus.EnAttente)]
	[InlineData(LeaveStatus.Annule)]
	public async Task Le_statut_doit_etre_approuve_ou_refuse(LeaveStatus decision)
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		var pending = await CreateAsync(sam, api.Team.Sam, Hours(Date, "08:00:00", "16:00:00"));

		var res = await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = decision });

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Une_demande_deja_traitee_ne_se_traite_plus()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");
		var mark = await api.LoginAsync("mark@test.local");
		var pending = await CreateAsync(sam, api.Team.Sam, Hours(Date, "08:00:00", "16:00:00"));
		await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Refuse });

		var res = await mark.PostAsJsonAsync(Review(api.Team.Sam, pending.Id), new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
	}

	[Fact]
	public async Task Une_demande_passee_se_refuse_mais_ne_s_approuve_plus()
	{
		using var api = new ApiFactory().Start();
		var adjustmentId = 0;
		api.WithDb(db =>
		{
			var past = new ScheduleAdjustment
			{
				EmployeeId = api.Team.Sam.Id,
				Date = new DateOnly(2026, 10, 13),
				StartTime = new(8, 0),
				EndTime = new(16, 0),
				Status = LeaveStatus.EnAttente,
				CreatedById = api.Team.Sam.Id,
			};
			db.ScheduleAdjustments.Add(past);
			db.SaveChanges();
			adjustmentId = past.Id;
		});
		var mark = await api.LoginAsync("mark@test.local");

		var approve = await mark.PostAsJsonAsync(Review(api.Team.Sam, adjustmentId), new { status = LeaveStatus.Approuve });
		var refuse = await mark.PostAsJsonAsync(Review(api.Team.Sam, adjustmentId), new { status = LeaveStatus.Refuse });

		Assert.Equal(HttpStatusCode.BadRequest, approve.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, refuse.StatusCode);
	}

	[Fact]
	public async Task On_ne_traite_pas_l_ajustement_d_un_autre_employe_par_l_url()
	{
		using var api = new ApiFactory().Start();
		var jane = await api.LoginAsync("jane@test.local");
		var alex = await api.LoginAsync("alex@test.local");
		var janeRequest = await CreateAsync(jane, api.Team.Jane, Hours(Date, "08:00:00", "16:00:00"));

		var res = await alex.PostAsJsonAsync(Review(api.Team.Sam, janeRequest.Id), new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	private static string Url(Employee employee) => $"/api/employees/{employee.Id}/adjustments";

	private static string Review(Employee employee, int id) => $"{Url(employee)}/{id}/review";

	private static object Hours(string date, string start, string end) =>
		new { date, startTime = start, endTime = end, breakMinutes = 30, reason = "Rendez-vous" };

	private static object DayOff(string date) => new { date, breakMinutes = 0 };

	private static async Task<AdjustmentResponse> CreateAsync(HttpClient client, Employee employee, object request)
	{
		var res = await client.PostAsJsonAsync(Url(employee), request);
		res.EnsureSuccessStatusCode();
		return (await res.Content.ReadFromJsonAsync<AdjustmentResponse>())!;
	}
}
