using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/employees/{employeeId:int}/absences")]
public class AbsenceController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly AccessScope _scope;
	private readonly SiteClock _clock;
	private readonly EffectiveScheduleService _schedule;

	public AbsenceController(AppDbContext db, AccessScope scope, SiteClock clock, EffectiveScheduleService schedule)
	{
		_db = db;
		_scope = scope;
		_clock = clock;
		_schedule = schedule;
	}

	[HttpGet]
	public async Task<ActionResult<List<AbsenceResponse>>> Get(int employeeId)
	{
		var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null || !employee.IsActive)
            return NotFound();

		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var userId))
            return Unauthorized();

		if (!await CanReadAsync(employee, userId))
			return NotFound();

		var (today, now) = await _clock.NowForAsync(employeeId);
		var isSelf = employee.Id == userId;

		var absences = await _db.Absences
				.AsNoTracking()
				.Include(a => a.CreatedBy)
				.Where(a => a.EmployeeId == employeeId && a.Date >= today)
				.OrderBy(a => a.Date)
				.ThenBy(a => a.StartedAt ?? a.PlannedStart)
				.ToListAsync();

		// La fin de journée de chaque date, en une seule fois
		var lastDate = absences.Count == 0 ? today : absences.Max(a => a.Date);
		var workdayEnds = (await _schedule.ForAsync(employee.Id, today, lastDate))
			.ToDictionary(d => d.Date, d => d.EndTime);

		var response = absences.Select(a =>
		{
			var status = AbsenceState.Compute(a, today, now, workdayEnds[a.Date]);
			return ToResponse(a, a.CreatedBy, isSelf, today, now, status);
		}).ToList();

		return Ok(response);
	}

	[HttpPost]
	public async Task<ActionResult<AbsenceResponse>> Create(int employeeId, SaveAbsenceRequest request)
	{
		var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null || !employee.IsActive)
            return NotFound();

		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var userId))
            return Unauthorized();
		
		var isSelf = employee.Id == userId;
		if (!isSelf && !await CanManageAsync(employee, userId))
			return NotFound();

		var autoApproved = !isSelf;
		var (today, now) = await _clock.NowForAsync(employeeId);
		var date = request.Date!.Value;

		if (date < today)
			return BadRequest(new { message = "Cette date est passée : choisis une date ultérieure." });
		
		Absence absence = new Absence
		{
			EmployeeId = employee.Id,
			Date = date,
			PlannedStart = request.PlannedStart,
			PlannedEnd = request.PlannedEnd,
			Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
			Status = autoApproved ? LeaveStatus.Approuve : LeaveStatus.EnAttente,
			ReturnConfirmed = false,
			CreatedById = userId,
			ReviewedById = autoApproved ? userId : null,
			ReviewedAt = autoApproved ? DateTime.UtcNow : null,
		};

		var error = AbsenceRules.Validate(absence);
		if (error is not null)
			return BadRequest(new { message = error });

		var day = (await _schedule.ForAsync(employee.Id, date, date))[0];

		if (day is null || day.StartTime is null)
			return BadRequest(new { message = "Ce jour n'est pas travaillé." });

		if (absence.PlannedStart < day.StartTime || absence.PlannedEnd > day.EndTime)
			return BadRequest(new { message = $"L'absence doit être comprise dans tes heures de travail ({day.StartTime:HH\\:mm} à {day.EndTime:HH\\:mm})." });

		var overlap = await _db.Absences.AnyAsync(a =>
			a.EmployeeId == employee.Id
			&& a.Date == date
			&& (a.Status == LeaveStatus.EnAttente || a.Status == LeaveStatus.Approuve)
			&& a.PlannedStart < absence.PlannedEnd
			&& absence.PlannedStart < a.PlannedEnd);

		if (overlap)
			return Conflict(new { message = "Tu as déjà une absence qui chevauche ces heures ce jour-là." });

		_db.Absences.Add(absence);
		await _db.SaveChangesAsync();

		var creator = await _db.Employees.FindAsync(userId);
		var status = AbsenceState.Compute(absence, today, now, day.EndTime);

		return Created($"api/employees/{employee.Id}/absences/{absence.Id}",
						ToResponse(absence, creator, isSelf, today, now, status));
	}

	[HttpPost("{id:int}/cancel")]
	public async Task<IActionResult> Cancel(int employeeId, int id)
	{
		var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null || !employee.IsActive)
            return NotFound();

		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var userId))
            return Unauthorized();

		var isSelf = employee.Id == userId;
		if (!isSelf && !await CanManageAsync(employee, userId))
			return NotFound();

		var absence = await _db.Absences
			.FirstOrDefaultAsync(a => a.Id == id && a.EmployeeId == employeeId);
		
		if (absence is null)
			return NotFound();

		if (absence.Status == LeaveStatus.Refuse || absence.Status == LeaveStatus.Annule)
			return BadRequest(new { message = "Cette absence n'est plus active." });
		
		var (today, now) = await _clock.NowForAsync(employee.Id);

		// Commencée : « Je pars » cliqué, ou son heure de début est passée
		var started = absence.StartedAt is not null
			|| absence.Date < today
			|| (absence.Date == today && absence.PlannedStart <= now);
		
		if (started)
			return BadRequest(new { message = "Cette absence a déjà commencée : elle ne peut plus être annulée." });
		
		if (isSelf && absence.Status == LeaveStatus.Approuve)
			return BadRequest(new { message = "Une absence approuvée ne peut être annulé que par ton manager." });

		absence.Status = LeaveStatus.Annule;
		await _db.SaveChangesAsync();

		return NoContent();
	}

	[HttpPost("depart")]
	public async Task<ActionResult<AbsenceResponse>> Depart(int employeeId, DepartRequest request)
	{
		var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null || !employee.IsActive)
            return NotFound();

		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var userId))
            return Unauthorized();

		var isSelf = employee.Id == userId;
		if (!isSelf && !await CanManageAsync(employee, userId))
			return NotFound();
		
		var (today, now) = await _clock.NowForAsync(employee.Id);

		var day = (await _schedule.ForAsync(employee.Id, today, today))[0];

		if (day.StartTime is null || now < day.StartTime || now > day.EndTime)
			return BadRequest(new { message = "Tu n'es pas en période de travail." });

		var todays = await _db.Absences
				.Where(a => a.EmployeeId == employeeId
					&& a.Date == today
					&& a.Status == LeaveStatus.Approuve)
				.ToListAsync();

		if (todays.Any(a => AbsenceState.Compute(a, today, now, day.EndTime).InProgress))
			return Conflict(new { message = "Tu es déjà absent." });

		var absence = todays.FirstOrDefault(a => a.Status == LeaveStatus.Approuve
											&& a.StartedAt is null && a.PlannedStart is not null
											&& Math.Abs((a.PlannedStart!.Value - now).TotalMinutes) <= 30);

		if (absence is not null)
			absence.StartedAt = now;
		else
		{
			absence = new Absence
			{
				EmployeeId = employee.Id,
				Date = today,
				PlannedStart = null,
				PlannedEnd = null,
				StartedAt = now,
				EndedAt = null,
				ReturnConfirmed = false,
				Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
				Status = LeaveStatus.Approuve,
				CreatedById = userId,
				ReviewedById = null,
				ReviewedAt = null,
				CreatedAt = DateTime.UtcNow
			};

			_db.Absences.Add(absence);
		}

		await _db.SaveChangesAsync();

		var status = AbsenceState.Compute(absence, today, now, day.EndTime);
		var creator = await _db.Employees.FindAsync(absence.CreatedById);

		return Ok(ToResponse(absence, creator, isSelf, today, now, status));
	}

	[HttpPost("return")]
	public async Task<ActionResult<AbsenceResponse>> Return(int employeeId)
	{
		var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null || !employee.IsActive)
            return NotFound();

		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var userId))
            return Unauthorized();

		var isSelf = employee.Id == userId;
		if (!isSelf && !await CanManageAsync(employee, userId))
			return NotFound();
		
		var (today, now) = await _clock.NowForAsync(employee.Id);

		var day = (await _schedule.ForAsync(employee.Id, today, today))[0];

		var todays = await _db.Absences
				.Where(a => a.EmployeeId == employeeId
					&& a.Date == today
					&& a.Status == LeaveStatus.Approuve)
				.ToListAsync();

		var absence = todays.FirstOrDefault(a => AbsenceState.Compute(a, today, now, day.EndTime).InProgress);

		if (absence is null)
			return BadRequest(new { message = "Tu n'es pas absent en ce moment." });

		absence.EndedAt = now;
		absence.ReturnConfirmed = true;

		await _db.SaveChangesAsync();

		var creator = await _db.Employees.FindAsync(absence.CreatedById);
		var status = AbsenceState.Compute(absence, today, now, day.EndTime);

		return Ok(ToResponse(absence, creator, isSelf, today, now, status));
	}

	private async Task<bool> CanManageAsync(Employee employee, int userId) =>
		await _scope.CanAdministerSiteAsync(employee.SiteId) || employee.ManagerId == userId;

	/// <summary>Annulable : active, pas encore commencée, et pas « soi-même avec une absence approuvée ».</summary>
	private static bool CanCancel(Absence absence, bool isSelf, DateOnly today, TimeOnly now) =>
		(absence.Status == LeaveStatus.EnAttente || absence.Status == LeaveStatus.Approuve)
		&& !(isSelf && absence.Status == LeaveStatus.Approuve)
		&& absence.StartedAt is null
		&& (absence.Date > today || (absence.Date == today && absence.PlannedStart > now));

	private async Task<bool> CanReadAsync(Employee employee, int userId) =>
		employee.Id == userId || await CanManageAsync(employee, userId);

	private static AbsenceResponse ToResponse(Absence absence, Employee? creator, bool isSelf, DateOnly today, TimeOnly now, AbsenceStatus status) => new()
	{
		Id = absence.Id,
		Date = absence.Date,
		PlannedStart = absence.PlannedStart,
		PlannedEnd = absence.PlannedEnd,
		Reason = string.IsNullOrWhiteSpace(absence.Reason) ? null : absence.Reason.Trim(),
		Status = absence.Status,
		Start = status.Start,
		End = status.End,
		InProgress = status.InProgress,
		ReturnConfirmed = status.ReturnConfirmed,
		CreatedAt = absence.CreatedAt,
		CreatedById = creator!.Id,
		CreatedByFirstName = creator.FirstName,
		CreatedByLastName = creator.LastName,
		CanCancel = CanCancel(absence, isSelf, today, now),
	};
}