using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

public class AbsenceWatcher
{
	private readonly AppDbContext _db;
	private readonly SiteClock _clock;
	private readonly EffectiveScheduleService _schedule;
	private readonly AbsenceNotifier _notifier;
	private readonly TimeProvider _time;

	public AbsenceWatcher(AppDbContext db, SiteClock clock, EffectiveScheduleService schedule, AbsenceNotifier notifier, TimeProvider time)
	{
		_db = db;
		_clock = clock;
		_schedule = schedule;
		_notifier = notifier;
		_time = time;
	}

	public async Task RunOnceAsync()
	{
		var yesterday = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime).AddDays(-1);
		var absences = await _db.Absences
			.Include(a => a.Employee)
			.Where(a => a.Status == LeaveStatus.Approuve
				&& a.StartedAt != null && a.EndedAt == null
				&& (!a.LateReturnNotified || !a.UnconfirmedReturnNotified)
				&& a.Date >= yesterday)
			.ToListAsync();

		foreach (var absence in absences)
		{
			var (today, now) = await _clock.NowForAsync(absence.EmployeeId);
			var zone = await _clock.TimeZoneForAsync(absence.Employee.Id);
			var day = (await _schedule.ForAsync(absence.EmployeeId, absence.Date, absence.Date))[0];
			var dayOver = absence.Date < today || (day.EndTime != null && now >= day.EndTime.Value);

			if (!absence.LateReturnNotified && absence.PlannedEnd is not null && (dayOver || now >= absence.PlannedEnd))
			{
				if (!dayOver && day.EndTime != null)
					await _notifier.NotifyMissedMeetingsAsync(absence.Employee.Id, MeetingTime.ToUtc(today, now, zone), MeetingTime.ToUtc(day.Date, day.EndTime.Value, zone), null);
				absence.LateReturnNotified = true;
			}

			if (!absence.UnconfirmedReturnNotified && dayOver)
			{
				var date = absence.Date.ToString("dddd d MMMM", new CultureInfo("fr-CA"));
				await _notifier.NotifyManagersAsync(absence.Employee.Id, null, NotificationType.AbsenceReturnUnconfirmed, $"{absence.Employee.FirstName} {absence.Employee.LastName} n'a pas confirmé son retour, {date}.");
				absence.UnconfirmedReturnNotified = true;
			}
		}

		await _db.SaveChangesAsync();
	}
}