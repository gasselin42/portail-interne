using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Étape 14 : qui peut lire et gérer l'horaire, les ajustements et les absences de Sam.
/// Employé = lui-même en lecture ; manager = son équipe ; Admin = son site ; Admin global = tout.
/// Pierre est Admin du site de Paris ; Sam est à Montréal.
/// </summary>
public class AuthorizationMatrixTests
{
	private const string Future = "2026-11-02";

	[Theory]
	[InlineData("sam@test.local", true)]     // lui-même
	[InlineData("mark@test.local", true)]    // son manager
	[InlineData("alex@test.local", true)]    // Admin de son site
	[InlineData("gaby@test.local", true)]    // Admin global
	[InlineData("jane@test.local", false)]   // collègue
	[InlineData("pierre@test.local", false)] // Admin d'un autre site
	public async Task Lecture(string caller, bool allowed)
	{
		using var api = Start();
		var client = await api.LoginAsync(caller);
		var expected = allowed ? HttpStatusCode.OK : HttpStatusCode.NotFound;

		foreach (var resource in new[] { "schedules", "adjustments", "absences" })
		{
			var res = await client.GetAsync($"/api/employees/{api.Team.Sam.Id}/{resource}");
			Assert.True(expected == res.StatusCode, $"{resource} : {res.StatusCode}");
		}
	}

	[Theory]
	[InlineData("sam@test.local", false)]    // pas son propre horaire
	[InlineData("mark@test.local", true)]
	[InlineData("alex@test.local", true)]
	[InlineData("gaby@test.local", true)]
	[InlineData("jane@test.local", false)]
	[InlineData("pierre@test.local", false)]
	public async Task Gestion_de_l_horaire(string caller, bool allowed)
	{
		using var api = Start();
		var client = await api.LoginAsync(caller);

		var res = await client.PostAsJsonAsync($"/api/employees/{api.Team.Sam.Id}/schedules", new
		{
			effectiveFrom = Future,
			days = new[] { new { dayOfWeek = DayOfWeek.Monday, startTime = "08:00:00", endTime = "17:00:00", breakMinutes = 30 } },
		});

		Assert.Equal(allowed ? HttpStatusCode.Created : HttpStatusCode.NotFound, res.StatusCode);
	}

	[Theory]
	[InlineData("mark@test.local", true)]
	[InlineData("alex@test.local", true)]
	[InlineData("gaby@test.local", true)]
	[InlineData("jane@test.local", false)]
	[InlineData("pierre@test.local", false)]
	public async Task Approbation_d_un_ajustement_et_d_un_conge(string caller, bool allowed)
	{
		using var api = Start();
		var sam = await api.LoginAsync("sam@test.local");
		var adjustment = await (await sam.PostAsJsonAsync($"/api/employees/{api.Team.Sam.Id}/adjustments",
				new { date = Future, startTime = "09:00:00", endTime = "15:00:00", breakMinutes = 30 }))
			.Content.ReadFromJsonAsync<Dtos.AdjustmentResponse>();
		var leave = await (await sam.PostAsJsonAsync("/api/leaves",
				new { startDate = Future, endDate = Future, type = LeaveType.Conge }))
			.Content.ReadFromJsonAsync<Dtos.LeaveRequestResponse>();
		var client = await api.LoginAsync(caller);
		var expected = allowed ? HttpStatusCode.NoContent : HttpStatusCode.NotFound;

		var adjustmentReview = await client.PostAsJsonAsync(
			$"/api/employees/{api.Team.Sam.Id}/adjustments/{adjustment!.Id}/review", new { status = LeaveStatus.Approuve });
		var leaveReview = await client.PostAsJsonAsync($"/api/leaves/{leave!.Id}/review", new { status = LeaveStatus.Approuve });

		Assert.Equal(expected, adjustmentReview.StatusCode);
		Assert.Equal(expected, leaveReview.StatusCode);
	}

	// ─── Aides ──────────────────────────────────────────────────────────────

	/// <summary>L'équipe de test, plus Pierre, Admin du site de Paris.</summary>
	private static ApiFactory Start()
	{
		var api = new ApiFactory().Start();
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Add(new Employee
			{
				FirstName = "Pierre",
				LastName = "Test",
				Email = "pierre@test.local",
				Departement = Departement.Ventes,
				SiteId = paris.Id,
				AnnualLeaveDays = 20,
				IsActive = true,
				CreatedAt = DateTime.UtcNow,
				UserAccount = new UserAccount
				{
					Email = "pierre@test.local",
					PasswordHash = new Services.PasswordService().Hash(ApiFactory.Password),
					Role = Role.Admin,
					MustChangePassword = false,
					IsActive = true,
					CreatedAt = DateTime.UtcNow,
				},
			});
		});
		return api;
	}
}
