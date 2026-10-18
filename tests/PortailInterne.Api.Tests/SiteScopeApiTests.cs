using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Portée par site (R23) : un Admin ne gère que son site ; l'Admin global gère tout.
/// Montréal (site 1) : Alex (Admin), Gaby (Admin global), Mark (manager de Sam), Sam, Jane.
/// Paris : Pierre (Admin) et Paul (employé), ajoutés par <see cref="AddParis"/>.
/// </summary>
public class SiteScopeApiTests
{
	// ─── Comptes et fiches ──────────────────────────────────────────────────

	[Fact]
	public async Task Un_admin_ne_voit_que_les_comptes_de_son_site()
	{
		using var api = new ApiFactory().Start();
		AddParis(api);
		var pierre = await api.LoginAsync("pierre@test.local");
		var gaby = await api.LoginAsync("gaby@test.local");

		var parisAccounts = await pierre.GetFromJsonAsync<List<AccountListItemResponse>>("/api/admin/accounts");
		var allAccounts = await gaby.GetFromJsonAsync<List<AccountListItemResponse>>("/api/admin/accounts");

		Assert.Equal(["Paul", "Pierre"], parisAccounts!.Select(a => a.FirstName).Order());
		Assert.Equal(7, allAccounts!.Count);
	}

	[Fact]
	public async Task Un_admin_d_un_autre_site_ne_gere_pas_l_employe()
	{
		using var api = new ApiFactory().Start();
		AddParis(api);
		var samAccountId = AccountIdOf(api, api.Team.Sam.Id);
		var pierre = await api.LoginAsync("pierre@test.local");

		var fiche = await pierre.GetAsync($"/api/admin/employees/{api.Team.Sam.Id}");
		var update = await pierre.PatchAsync($"/api/admin/employees/{api.Team.Sam.Id}", EmployeeForm("Sam", siteId: 1, role: Role.Employee));
		var deactivate = await pierre.PatchAsJsonAsync($"/api/admin/accounts/{samAccountId}", new { isActive = false });
		var reset = await pierre.PostAsync($"/api/admin/accounts/{samAccountId}/reset-password", null);

		Assert.Equal(HttpStatusCode.NotFound, fiche.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, deactivate.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, reset.StatusCode);
	}

	[Fact]
	public async Task Un_admin_ne_touche_pas_au_compte_d_un_admin_global_de_son_site()
	{
		using var api = new ApiFactory().Start();
		var gabyAccountId = AccountIdOf(api, api.Team.Gaby.Id);
		var alex = await api.LoginAsync("alex@test.local");

		var fiche = await alex.GetAsync($"/api/admin/employees/{api.Team.Gaby.Id}");
		var reset = await alex.PostAsync($"/api/admin/accounts/{gabyAccountId}/reset-password", null);

		Assert.Equal(HttpStatusCode.NotFound, fiche.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, reset.StatusCode);
	}

	// ─── Rôle Admin global ──────────────────────────────────────────────────

	[Fact]
	public async Task Seul_un_admin_global_attribue_le_role_admin_global()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");
		var gaby = await api.LoginAsync("gaby@test.local");

		var byAdmin = await alex.PostAsync("/api/admin/employees", EmployeeForm("Nina", siteId: 1, role: Role.GlobalAdmin));
		var byGlobalAdmin = await gaby.PostAsync("/api/admin/employees", EmployeeForm("Nina", siteId: 1, role: Role.GlobalAdmin));

