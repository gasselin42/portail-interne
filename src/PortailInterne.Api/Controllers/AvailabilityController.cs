using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Services;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/availability")]
public class AvailabilityController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly EffectiveScheduleService _schedule;
	private readonly SiteClock _clock;

	public AvailabilityController(AppDbContext db, EffectiveScheduleService schedule, SiteClock clock)
	{
		_db = db;
		_schedule = schedule;
		_clock = clock;
	}

	[HttpGet]
	public async Task<ActionResult<List<EmployeeAvailability>>> Get(
		[FromQuery, BindRequired] DateOnly from,
		[FromQuery, BindRequired] DateOnly to,
		[FromQuery] List<int> employeeIds)
	{
		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
			return Unauthorized();

		var ids = employeeIds.Distinct().ToList();

		if (ids.Count == 0)
			return BadRequest(new { message = "Choisis au moins un employé." });
		
		if (ids.Count > 20)
			return BadRequest(new { message = "Choisis 20 employés au maximum." });

		if (to < from)
			return BadRequest(new { message = "La date de fin doit être après la date de début." });
			
		if (to.DayNumber - from.DayNumber >= 62)
			return BadRequest(new { message = "Sélectionne une plage de 62 jours au maximum."});

		var employees = await _db.Employees
			.AsNoTracking()
			.Where(e => ids.Contains(e.Id) && e.IsActive)
			.OrderBy(e => e.LastName)
			.ThenBy(e => e.FirstName)
			.ThenBy(e => e.Id)
			.Select(e => new { e.Id, e.FirstName, e.LastName, e.Site.TimeZoneId })
			.ToListAsync();

		if (employees.Count != ids.Count)
			return BadRequest(new { message = "Un des employés est introuvable ou inactif."});

		var zone = await _clock.TimeZoneForAsync(employeeId);

		var windowStart = MeetingTime.ToUtc(from, TimeOnly.MinValue, zone);
		var windowEnd = MeetingTime.ToUtc(to.AddDays(1), TimeOnly.MinValue, zone);

		var events = await _db.MeetingParticipants
			.Where(p => ids.Contains(p.EmployeeId)
				&& p.Meeting.StartAt < windowEnd
				&& p.Meeting.EndAt > windowStart
				&& p.Response != ParticipantResponse.Decline)
			.Select(p => new { p.EmployeeId, p.Meeting.StartAt, p.Meeting.EndAt, p.Meeting.IsFlexible })
			.ToListAsync();

		var result = new List<EmployeeAvailability>();

		foreach (var employee in employees)
		{
			var employeeZone = TimeZoneInfo.FindSystemTimeZoneById(employee.TimeZoneId);
			var (first, last) = MeetingTime.LocalDates(windowStart, windowEnd, employeeZone);
			var days = await _schedule.ForAsync(employee.Id, first, last);

			var availability = new EmployeeAvailability
			{
				EmployeeId = employee.Id,
				FirstName = employee.FirstName,
				LastName = employee.LastName,
				Days = days.Select(d => ToAvailabilityDay(d, employeeZone)).ToList(),
			};

			availability.Busy.AddRange(days.SelectMany(d => AbsenceSlots(d, employeeZone)));
			availability.Busy.AddRange(days.SelectMany(d => LeaveSlots(d, employeeZone)));
			availability.Busy.AddRange(events.Where(e => e.EmployeeId == employee.Id).Select(e => new BusySlot { StartAt = e.StartAt, EndAt = e.EndAt, Kind = e.IsFlexible ? BusyKind.FlexibleBusy : BusyKind.Busy } ));

			availability.Busy = availability.Busy.OrderBy(b => b.StartAt).ToList();

			result.Add(availability);
		}
		
		return Ok(result);
	}

	private static AvailabilityDay ToAvailabilityDay(EffectiveDay day, TimeZoneInfo zone)
	{
		DayStatus status = DayStatus.Working;
	
		if (day.Source == DaySource.NonDefini)
			status = DayStatus.Undefined;
		else if (day.StartTime is null || day.EndTime is null)
			status = DayStatus.NotWorking;

		return new AvailabilityDay
		{
			Day = day.Date,
			Status = status,
			StartAt = day.StartTime is null ? null : MeetingTime.ToUtc(day.Date, day.StartTime.Value, zone),
			EndAt = day.EndTime is null ? null : MeetingTime.ToUtc(day.Date, day.EndTime.Value, zone),
		};
	}

	private static List<BusySlot> AbsenceSlots(EffectiveDay day, TimeZoneInfo zone)
	{
		var slots = new List<BusySlot>();

		foreach (var absence in day.Absences)
		{
			var start = absence.StartedAt ?? absence.PlannedStart;
			var end = absence.EndedAt ?? absence.PlannedEnd ?? day.EndTime;

			if (start is null || end is null)
				continue;

			slots.Add(new BusySlot
			{
				StartAt = MeetingTime.ToUtc(day.Date, start.Value, zone),
				EndAt = MeetingTime.ToUtc(day.Date, end.Value, zone),
				Kind = BusyKind.Absent
			});
		}

		return slots;
	}

	private static List<BusySlot> LeaveSlots(EffectiveDay day, TimeZoneInfo zone)
	{
		var slots = new List<BusySlot>();

		if (day.Source != DaySource.Conge)
			return slots;

		slots.Add(new BusySlot
		{
			StartAt = MeetingTime.ToUtc(day.Date, TimeOnly.MinValue, zone),
			EndAt = MeetingTime.ToUtc(day.Date.AddDays(1), TimeOnly.MinValue, zone),
			Kind = BusyKind.Absent
		});

		return slots;
	}
}