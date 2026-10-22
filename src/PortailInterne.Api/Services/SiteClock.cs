using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;

namespace PortailInterne.Api.Services;

public class SiteClock
{
	private readonly AppDbContext _db;
    private readonly IAppClock _clock;

	public SiteClock(AppDbContext db, IAppClock clock)
    {
        _db = db;
        _clock = clock;
    }

	public async Task<DateOnly> TodayForAsync(int employeeId)
	{
		var timeZoneId = await _db.Employees.AsNoTracking().Where(e => e.Id == employeeId).Select(e => e.Site.TimeZoneId).FirstAsync();
		return DateOnly.FromDateTime(_clock.TodayIn(timeZoneId));
	}

	public async Task<DateOnly> TodayForSiteAsync(int siteId)
	{
		var timeZoneId = await _db.Sites.Where(s => s.Id == siteId).Select(s => s.TimeZoneId).FirstAsync();
		return DateOnly.FromDateTime(_clock.TodayIn(timeZoneId));
	}

	/// <summary>La date et l'heure actuelles dans le fuseau du site de l'employé.</summary>
	public async Task<(DateOnly Today, TimeOnly Now)> NowForAsync(int employeeId)
	{
		var timeZoneId = await _db.Employees.AsNoTracking()
			.Where(e => e.Id == employeeId)
			.Select(e => e.Site.TimeZoneId)
			.FirstAsync();
		var now = _clock.NowIn(timeZoneId);
		return (DateOnly.FromDateTime(now), TimeOnly.FromDateTime(now));
	}
}