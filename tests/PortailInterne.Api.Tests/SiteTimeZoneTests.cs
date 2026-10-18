using System.Net;
using System.Net.Http.Json;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>« Aujourd'hui » se calcule dans le fuseau du site de chaque employé (R15).</summary>
public class SiteTimeZoneTests
{
	// Mercredi 14 octobre 2026, 23 h 30 UTC :
	// encore le 14 à Montréal (19 h 30), déjà le 15 à Paris (1 h 30).
	private static readonly DateTimeOffset LateEvening = new(2026, 10, 14, 23, 30, 0, TimeSpan.Zero);

	[Fact]
	public async Task Aujourd_hui_depend_du_site_de_l_employe()
	{
		using var api = new ApiFactory(LateEvening).Start();
		api.WithDb(db =>
		{
			var paris = new Site { Name = "Paris", TimeZoneId = "Europe/Paris" };
			db.Sites.Add(paris);
			db.SaveChanges();
			db.Employees.Single(e => e.Id == api.Team.Jane.Id).SiteId = paris.Id;
		});
		var sam = await api.LoginAsync("sam@test.local");   // Montréal
		var jane = await api.LoginAsync("jane@test.local"); // Paris
		var leave = new { startDate = "2026-10-14", endDate = "2026-10-14", type = LeaveType.Conge };

		var forSam = await sam.PostAsJsonAsync("/api/leaves", leave);
		var forJane = await jane.PostAsJsonAsync("/api/leaves", leave);

		Assert.Equal(HttpStatusCode.Created, forSam.StatusCode);     // le 14 est encore aujourd'hui pour Sam
		Assert.Equal(HttpStatusCode.BadRequest, forJane.StatusCode); // le 14 est déjà passé pour Jane
	}

	[Fact]
	public async Task Me_renvoie_le_fuseau_du_site()
	{
		using var api = new ApiFactory().Start();
		var sam = await api.LoginAsync("sam@test.local");

		var me = await sam.GetFromJsonAsync<Dtos.MeResponse>("/api/auth/me");

		Assert.Equal("America/Toronto", me!.SiteTimeZoneId);
	}
}
