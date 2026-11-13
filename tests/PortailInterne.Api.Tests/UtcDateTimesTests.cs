using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Tests;

/// <summary>
/// Bonus phase 3 : les instants (<c>CreatedAt</c>, <c>ReviewedAt</c>…) sont enregistrés en UTC,
/// mais SQLite ne garde pas le « Kind ». Une convention les relit comme UTC : le JSON finit par « Z ».
/// Les dates d'un congé (<c>StartDate</c>, <c>EndDate</c>) ne sont pas des instants : jamais de « Z ».
/// </summary>
public class UtcDateTimesTests
{
	[Fact]
	public async Task Un_instant_sort_avec_Z_et_une_date_de_conge_sans()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		api.WithDb(db => db.LeaveRequests.Add(new LeaveRequest
		{
			EmployeeId = team.Sam.Id,
			StartDate = new DateTime(2026, 10, 15),
			EndDate = new DateTime(2026, 10, 16),
			Type = LeaveType.Conge,
			Status = LeaveStatus.EnAttente,
			CreatedAt = new DateTime(2026, 10, 1, 2, 30, 0, DateTimeKind.Utc), // 30 septembre, 22 h 30 à Montréal
		}));
		var sam = await api.LoginAsync("sam@test.local");

		using var json = JsonDocument.Parse(await sam.GetStringAsync("/api/leaves"));
		var leave = json.RootElement.EnumerateArray().Single();

		Assert.Equal("2026-10-01T02:30:00Z", leave.GetProperty("createdAt").GetString());
		Assert.Equal("2026-10-15T00:00:00", leave.GetProperty("startDate").GetString());
		Assert.Equal("2026-10-16T00:00:00", leave.GetProperty("endDate").GetString());
	}

	[Fact]
	public void Tous_les_DateTime_du_modele_sont_UTC_sauf_les_dates_de_conge()
	{
		using var api = new ApiFactory().Start();

		api.WithDb(db =>
		{
			var properties = db.Model.GetEntityTypes()
				.SelectMany(e => e.GetProperties())
				.Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?))
				.ToList();

			Assert.NotEmpty(properties);
			foreach (var p in properties)
			{
				var name = $"{p.DeclaringType.ClrType.Name}.{p.Name}";
				var isLeaveDate = p.DeclaringType.ClrType == typeof(LeaveRequest)
					&& (p.Name == nameof(LeaveRequest.StartDate) || p.Name == nameof(LeaveRequest.EndDate));

				if (isLeaveDate)
					Assert.True(p.GetValueConverter() is null, $"{name} est une date : pas de conversion UTC");
				else
					Assert.True(p.GetValueConverter() is not null, $"{name} doit être relu comme UTC");
			}
		});
	}

	[Fact]
	public void La_lecture_marque_UTC_sans_changer_l_heure()
	{
		using var api = new ApiFactory().Start();
		var team = api.Team;
		var stored = new DateTime(2026, 10, 1, 2, 30, 0, DateTimeKind.Utc);
		api.WithDb(db => db.Notifications.Add(new Notification
		{
			RecipientId = team.Sam.Id,
			Type = NotificationType.MeetingInvitation,
			Message = "Test",
			CreatedAt = stored,
			ReadAt = stored,
		}));

		api.WithDb(db =>
		{
			var n = db.Notifications.AsNoTracking().Single();
			Assert.Equal(DateTimeKind.Utc, n.CreatedAt.Kind);
			Assert.Equal(stored, n.CreatedAt);
			Assert.Equal(DateTimeKind.Utc, n.ReadAt!.Value.Kind);
		});
	}
}
