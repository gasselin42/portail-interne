using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

public class AbsenceNotifier
{
	private readonly AppDbContext _db;

	public AbsenceNotifier(AppDbContext db) { _db = db; }

	public Task NotifyMissedMeetingsAsync(int employeeId, DateTime start, DateTime end, int? actorId) =>
		NotifyAsync(employeeId, start, end, actorId, NotificationType.AbsenceOverlapsMeeting,
			(name, m, when) => $"{name} manquera {MeetingTexts.MissedPart(start, end, m.StartAt, m.EndAt, m.Title)}, {when}.",
			(m, when) => $"Tu manqueras {MeetingTexts.MissedPart(start, end, m.StartAt, m.EndAt, m.Title)}, {when}.");

	public Task NotifyCancelledAsync(int employeeId, DateTime start, DateTime end, int actorId) =>
		NotifyAsync(employeeId, start, end, actorId, NotificationType.AbsenceCancelled,
			(name, m, when) => $"L'absence de {name} pendant « {m.Title} » est annulée, {when}.",
			(m, when) => $"Ton absence pendant « {m.Title} » est annulée, {when}.");

	public async Task NotifyManagersAsync(int employeeId, int? actorId, NotificationType type, string message)
	{
		var employee = await _db.Employees
			.AsNoTracking()
			.Where(e => e.Id == employeeId)
			.Select(e => new { e.ManagerId, e.SiteId })
			.FirstOrDefaultAsync();

		if (employee is null)
			return;

		var manager = await _db.Employees
			.AsNoTracking()
			.Where(e => e.Id == employee.ManagerId)
			.FirstOrDefaultAsync();

		if (manager is not null && manager.IsActive)
		{
			if (actorId != manager.Id)
			{
				_db.Notifications.Add(new Notification
				{
					RecipientId = manager.Id,
					Type = type,
					Message = message,
					Link = "/team",
				});
			}
		}
		else
		{
			var admins = await _db.UserAccounts
				.AsNoTracking()
				.Where(u => u.IsActive && u.Employee.IsActive
					&& u.Role == Role.Admin
					&& u.Employee.SiteId == employee.SiteId
					&& actorId != u.EmployeeId)
				.ToListAsync();
			
			_db.Notifications.AddRange(admins.Select(a => new Notification
			{
				RecipientId = a.EmployeeId,
				Type = type,
				Message = message,
				Link = "/team",
			}));
		}
	}

	public async Task NotifyLeaveAsync(int employeeId, DateTime start, DateTime end, int actorId)
	{
		var employee = await _db.Employees
			.AsNoTracking()
			.Where(e => e.Id == employeeId)
			.Select(e => new { e.FirstName, e.LastName, e.Site.TimeZoneId, e.ManagerId })
			.FirstOrDefaultAsync();

		if (employee is null)
			return;

		var meetings = await LoadMissedMeetingsAsync(employeeId, start, end);
		var employeeZone = TimeZoneInfo.FindSystemTimeZoneById(employee.TimeZoneId);

		// Le manager, seulement s'il est actif (désactiver un compte ne retire pas le ManagerId des subordonnés)
		int? activeManagerId = employee.ManagerId is null
			? null
			: await _db.Employees
				.Where(e => e.Id == employee.ManagerId && e.IsActive)
				.Select(e => (int?)e.Id)
				.FirstOrDefaultAsync();

		foreach (var meeting in meetings)
		{
			if (meeting.OrganizerId != employeeId)
			{
				var organizerZone = TimeZoneInfo.FindSystemTimeZoneById(meeting.OrganizerTimeZoneId);
				_db.Notifications.Add(MeetingTexts.Notification(meeting.OrganizerId, NotificationType.LeaveOverlapsMeeting, meeting.StartAt, organizerZone,
					$"{employee.FirstName} {employee.LastName} sera en congé pendant « {meeting.Title} », {MeetingTexts.FormatWhen(meeting.StartAt, organizerZone)}."));
			}
			else
			{
				var participants = await _db.MeetingParticipants
					.AsNoTracking()
					.Where(p => p.MeetingId == meeting.MeetingId && p.EmployeeId != employeeId)
					.Select(p => new { p.EmployeeId, p.Employee.Site.TimeZoneId })
					.ToListAsync();

				foreach (var participant in participants)
				{
					var participantZone = TimeZoneInfo.FindSystemTimeZoneById(participant.TimeZoneId);
					_db.Notifications.Add(MeetingTexts.Notification(participant.EmployeeId, NotificationType.LeaveOverlapsMeeting, meeting.StartAt, participantZone,
						$"{employee.FirstName} {employee.LastName}, qui organise « {meeting.Title} », sera en congé, {MeetingTexts.FormatWhen(meeting.StartAt, participantZone)}."));
				}

				if (meeting.IsTeamMeeting && activeManagerId is not null && !participants.Any(p => p.EmployeeId == employee.ManagerId))
				{
					_db.Notifications.Add(MeetingTexts.Notification(activeManagerId.Value, NotificationType.LeaveOverlapsMeeting, meeting.StartAt, employeeZone,
						$"{employee.FirstName} {employee.LastName}, qui organise « {meeting.Title} », sera en congé, {MeetingTexts.FormatWhen(meeting.StartAt, employeeZone)}."));
				}
			}

			if (actorId != employeeId)
			{
				_db.Notifications.Add(MeetingTexts.Notification(employeeId, NotificationType.LeaveOverlapsMeeting, meeting.StartAt, employeeZone,
					$"Tu seras en congé pendant « {meeting.Title} », {MeetingTexts.FormatWhen(meeting.StartAt, employeeZone)}."));
			}
		}
	}

