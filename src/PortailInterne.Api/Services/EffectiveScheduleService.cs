using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

/// <summary>
/// Horaire effectif d'un employé sur une période (R16) : charge horaires, fériés, ajustements
/// et congés en une seule fois, puis calcule chaque jour avec <see cref="EffectiveSchedule.ComputeDay"/>.
/// </summary>
public class EffectiveScheduleService
{
	private readonly AppDbContext _db;

	public EffectiveScheduleService(AppDbContext db)
	{
		_db = db;
	}

	/// <summary>Un <see cref="EffectiveDay"/> par jour, de <paramref name="from"/> à <paramref name="to"/> inclus.</summary>
	public async Task<List<EffectiveDay>> ForAsync(int employeeId, DateOnly from, DateOnly to, bool applyLeaves = true)
	{
		var siteId = await _db.Employees
			.Where(e => e.Id == employeeId)
			.Select(e => e.SiteId)
			.FirstAsync();

		var schedules = await _db.WorkSchedules
			.AsNoTracking()
			.Include(s => s.Days)
			.Where(s => s.EmployeeId == employeeId && s.EffectiveFrom <= to)
			.ToListAsync();

		var absences = await _db.Absences
			.AsNoTracking()
			.Where(a => a.EmployeeId == employeeId
				&& a.Date >= from && a.Date <= to
				&& (a.Status == LeaveStatus.Approuve || a.Status == LeaveStatus.EnAttente))
			.ToListAsync();

		var holidays = await _db.Holidays
			.AsNoTracking()
			.Where(h => h.SiteId == siteId && h.Date >= from && h.Date <= to)
			.ToListAsync();

		var adjustments = await _db.ScheduleAdjustments
			.AsNoTracking()
			.Where(a => a.EmployeeId == employeeId
				&& a.Date >= from && a.Date <= to
				&& (a.Status == LeaveStatus.Approuve || a.Status == LeaveStatus.EnAttente))
			.ToListAsync();

		List<LeaveRequest> leaves = !applyLeaves ? [] : await _db.LeaveRequests
			.AsNoTracking()
			.Where(l => l.EmployeeId == employeeId
				&& l.Status == LeaveStatus.Approuve
				&& DateOnly.FromDateTime(l.StartDate) <= to
				&& DateOnly.FromDateTime(l.EndDate) >= from)
			.ToListAsync();

		var result = new List<EffectiveDay>();

		for (var date = from; date <= to; date = date.AddDays(1))
		{
			// L'horaire en vigueur ce jour-là : le plus récent déjà commencé
			var schedule = schedules
				.Where(s => s.EffectiveFrom <= date)
				.OrderByDescending(s => s.EffectiveFrom)
				.FirstOrDefault();

			var holiday = holidays.FirstOrDefault(h => h.Date == date);
			var approved = adjustments.FirstOrDefault(a => a.Date == date && a.Status == LeaveStatus.Approuve);
			var pending = adjustments.FirstOrDefault(a => a.Date == date && a.Status == LeaveStatus.EnAttente);
			var onLeave = leaves.Any(l =>
				DateOnly.FromDateTime(l.StartDate) <= date && DateOnly.FromDateTime(l.EndDate) >= date);

			var day = EffectiveSchedule.ComputeDay(date, schedule, holiday, approved, pending, onLeave);

			// Les absences du jour, triées ; approuvées = effectives, en attente = affichées seulement
			var todays = absences
				.Where(a => a.Date == date)
				.OrderBy(a => a.StartedAt ?? a.PlannedStart)
				.ToList();

			result.Add(day with
			{
				Absences = onLeave ? [] : todays.Where(a => a.Status == LeaveStatus.Approuve).ToList(),
				PendingAbsences = onLeave ? [] : todays.Where(a => a.Status == LeaveStatus.EnAttente).ToList(),
			});
		}

		return result;
	}

	public async Task<HashSet<DateOnly>> WorkedDaysAsync(int employeeId, DateOnly from, DateOnly to)
	{
		return (await ForAsync(employeeId, from, to, applyLeaves: false))
				.Where(d => d.StartTime is not null || (d.Source == DaySource.NonDefini
				&& d.Date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)))
			.Select(d => d.Date)
			.ToHashSet();
	}
}
