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
[Route("api/calendar")]
public class CalendarController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly EffectiveScheduleService _scheduleService;
	private readonly LeaveUsageService _leaveUsage;
	private readonly SiteClock _clock;

	public CalendarController(AppDbContext db, EffectiveScheduleService scheduleService, LeaveUsageService leaveUsage, SiteClock clock)
	{
		_db = db;
		_scheduleService = scheduleService;
		_leaveUsage = leaveUsage;
		_clock = clock;
	}

	[HttpGet]
	public async Task<ActionResult<CalendarResponse>> Get(
		[FromQuery, BindRequired] DateOnly from,
		[FromQuery, BindRequired] DateOnly to)
	{
		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
			return Unauthorized();

		if (to < from)
			return BadRequest(new { message = "La date de fin doit être après la date de début." });
			
		if (to.DayNumber - from.DayNumber >= 62)
			return BadRequest(new { message = "Sélectionne une plage de 62 jours au maximum."});

		var (today, now) = await _clock.NowForAsync(employeeId);

		List<EffectiveDay> days = await _scheduleService.ForAsync(employeeId, from, to);

		// Une absence pour le calendrier, avec son état calculé maintenant (R24)
		CalendarAbsenceResponse ToCalendarAbsence(Absence a, TimeOnly? workdayEnd)
		{
			var status = AbsenceState.Compute(a, today, now, workdayEnd);
			return new CalendarAbsenceResponse
			{
				Id = a.Id,
				Status = a.Status,
				Reason = a.Reason,
				PlannedStart = a.PlannedStart,
				PlannedEnd = a.PlannedEnd,
				Start = status.Start,
				End = status.End,
				InProgress = status.InProgress,
				ReturnConfirmed = status.ReturnConfirmed,
			};
		}

		List<EffectiveDayResponse> daysResponse = days
			.Select(d => new EffectiveDayResponse
			{
				Date = d.Date,
				Source = d.Source,
				StartTime = d.StartTime,
				EndTime = d.EndTime,
				BreakMinutes = d.BreakMinutes,
				HolidayName = d.HolidayName,
				PendingAdjustment = 
					d.PendingAdjustment is not null
						? new PendingAdjustmentResponse
							{
								Id = d.PendingAdjustment.Id,
								StartTime = d.PendingAdjustment.StartTime,
								EndTime = d.PendingAdjustment.EndTime,
								BreakMinutes = d.PendingAdjustment.BreakMinutes
							}
						: null,
				Absences = d.Absences.Select(a => ToCalendarAbsence(a, d.EndTime)).ToList(),
				PendingAbsences = d.PendingAbsences.Select(a => ToCalendarAbsence(a, d.EndTime)).ToList(),
			})
			.ToList();

		List<LeaveRequestResponse> leaves = await _db.LeaveRequests
			.AsNoTracking()
			.Where(l => l.EmployeeId == employeeId)
			.Where(l => DateOnly.FromDateTime(l.StartDate) <= to && DateOnly.FromDateTime(l.EndDate) >= from)
			.Where(l => l.Status == LeaveStatus.EnAttente || l.Status == LeaveStatus.Approuve)
			.OrderBy(l => l.StartDate)
			.ThenBy(l => l.EndDate)
			.Select(l => new LeaveRequestResponse
			{
				Id = l.Id,
				EmployeeId = l.EmployeeId,
				StartDate = l.StartDate,
				EndDate = l.EndDate,
				Type = l.Type,
				Status = l.Status,
				Reason = l.Reason,
				CreatedAt = l.CreatedAt
			})
			.ToListAsync();

		await _leaveUsage.AddUsageAsync(leaves);

		return Ok(new CalendarResponse { Days = daysResponse, Leaves = leaves });
	}
}