		Assert.Equal(HttpStatusCode.BadRequest, byAdmin.StatusCode);
		Assert.Equal(HttpStatusCode.Created, byGlobalAdmin.StatusCode);
	}

	// ─── Création et manager ────────────────────────────────────────────────

	[Fact]
	public async Task Un_admin_cree_toujours_sur_son_site()
	{
		using var api = new ApiFactory().Start();
		var paris = AddParis(api);
		var pierre = await api.LoginAsync("pierre@test.local");

		// Pierre (Paris) demande Montréal : ignoré
		var res = await pierre.PostAsync("/api/admin/employees", EmployeeForm("Nina", siteId: 1, role: Role.Employee));

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
		var created = await res.Content.ReadFromJsonAsync<CreateEmployeeResponse>();
		Assert.Equal(paris.SiteId, SiteIdOf(api, created!.EmployeeId));
	}

	[Fact]
	public async Task Le_manager_doit_etre_du_meme_site()
	{
		using var api = new ApiFactory().Start();
		var paris = AddParis(api);
		var gaby = await api.LoginAsync("gaby@test.local");

		var res = await gaby.PostAsync("/api/admin/employees",
			EmployeeForm("Nina", siteId: 1, role: Role.Employee, managerId: paris.PaulId));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Contains("même site", await res.Content.ReadAsStringAsync());
	}

	[Fact]
	public async Task La_recherche_d_employes_peut_etre_limitee_a_un_site()
	{
		using var api = new ApiFactory().Start();
		var paris = AddParis(api);
		var gaby = await api.LoginAsync("gaby@test.local");

		var result = await gaby.GetFromJsonAsync<EmployeeLookupResponse>($"/api/employees/lookup?search=Test&siteId={paris.SiteId}");

		Assert.Equal(["Paul", "Pierre"], result!.Items.Select(e => e.FirstName).Order());
	}

	// ─── Changement de site ─────────────────────────────────────────────────

	[Fact]
	public async Task Seul_un_admin_global_change_le_site_d_un_employe()
	{
		using var api = new ApiFactory().Start();
		var paris = AddParis(api);
		var alex = await api.LoginAsync("alex@test.local");

		var res = await alex.PatchAsync($"/api/admin/employees/{api.Team.Jane.Id}",
			EmployeeForm("Jane", siteId: paris.SiteId, role: Role.Employee));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Equal(1, SiteIdOf(api, api.Team.Jane.Id));
	}

	[Fact]
	public async Task Un_employe_avec_un_manager_change_de_site_seulement_sans_manager()
	{
		using var api = new ApiFactory().Start();
		var paris = AddParis(api);
		var gaby = await api.LoginAsync("gaby@test.local");
		var url = $"/api/admin/employees/{api.Team.Sam.Id}";

		var keepManager = await gaby.PatchAsync(url, EmployeeForm("Sam", siteId: paris.SiteId, role: Role.Employee, managerId: api.Team.Mark.Id));
		var removeManager = await gaby.PatchAsync(url, EmployeeForm("Sam", siteId: paris.SiteId, role: Role.Employee));

		Assert.Equal(HttpStatusCode.BadRequest, keepManager.StatusCode);
		Assert.Equal(HttpStatusCode.NoContent, removeManager.StatusCode);
		Assert.Equal(paris.SiteId, SiteIdOf(api, api.Team.Sam.Id));
	}

	[Fact]
	public async Task Un_manager_avec_des_subordonnes_ne_change_pas_de_site()
	{
		using var api = new ApiFactory().Start();
		var paris = AddParis(api);
		var gaby = await api.LoginAsync("gaby@test.local");

		var res = await gaby.PatchAsync($"/api/admin/employees/{api.Team.Mark.Id}",
			EmployeeForm("Mark", siteId: paris.SiteId, role: Role.Employee));

		Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
		Assert.Contains("réassigne", await res.Content.ReadAsStringAsync());
	}

	// ─── Congés, horaires, fériés ───────────────────────────────────────────

	[Fact]
	public async Task Un_admin_d_un_autre_site_n_approuve_pas_et_ne_declare_pas()
	{
		using var api = new ApiFactory().Start();
		AddParis(api);
		var sam = await api.LoginAsync("sam@test.local");
		var pierre = await api.LoginAsync("pierre@test.local");
		var leave = await (await sam.PostAsJsonAsync("/api/leaves",
			new { startDate = "2026-11-16", endDate = "2026-11-20", type = LeaveType.Conge }))
			.Content.ReadFromJsonAsync<LeaveRequestResponse>();

		var pending = await pierre.GetFromJsonAsync<List<LeaveRequestResponse>>("/api/leaves/pending");
		var review = await pierre.PostAsJsonAsync($"/api/leaves/{leave!.Id}/review", new { status = LeaveStatus.Approuve });
		var declare = await pierre.PostAsJsonAsync("/api/leaves/declare",
			new { employeeId = api.Team.Sam.Id, startDate = "2026-10-14", endDate = "2026-10-14" });

		Assert.DoesNotContain(pending!, l => l.Id == leave.Id);
		Assert.Equal(HttpStatusCode.NotFound, review.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, declare.StatusCode);
	}

	[Fact]
	public async Task L_admin_global_approuve_sur_tous_les_sites()
	{
		using var api = new ApiFactory().Start();
		var paris = AddParis(api);
		var paul = await api.LoginAsync("paul@test.local");
		var gaby = await api.LoginAsync("gaby@test.local");
		var leave = await (await paul.PostAsJsonAsync("/api/leaves",
			new { startDate = "2026-11-16", endDate = "2026-11-20", type = LeaveType.Conge }))
			.Content.ReadFromJsonAsync<LeaveRequestResponse>();

		var res = await gaby.PostAsJsonAsync($"/api/leaves/{leave!.Id}/review", new { status = LeaveStatus.Approuve });

		Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
	}

	[Fact]
	public async Task Un_admin_d_un_autre_site_ne_gere_pas_les_horaires()
	{
		using var api = new ApiFactory().Start();
		AddParis(api);
		var pierre = await api.LoginAsync("pierre@test.local");

		var res = await pierre.PostAsJsonAsync($"/api/employees/{api.Team.Sam.Id}/schedules", new
		{
			effectiveFrom = "2026-11-02",
			days = new[] { new { dayOfWeek = DayOfWeek.Monday, startTime = "08:00:00", endTime = "17:00:00", breakMinutes = 30 } },
		});

		Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
	}

	[Fact]
	public async Task L_admin_global_gere_les_feries_de_tous_les_sites()
	{
		using var api = new ApiFactory().Start();
		var paris = AddParis(api);
		var gaby = await api.LoginAsync("gaby@test.local");

		var res = await gaby.PostAsJsonAsync($"/api/sites/{paris.SiteId}/holidays", new { date = "2026-11-11", name = "Armistice" });

		Assert.Equal(HttpStatusCode.Created, res.StatusCode);
	}

	// ─── Sites (fuseau IANA) ────────────────────────────────────────────────

	[Fact]
	public async Task L_admin_global_cree_un_site_avec_un_fuseau_iana()
	{
		using var api = new ApiFactory().Start();
		var gaby = await api.LoginAsync("gaby@test.local");

		var valid = await gaby.PostAsJsonAsync("/api/sites", new { name = " Vancouver ", timeZoneId = "America/Vancouver" });
		var unknownZone = await gaby.PostAsJsonAsync("/api/sites", new { name = "Atlantide", timeZoneId = "Atlantis/Capital" });
		var duplicate = await gaby.PostAsJsonAsync("/api/sites", new { name = "vancouver", timeZoneId = "America/Vancouver" });

		Assert.Equal(HttpStatusCode.Created, valid.StatusCode);
		Assert.Equal("Vancouver", (await valid.Content.ReadFromJsonAsync<Site>())!.Name);
		Assert.Equal(HttpStatusCode.BadRequest, unknownZone.StatusCode);
		Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
	}

	[Fact]
	public async Task Un_admin_ne_cree_ni_ne_modifie_de_site()
	{
		using var api = new ApiFactory().Start();
		var alex = await api.LoginAsync("alex@test.local");
		var gaby = await api.LoginAsync("gaby@test.local");

		var create = await alex.PostAsJsonAsync("/api/sites", new { name = "Vancouver", timeZoneId = "America/Vancouver" });
		var update = await alex.PutAsJsonAsync("/api/sites/1", new { name = "Montréal", timeZoneId = "America/Toronto" });
		var unknown = await gaby.PutAsJsonAsync("/api/sites/99", new { name = "Nulle part", timeZoneId = "America/Toronto" });

		Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
		Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	private sealed record Paris(int SiteId, int PierreId, int PaulId);

	/// <summary>Ajoute le site Paris, avec Pierre (Admin) et Paul (employé).</summary>
	private static Paris AddParis(ApiFactory api)
	{
		Paris? result = null;
		api.WithDb(db =>
		{
			var site = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(site);
			db.SaveChanges();

			var passwords = new Services.PasswordService();
			Employee Create(string firstName, Role role) => new()
			{
				FirstName = firstName,
				LastName = "Test",
				Email = $"{firstName.ToLowerInvariant()}@test.local",
				Departement = Departement.Ventes,
				SiteId = site.Id,
				AnnualLeaveDays = 20,
				IsActive = true,
				CreatedAt = DateTime.UtcNow,
				UserAccount = new UserAccount
				{
					Email = $"{firstName.ToLowerInvariant()}@test.local",
					PasswordHash = passwords.Hash(ApiFactory.Password),
					Role = role,
					MustChangePassword = false,
					IsActive = true,
					CreatedAt = DateTime.UtcNow,
				},
			};

			var pierre = Create("Pierre", Role.Admin);
			var paul = Create("Paul", Role.Employee);
			db.Employees.AddRange(pierre, paul);
			db.SaveChanges();
			result = new Paris(site.Id, pierre.Id, paul.Id);
		});
		return result!;
	}

	/// <summary>Formulaire multipart des endpoints admin (création et modification d'un employé).</summary>
	private static MultipartFormDataContent EmployeeForm(string firstName, int siteId, Role role, int? managerId = null)
	{
		var form = new MultipartFormDataContent
		{
			{ new StringContent(firstName), "firstName" },
			{ new StringContent("Test"), "lastName" },
			{ new StringContent($"{firstName.ToLowerInvariant()}.form@test.local"), "email" },
			{ new StringContent(((int)Departement.TI).ToString()), "departement" },
			{ new StringContent(((int)role).ToString()), "role" },
			{ new StringContent("20"), "annualLeaveDays" },
			{ new StringContent(siteId.ToString()), "siteId" },
		};
		if (managerId is not null)
			form.Add(new StringContent(managerId.Value.ToString()), "managerId");
		return form;
	}

	private static int AccountIdOf(ApiFactory api, int employeeId)
	{
		var id = 0;
		api.WithDb(db => id = db.UserAccounts.Single(u => u.EmployeeId == employeeId).Id);
		return id;
	}

	private static int SiteIdOf(ApiFactory api, int employeeId)
	{
		var id = 0;
		api.WithDb(db => id = db.Employees.Single(e => e.Id == employeeId).SiteId);
		return id;
	}
}
