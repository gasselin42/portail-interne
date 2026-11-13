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

		var (participants, participantsError) = await LoadParticipantsAsync(request.Kind!.Value, request, _scope.CallerEmployeeId);
		if (participantsError is not null)
			return BadRequest(new { message = participantsError });

		if (!IsValidColor(request.Color))
			return BadRequest(new { message = "Couleur inconnue." });

		var organizer = await LoadPersonAsync(_scope.CallerEmployeeId);

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

		var overlaps = await LoadOverlapsAsync(everyone, start, end);
		var conflicts = FindConflicts(overlaps, everyone, isTeamMeeting);
		var outsideHours = await FindOutsideHoursAsync(participants, start, end);

		if ((conflicts.Count > 0 || outsideHours.Count > 0) && !request.ConfirmConflicts)
			return Conflict(BuildConflictResponse(conflicts, outsideHours));

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
				new MeetingParticipant { EmployeeId = _scope.CallerEmployeeId, Color = request.Color, Response = ParticipantResponse.Accepte },
				.. participants.Select(p => new MeetingParticipant { EmployeeId = p.Id }),
			]
		};

		_db.Meetings.Add(meeting);

		AddInvitations(meeting, organizer, participants, conflicts, overlaps);
		AddPriorityNotices(meeting, overlaps, everyone);

		await _db.SaveChangesAsync();

		return Created($"api/meetings/{meeting.Id}", MeetingMapping.ToResponse(meeting, request.Color, ParticipantResponse.Accepte));
	}

	[HttpGet("{id:int}")]
	public async Task<ActionResult<MeetingDetailResponse>> Get(int id)
	{
		var meeting = await _db.Meetings
			.AsNoTracking()
			.Include(m => m.Organizer)
			.Include(m => m.Participants)
				.ThenInclude(p => p.Employee)
			.FirstOrDefaultAsync(m => m.Id == id);

		if (meeting is null)
			return NotFound();

		var canEdit = await CanEditAsync(meeting);
		var isParticipant = meeting.Participants.Any(p => p.EmployeeId == _scope.CallerEmployeeId);
		if (!(canEdit || isParticipant))
			return NotFound();

		var caller = meeting.Participants.FirstOrDefault(p => p.EmployeeId == _scope.CallerEmployeeId);
		var response = new MeetingDetailResponse
		{
			Meeting = MeetingMapping.ToResponse(meeting, caller?.Color, caller?.Response),
			Organizer = new MeetingPerson { EmployeeId = meeting.OrganizerId, FirstName = meeting.Organizer.FirstName, LastName = meeting.Organizer.LastName, Response = ParticipantResponse.Accepte },
			Participants = meeting.Participants
						.Where(p => p.EmployeeId != meeting.OrganizerId)
						.OrderBy(p => p.Employee.LastName)
						.ThenBy(p => p.Employee.FirstName)
						.ThenBy(p => p.EmployeeId)
						.Select(p => new MeetingPerson { EmployeeId = p.EmployeeId, FirstName = p.Employee.FirstName, LastName = p.Employee.LastName, Response = p.Response })
						.ToList(),
			CanEdit = canEdit
		};

		return Ok(response);
	}

	[HttpPut("{id:int}")]
	public async Task<ActionResult<MeetingResponse>> Update(int id, UpdateMeetingRequest request)
	{
		var meeting = await _db.Meetings
			.Include(m => m.Organizer)
			.Include(m => m.Participants)
				.ThenInclude(p => p.Employee)
					.ThenInclude(e => e.Site)
			.FirstOrDefaultAsync(m => m.Id == id);

		if (meeting is null)
			return NotFound();
		
		if (!await CanEditAsync(meeting))
		{
			if (meeting.Participants.Any(p => p.EmployeeId == _scope.CallerEmployeeId))
				return Forbid();
			else
				return NotFound();
		}
		
		if (meeting.StartAt <= _time.GetUtcNow().UtcDateTime)
			return BadRequest(new { message = "Ce meeting a déjà commencé." });

		if (string.IsNullOrWhiteSpace(request.Title))
			return BadRequest(new { message = "Le titre est obligatoire." });

		var title = request.Title.Trim();
		var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

		var organizer = await LoadPersonAsync(meeting.OrganizerId);
		var (participants, error) = await LoadParticipantsAsync(meeting.Kind, request, meeting.OrganizerId);

		if (error is not null)
			return BadRequest(new { message = error });
		
		var start = request.StartAt!.Value;
		var end = request.EndAt!.Value;

		var timesError = ValidateTimes(start, end, organizer.Zone);
		if (timesError is not null)
			return BadRequest(new { message = timesError });

		var timeChanged = start != meeting.StartAt || end != meeting.EndAt;
		var newcomers = participants.Where(p => !meeting.Participants.Any(m => m.EmployeeId == p.Id)).ToList();

		var leaveError = await FindLeaveErrorAsync(start, end, timeChanged ? organizer : null, timeChanged ? participants : newcomers);
		if (leaveError is not null)
			return BadRequest(new { message = leaveError });

		List<PersonInfo> everyone = [organizer, .. participants];

		var overlaps = await LoadOverlapsAsync(everyone, start, end, meeting.Id);
		var conflicts = FindConflicts(overlaps, everyone, meeting.IsTeamMeeting);
		var outsideHours = await FindOutsideHoursAsync(timeChanged ? participants : newcomers, start, end);

		if ((conflicts.Count > 0 || outsideHours.Count > 0) && !request.ConfirmConflicts)
			return Conflict(BuildConflictResponse(conflicts, outsideHours));

		var actor = (organizer.Id == _scope.CallerEmployeeId) ? organizer : await LoadPersonAsync(_scope.CallerEmployeeId);

		var newIds = participants.Select(p => p.Id).ToHashSet();
		var before = new MeetingBefore(
			meeting.Title,
			meeting.StartAt,
			meeting.EndAt,
			meeting.Participants.Where(p => p.EmployeeId != meeting.OrganizerId).Select(p => p.EmployeeId).ToHashSet(),
			meeting.Participants.Where(p => p.EmployeeId != meeting.OrganizerId && !newIds.Contains(p.EmployeeId)).ToList());

		meeting.Title = title;
		meeting.Description = description;
		meeting.StartAt = start;
		meeting.EndAt = end;
		meeting.IsFlexible = request.IsFlexible;

		meeting.Participants.RemoveAll(before.Removed.Contains);

		foreach (var newId in newIds)
		{
			if (!meeting.Participants.Any(p => p.EmployeeId == newId))
				meeting.Participants.Add(new MeetingParticipant { EmployeeId = newId });
		}

		if (timeChanged)
		{
			foreach (var participant in meeting.Participants.Where((p) => p.EmployeeId != meeting.OrganizerId).ToList())
				participant.Response = ParticipantResponse.EnAttente;
		}

		AddUpdateNotices(meeting, before, actor, organizer, participants, conflicts, overlaps, everyone);
		
		await _db.SaveChangesAsync();

		var caller = meeting.Participants.FirstOrDefault(p => p.EmployeeId == _scope.CallerEmployeeId);
		return Ok(MeetingMapping.ToResponse(meeting, caller?.Color, caller?.Response));
	}

	[HttpPut("{id:int}/response")]
	public async Task<IActionResult> UpdateResponse(int id, SaveMeetingResponseRequest request)
	{
		var meeting = await _db.Meetings
			.Include(m => m.Organizer)
			.Include(m => m.Participants)
				.ThenInclude(p => p.Employee)
					.ThenInclude(e => e.Site)
			.FirstOrDefaultAsync(m => m.Id == id);

		if (meeting is null || !meeting.Participants.Any((p) => p.EmployeeId == _scope.CallerEmployeeId))
			return NotFound();

		if (meeting.OrganizerId == _scope.CallerEmployeeId)
			return BadRequest(new { message = "L'organisateur participe d'office."});

		if (!Enum.IsDefined(request.Response) || request.Response == ParticipantResponse.EnAttente)
			return BadRequest(new { message = "Réponse inconnue."});

		if (meeting.StartAt <= _time.GetUtcNow().UtcDateTime)
			return BadRequest(new { message = "Ce meeting a déjà commencé." });
		
		var participant = meeting.Participants.First((p) => p.EmployeeId == _scope.CallerEmployeeId);

		if (request.Response != participant.Response)
		{
			var oldResponse = participant.Response;
			participant.Response = request.Response;

			var organizer = await LoadPersonAsync(meeting.OrganizerId);

			if (request.Response == ParticipantResponse.Decline)
			{
				AddNotification(meeting.OrganizerId, NotificationType.MeetingDeclined, meeting.StartAt, organizer.Zone,
					$"{participant.Employee.FirstName} {participant.Employee.LastName} a décliné « {meeting.Title} », " + MeetingTexts.FormatWhen(meeting.StartAt, organizer.Zone) + ".");
			}

			if (request.Response == ParticipantResponse.Accepte && oldResponse == ParticipantResponse.Decline)
			{
				AddNotification(meeting.OrganizerId, NotificationType.MeetingDeclinedWithdrawn, meeting.StartAt, organizer.Zone,
					$"{participant.Employee.FirstName} {participant.Employee.LastName} participera finalement à « {meeting.Title} », " + MeetingTexts.FormatWhen(meeting.StartAt, organizer.Zone) + ".");
			}
		
			await _db.SaveChangesAsync();
		}

		return NoContent();
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> Delete(int id)
	{
		var meeting = await _db.Meetings
			.Include(m => m.Organizer)
			.Include(m => m.Participants)
				.ThenInclude(p => p.Employee)
					.ThenInclude(e => e.Site)
			.FirstOrDefaultAsync(m => m.Id == id);

		if (meeting is null)
			return NotFound();
		
		if (!await CanEditAsync(meeting))
		{
			if (meeting.Participants.Any(p => p.EmployeeId == _scope.CallerEmployeeId))
				return Forbid();
			else
				return NotFound();
		}
		
		if (meeting.StartAt <= _time.GetUtcNow().UtcDateTime)
			return BadRequest(new { message = "Ce meeting a déjà commencé." });
		
		var actor = await LoadPersonAsync(_scope.CallerEmployeeId);
		var participants = meeting.Participants.Where(p => p.EmployeeId != _scope.CallerEmployeeId);

		foreach (var participant in participants)
		{
			var participantZone = TimeZoneInfo.FindSystemTimeZoneById(participant.Employee.Site.TimeZoneId);
			AddNotification(participant.EmployeeId, NotificationType.MeetingCancelled, meeting.StartAt, participantZone, $"{actor.FullName} a annulé « {meeting.Title} », {MeetingTexts.FormatWhen(meeting.StartAt, participantZone)}.");
		}

		_db.Meetings.Remove(meeting);
		await _db.SaveChangesAsync();

		return NoContent();
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

	private async Task<bool> CanEditAsync(Meeting meeting)
	{
		return meeting.OrganizerId == _scope.CallerEmployeeId || (meeting.Kind == MeetingKind.Meeting && await _scope.CanAdministerSiteAsync(meeting.Organizer.SiteId));
	}

	private Task<PersonInfo> LoadPersonAsync(int employeeId) =>
		_db.Employees
			.Where(e => e.Id == employeeId)
			.Select(e => new PersonInfo(e.Id, e.FirstName, e.LastName, e.Site.TimeZoneId))
			.FirstAsync();

	private async Task<(List<PersonInfo> Participants, string? Error)> LoadParticipantsAsync(MeetingKind kind, MeetingFormRequest request, int organizerId)
	{	
		if (kind == MeetingKind.PersonalBlock)
		{
			if (request.ParticipantIds.Count > 0)
				return ([], "Un bloc personnel n'a pas de participants.");
			return ([], null);
		}
		
		var participantsIds = request.ParticipantIds.Where(p => p != organizerId).Distinct().ToList();
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

	private async Task<string?> FindLeaveErrorAsync(DateTime start, DateTime end, PersonInfo? organizer, List<PersonInfo> participants)
	{
		var everyoneIds = participants.Select(p => p.Id).ToList();
		if (organizer is not null)
			everyoneIds.Add(organizer.Id);

		var leaves = await _db.LeaveRequests
			.Where(l => l.Status == LeaveStatus.Approuve
				&& l.StartDate <= end.Date.AddDays(1) && l.EndDate >= start.Date.AddDays(-1)
				&& everyoneIds.Contains(l.EmployeeId))
			.ToListAsync();

		if (organizer is not null)
		{
			var organizerDates = MeetingTime.LocalDates(start, end, organizer.Zone);
			if (leaves.Any(l => l.EmployeeId == organizer.Id && Overlaps(l, organizerDates)))
				return $"{(organizer.Id == _scope.CallerEmployeeId ? "Tu es" : $"{organizer.FullName} est")} en congé ce jour-là.";
		}

		var onLeave = participants
			.Where(p =>
			{
				var dates = MeetingTime.LocalDates(start, end, p.Zone);
				return leaves.Any(l => l.EmployeeId == p.Id && Overlaps(l, dates));
			})
			.Select(p => p.FullName)
			.Order()
			.ToList();

		if (onLeave.Count > 0)
			return $"{string.Join(" et ", onLeave)} {(onLeave.Count == 1 ? "est" : "sont")} en congé ce jour-là.";

		return null;
	}

	private async Task<List<Overlap>> LoadOverlapsAsync(List<PersonInfo> everyone, DateTime start, DateTime end, int? excludeMeetingId = null)
	{
		var everyoneIds = everyone.Select(e => e.Id).ToList();

		return await _db.MeetingParticipants
			.Where(p => everyoneIds.Contains(p.EmployeeId)
				&& p.Meeting.StartAt < end
				&& p.Meeting.EndAt > start
				&& p.MeetingId != excludeMeetingId
				&& p.Response != ParticipantResponse.Decline)
			.Select(p => new Overlap(
				p.EmployeeId,
				p.MeetingId,
				p.Meeting.Title,
				p.Meeting.Kind,
				p.Meeting.StartAt,
				p.Meeting.OrganizerId,
				p.Meeting.Organizer.Site.TimeZoneId,
				p.Meeting.IsTeamMeeting,
				p.Meeting.IsFlexible
			))
			.ToListAsync();
	}

	private static List<MeetingConflict> FindConflicts(List<Overlap> overlaps, List<PersonInfo> everyone, bool isTeamMeeting)
	{
		return overlaps
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

	private static MeetingConflictResponse BuildConflictResponse(List<MeetingConflict> conflicts, List<MeetingOutsideHours> outsideHours)
	{
		List<string> messages = [];

		if (conflicts.Count > 0)
			messages.Add($"Conflit d'horaire pour {conflicts.Count} {(conflicts.Count == 1 ? "personne" : "personnes")}.");

		if (outsideHours.Count > 0)
			messages.Add($"{outsideHours.Count} {(outsideHours.Count == 1 ? "personne est hors de son" : "personnes sont hors de leur")} horaire.");

		return new MeetingConflictResponse
		{
			Message = string.Join(" ", messages),
			Conflicts = conflicts,
			OutsideHours = outsideHours
		};
	}

	private void AddInvitations(Meeting meeting, PersonInfo actor, List<PersonInfo> participants, List<MeetingConflict> conflicts, List<Overlap> overlaps)
	{
		foreach (var p in participants)
		{
			var participantZone = p.Zone;
			NotificationType type;
			string note;

			if (conflicts.Any(c => c.EmployeeId == p.Id))
			{
				type = NotificationType.MeetingConflict;
				note = " Ça chevauche un autre de tes événements." + (meeting.IsTeamMeeting ? " Meeting d'équipe : il a priorité." : "");
			}
			else if (meeting.IsTeamMeeting && overlaps.Any(o => o.EmployeeId == p.Id && o.IsFlexible))
			{
				type = NotificationType.FlexibleBlockOverlapped;
				note = " Il chevauche ton bloc souple, qui reste en place.";
			}
			else
			{
				type = NotificationType.MeetingInvitation;
				note = "";
			}

			AddNotification(p.Id, type, meeting.StartAt, participantZone, $"{actor.FullName} t'invite à « {meeting.Title} », {MeetingTexts.FormatWhen(meeting.StartAt, participantZone)}.{note}");
		}
	}

	private void AddPriorityNotices(Meeting meeting, List<Overlap> overlaps, List<PersonInfo> everyone)
	{
		if (!meeting.IsTeamMeeting) return;

		var groups = overlaps
			.Where(o => o.Kind == MeetingKind.Meeting
					&& !o.IsTeamMeeting
					&& o.OrganizerId != meeting.OrganizerId
					&& o.OrganizerId != o.EmployeeId)
			.GroupBy(o => o.MeetingId)
			.ToList();
		
		foreach (var g in groups)
		{
			var names = g
				.Select(o => everyone.First(e => e.Id == o.EmployeeId).FullName)
				.Order()
				.ToList();

			var first = g.First(); // Le 1er meeting du groupe, ils sont tous identiques de toute façon
			var zone = TimeZoneInfo.FindSystemTimeZoneById(first.OrganizerTimeZoneId);

			AddNotification(first.OrganizerId, NotificationType.MeetingConflict, first.StartAt, zone, $"{string.Join(" et ", names)} {(names.Count == 1 ? "a" : "ont")} un meeting d'équipe prioritaire pendant « {first.Title} », {MeetingTexts.FormatWhen(first.StartAt, zone)}.");
		}
	}

	private void AddUpdateNotices(Meeting meeting, MeetingBefore before, PersonInfo actor, PersonInfo organizer,
		List<PersonInfo> participants, List<MeetingConflict> conflicts, List<Overlap> overlaps, List<PersonInfo> everyone)
	{
		var timeChanged = meeting.StartAt != before.Start || meeting.EndAt != before.End;
		var titleChanged = meeting.Title != before.Title;

		// 1. Retirés : l'ancien titre et l'ancienne heure, ceux qu'ils connaissaient
		foreach (var p in before.Removed)
		{
			var zone = TimeZoneInfo.FindSystemTimeZoneById(p.Employee.Site.TimeZoneId);
			AddNotification(p.EmployeeId, NotificationType.MeetingCancelled, before.Start, zone,
				$"{actor.FullName} t'a retiré de « {before.Title} », {MeetingTexts.FormatWhen(before.Start, zone)}.");
		}

		// 2. Ajoutés : une invitation, comme à la création
		var added = participants.Where(p => !before.Ids.Contains(p.Id)).ToList();
		AddInvitations(meeting, actor, added, conflicts, overlaps);

		// 3. Conservés : seulement si l'heure ou le titre change
		if (timeChanged || titleChanged)
		{
			foreach (var p in participants.Where(p => before.Ids.Contains(p.Id)))
			{
				NotificationType type = NotificationType.MeetingUpdated;
				var when = MeetingTexts.FormatWhen(meeting.StartAt, p.Zone);
				string text;

				if (titleChanged && timeChanged)
					text = $"{actor.FullName} a renommé « {before.Title} » en « {meeting.Title} » et modifié son horaire : {when}.";
				else if (titleChanged)
					text = $"{actor.FullName} a renommé « {before.Title} » en « {meeting.Title} ».";
				else
					text = $"{actor.FullName} a modifié l'horaire de « {meeting.Title} » : {when}.";

				if (timeChanged && conflicts.Any(c => c.EmployeeId == p.Id))
					text += " Ça chevauche un autre de tes événements." + (meeting.IsTeamMeeting ? " Meeting d'équipe : il a priorité." : "");

				if (timeChanged && !conflicts.Any(c => c.EmployeeId == p.Id) && meeting.IsTeamMeeting && overlaps.Any(o => o.EmployeeId == p.Id && o.IsFlexible))
				{
					text += " Il chevauche ton bloc souple, qui reste en place.";
					type = NotificationType.FlexibleBlockOverlapped;
				}

				AddNotification(p.Id, type, meeting.StartAt, p.Zone, text);
			}
		}

		// 4. Organisateur : prévenu quand un Admin agit à sa place
		if (actor.Id != organizer.Id)
			AddNotification(organizer.Id, NotificationType.MeetingUpdated, meeting.StartAt, organizer.Zone,
				$"{actor.FullName} a modifié ton meeting « {meeting.Title} », {MeetingTexts.FormatWhen(meeting.StartAt, organizer.Zone)}.");

		// 5. Priorité : seulement les chevauchements nouveaux
		var newOverlaps = timeChanged
			? overlaps
			: overlaps.Where(o => !before.Ids.Contains(o.EmployeeId) && o.EmployeeId != meeting.OrganizerId).ToList();
		AddPriorityNotices(meeting, newOverlaps, everyone);
	}

	private void AddNotification(int recipientId, NotificationType type, DateTime start, TimeZoneInfo zone, string message)
	{
		_db.Notifications.Add(MeetingTexts.Notification(recipientId, type, start, zone, message));
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

	private record Overlap(
		int EmployeeId,
		int MeetingId,
		string Title,
		MeetingKind Kind,
		DateTime StartAt,
		int OrganizerId,
		string OrganizerTimeZoneId,
		bool IsTeamMeeting,
		bool IsFlexible);

	private record MeetingBefore(string Title, DateTime Start, DateTime End, HashSet<int> Ids, List<MeetingParticipant> Removed);

	private static bool IsValidColor(string? color) => color is null || MeetingColors.All.Contains(color);

	private static bool Overlaps(LeaveRequest leave, (DateOnly First, DateOnly Last) dates) =>
		DateOnly.FromDateTime(leave.StartDate) <= dates.Last && DateOnly.FromDateTime(leave.EndDate) >= dates.First;
}
