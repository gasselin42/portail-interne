using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Services;
using PortailInterne.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace PortailInterne.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/meetings")]
public class MeetingsController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly AccessScope _scope;
	private readonly TimeProvider _time;
	private readonly EffectiveScheduleService _schedule;

	public MeetingsController(AppDbContext db, AccessScope scope, TimeProvider time, EffectiveScheduleService schedule)
	{
		_db = db;
		_scope = scope;
		_time = time;
		_schedule = schedule;
	}

	[HttpPost]
	public async Task<ActionResult<MeetingResponse>> Create(CreateMeetingRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Title))
			return BadRequest(new { message = "Le titre est obligatoire." });

		var title = request.Title.Trim();
		var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

		var (participants, participantsError) = await LoadParticipantsAsync(request);
		if (participantsError is not null)
			return BadRequest(new { message = participantsError });

		if (!IsValidColor(request.Color))
			return BadRequest(new { message = "Couleur inconnue." });

		var organizer = await _db.Employees
			.Where(e => e.Id == _scope.CallerEmployeeId)
			.Select(e => new PersonInfo(e.Id, e.FirstName, e.LastName, e.Site.TimeZoneId))
			.FirstAsync();

		var start = request.StartAt!.Value;
		var end = request.EndAt!.Value;

		var timesError = ValidateTimes(start, end, organizer.Zone);
		if (timesError is not null)
			return BadRequest(new { message = timesError });

		var leaveError = await FindLeaveErrorAsync(start, end, organizer, participants);
		if (leaveError is not null)
			return BadRequest(new { message = leaveError });
		
		var isTeamMeeting = request.Kind == MeetingKind.Meeting && (_scope.IsAdmin || await _scope.HasSubordinatesAsync());

		List<PersonInfo> everyone = [organizer, .. participants];

		var conflicts = await FindConflictsAsync(everyone, start, end, isTeamMeeting);
		var outsideHours = await FindOutsideHoursAsync(participants, start, end);

		if ((conflicts.Count > 0 || outsideHours.Count > 0) && !request.ConfirmConflicts)
		{
			List<string> messages = [];

			if (conflicts.Count > 0)
				messages.Add($"Conflit d'horaire pour {conflicts.Count} {(conflicts.Count == 1 ? "personne" : "personnes")}.");

			if (outsideHours.Count > 0)
				messages.Add($"{outsideHours.Count} {(outsideHours.Count == 1 ? "personne est hors de son" : "personnes sont hors de leur")} horaire.");

			return Conflict(new MeetingConflictResponse
			{
				Message = string.Join(" ", messages),
				Conflicts = conflicts,
				OutsideHours = outsideHours
			});
		}

		var meeting = new Meeting
		{
			Kind = request.Kind!.Value,
			Title = title,
			Description = description,
			StartAt = start,
			EndAt = end,
			OrganizerId = _scope.CallerEmployeeId,
			IsTeamMeeting = isTeamMeeting,
			IsFlexible = request.IsFlexible,
			Participants = [
				new MeetingParticipant { EmployeeId = _scope.CallerEmployeeId, Color = request.Color },
				.. participants.Select(p => new MeetingParticipant { EmployeeId = p.Id }),
			]
		};

		_db.Meetings.Add(meeting);

		AddInvitations(meeting, organizer, participants);

		await _db.SaveChangesAsync();

		return Created($"api/meetings/{meeting.Id}", ToResponse(meeting, request.Color));
	}

	[HttpPut("{id:int}/color")]
	public async Task<IActionResult> ChangeColor(int id, SaveMeetingColorRequest request)
	{
		if (!IsValidColor(request.Color))
			return BadRequest(new { message = "Couleur inconnue." });
		
		var participant = await _db.MeetingParticipants
			.FirstOrDefaultAsync(p => p.MeetingId == id && p.EmployeeId == _scope.CallerEmployeeId);

		if (participant is null)
			return NotFound();

		participant.Color = request.Color;
		await _db.SaveChangesAsync();

		return NoContent();
	}

	private async Task<(List<PersonInfo> Participants, string? Error)> LoadParticipantsAsync(CreateMeetingRequest request)
	{	
		if (request.Kind == MeetingKind.PersonalBlock)
		{
			if (request.ParticipantIds.Count > 0)
				return ([], "Un bloc personnel n'a pas de participants.");
			return ([], null);
		}
		
		var participantsIds = request.ParticipantIds.Where(p => p != _scope.CallerEmployeeId).Distinct().ToList();
		if (participantsIds.Count == 0)
			return ([], "Ajoute au moins un participant.");

		if (request.IsFlexible)
			return ([], "Seul un bloc personnel peut être souple.");

		var participants = await _db.Employees
			.Where(e => participantsIds.Contains(e.Id) && e.IsActive)
			.Select(e => new PersonInfo(e.Id, e.FirstName, e.LastName, e.Site.TimeZoneId))
			.ToListAsync();

		if (participants.Count != participantsIds.Count)
			return ([], "Un des participants est introuvable ou inactif.");

		return (participants, null);
	}

	private string? ValidateTimes(DateTime start, DateTime end, TimeZoneInfo zone)
	{
		if (start.Kind != DateTimeKind.Utc || end.Kind != DateTimeKind.Utc)
			return "Les heures doivent inclure leur fuseau (UTC).";
		
		if (end <= start)
			return "La fin doit être après le début.";

		if (!IsOnSlot(start) || !IsOnSlot(end))
			return "Les heures vont par tranche de 15 minutes.";
		
		var localStart = TimeZoneInfo.ConvertTimeFromUtc(start, zone);
		var localEnd = TimeZoneInfo.ConvertTimeFromUtc(end, zone);

		var sameDay = localStart.Date == localEnd.Date
			|| (localEnd.TimeOfDay == TimeSpan.Zero && localEnd.Date == localStart.Date.AddDays(1));

		if (!sameDay)
			return "Un événement doit commencer et finir le même jour.";
		
		var now = _time.GetUtcNow().UtcDateTime;
		var currentSlot = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute - now.Minute % 15, 0, DateTimeKind.Utc);

		if (start < currentSlot)
			return "Cet horaire est déjà passé.";

		return null;
	}

	private async Task<string?> FindLeaveErrorAsync(DateTime start, DateTime end, PersonInfo organizer, List<PersonInfo> participants)
	{
		var everyoneIds = participants.Select(p => p.Id).Append(organizer.Id).ToList();

		var leaves = await _db.LeaveRequests
			.Where(l => l.Status == LeaveStatus.Approuve
				&& l.StartDate <= end.Date.AddDays(1) && l.EndDate >= start.Date.AddDays(-1)
				&& everyoneIds.Contains(l.EmployeeId))
			.ToListAsync();

		var organizerDates = MeetingTime.LocalDates(start, end, organizer.Zone);
		if (leaves.Any(l => l.EmployeeId == organizer.Id && Overlaps(l, organizerDates)))
			return "Tu es en congé ce jour-là.";

		var onLeave = participants
			.Where(p =>
			{
				var dates = MeetingTime.LocalDates(start, end, p.Zone);
				return leaves.Any(l => l.EmployeeId == p.Id && Overlaps(l, dates));
			})
			.Select(p => p.FullName)
			.ToList();

		if (onLeave.Count > 0)
			return $"{string.Join(" et ", onLeave)} {(onLeave.Count == 1 ? "est" : "sont")} en congé ce jour-là.";

		return null;
	}

	private async Task<List<MeetingConflict>> FindConflictsAsync(List<PersonInfo> everyone, DateTime start, DateTime end, bool isTeamMeeting)
	{
		var everyoneIds = everyone.Select(e => e.Id).ToList();

		var overlapping = await _db.MeetingParticipants
			.Where(p => everyoneIds.Contains(p.EmployeeId)
				&& p.Meeting.StartAt < end
				&& p.Meeting.EndAt > start)
			.Select(p => new { p.EmployeeId, p.Meeting.IsTeamMeeting, p.Meeting.IsFlexible })
			.ToListAsync();

		return overlapping
			.Where(o => !(isTeamMeeting && o.IsFlexible))
			.GroupBy(o => o.EmployeeId)
			.Select(g =>
			{
				var person = everyone.First(e => e.Id == g.Key);
				return new MeetingConflict
				{
					EmployeeId = g.Key,
					FirstName = person.FirstName,
					LastName = person.LastName,
					WithTeamMeeting = g.Any(o => o.IsTeamMeeting)
				};
			})
			.ToList();
	}

	private async Task<List<MeetingOutsideHours>> FindOutsideHoursAsync(List<PersonInfo> participants, DateTime start, DateTime end)
	{
		List<MeetingOutsideHours> outsideHours = [];
		foreach (var participant in participants)
		{
			var (first, last) = MeetingTime.LocalDates(start, end, participant.Zone);

			OutsideHoursReason? reason;
			if (first != last)
				reason = OutsideHoursReason.OutsideHours;
			else
			{
				var day = (await _schedule.ForAsync(participant.Id, first, last))[0];
				var localStart = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(start, participant.Zone));
				var localEnd = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(end, participant.Zone));
				reason = MeetingTime.FindOutsideHoursReason(day, localStart, localEnd);
			}

			if (reason is not null)
			{
				outsideHours.Add(new MeetingOutsideHours
				{
					EmployeeId = participant.Id,
					FirstName = participant.FirstName,
					LastName = participant.LastName,
					Reason = reason.Value
				});
			}
		}

		return outsideHours;
	}

	private void AddInvitations(Meeting meeting, PersonInfo organizer, List<PersonInfo> participants)
	{
		foreach (var p in participants)
		{
			var participantZone = p.Zone;
			var participantStart = TimeZoneInfo.ConvertTimeFromUtc(meeting.StartAt, participantZone);

			_db.Notifications.Add(new Notification
			{
				RecipientId = p.Id,
				Type = NotificationType.MeetingInvitation,
				Message = $"{organizer.FullName} t'invite à « {meeting.Title} », {MeetingTexts.FormatWhen(meeting.StartAt, participantZone)}.",
				Link = $"/calendar?vue=jour&date={participantStart:yyyy-MM-dd}",
			});
		}
	}

	private static MeetingResponse ToResponse(Meeting meeting, string? color)
	{
		return new MeetingResponse
		{
			Id = meeting.Id,
			Kind = meeting.Kind,
			Title = meeting.Title,
			Description = meeting.Description,
			StartAt = meeting.StartAt,
			EndAt = meeting.EndAt,
			OrganizerId = meeting.OrganizerId,
			IsTeamMeeting = meeting.IsTeamMeeting,
			IsFlexible = meeting.IsFlexible,
			Color = color ?? MeetingColors.Default
		};
	}

	private static bool IsOnSlot(DateTime t)
	{
		return t.Minute % 15 == 0 && t.Second == 0 && t.Millisecond == 0;
	}

	private record PersonInfo(int Id, string FirstName, string LastName, string TimeZoneId)
	{
		public string FullName => $"{FirstName} {LastName}";
		public TimeZoneInfo Zone => TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
	}

	private static bool IsValidColor(string? color) => color is null || MeetingColors.All.Contains(color);

	private static bool Overlaps(LeaveRequest leave, (DateOnly First, DateOnly Last) dates) =>
		DateOnly.FromDateTime(leave.StartDate) <= dates.Last && DateOnly.FromDateTime(leave.EndDate) >= dates.First;
}