	/// <summary>
	/// Pour chaque meeting de groupe que l'employé a pendant [start, end) : prévient l'organisateur (s'il n'est pas l'employé)
	/// et l'employé (si quelqu'un d'autre a agi). Ajoute les notifications sans sauvegarder.
	/// </summary>
	private async Task NotifyAsync(
		int employeeId,
		DateTime start,
		DateTime end,
		int? actorId,
		NotificationType type,
		Func<string, MissedMeeting, string, string> organizerMessage,
		Func<MissedMeeting, string, string> employeeMessage)
	{
		var employee = await _db.Employees
			.AsNoTracking()
			.Where(e => e.Id == employeeId)
			.Select(e => new { e.FirstName, e.LastName, e.Site.TimeZoneId })
			.FirstOrDefaultAsync();

		if (employee is null)
			return;

		var fullName = $"{employee.FirstName} {employee.LastName}";
		var employeeZone = TimeZoneInfo.FindSystemTimeZoneById(employee.TimeZoneId);

		foreach (var meeting in await LoadMissedMeetingsAsync(employeeId, start, end))
		{
			if (meeting.OrganizerId != employeeId)
			{
				var organizerZone = TimeZoneInfo.FindSystemTimeZoneById(meeting.OrganizerTimeZoneId);
				var when = MeetingTexts.FormatWhen(meeting.StartAt, organizerZone);
				_db.Notifications.Add(MeetingTexts.Notification(meeting.OrganizerId, type, meeting.StartAt, organizerZone,
					organizerMessage(fullName, meeting, when)));
			}

			if (actorId != employeeId)
			{
				var when = MeetingTexts.FormatWhen(meeting.StartAt, employeeZone);
				_db.Notifications.Add(MeetingTexts.Notification(employeeId, type, meeting.StartAt, employeeZone,
					employeeMessage(meeting, when)));
			}
		}
	}

	private async Task<List<MissedMeeting>> LoadMissedMeetingsAsync(int employeeId, DateTime start, DateTime end)
	{
		return await _db.MeetingParticipants
			.Where(p => p.EmployeeId == employeeId
				&& p.Meeting.StartAt < end
				&& p.Meeting.EndAt > start
				&& p.Meeting.Kind == MeetingKind.Meeting)
			.Select(p => new MissedMeeting(
				p.Meeting.Title,
				p.Meeting.StartAt,
				p.Meeting.EndAt,
				p.Meeting.OrganizerId,
				p.Meeting.Organizer.Site.TimeZoneId,
				p.MeetingId,
				p.Meeting.IsTeamMeeting
			))
			.ToListAsync();
	}

	private record MissedMeeting(
		string Title,
		DateTime StartAt,
		DateTime EndAt,
		int OrganizerId,
		string OrganizerTimeZoneId,
		int MeetingId,
		bool IsTeamMeeting
	);
}