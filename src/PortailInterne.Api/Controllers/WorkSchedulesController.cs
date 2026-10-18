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
[Route("api/employees/{employeeId:int}/schedules")]
public class WorkSchedulesController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly SiteClock _siteClock;
	private readonly AccessScope _scope;

    public WorkSchedulesController(AppDbContext db, SiteClock siteClock, AccessScope scope)
    {
        _db = db;
		_siteClock = siteClock;
		_scope = scope;
    }

	[HttpGet]
	public async Task<ActionResult<List<WorkScheduleResponse>>> List(int employeeId)
	{
		var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null || !employee.IsActive)
            return NotFound();

		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var userId))
            return Unauthorized();

		if (!await CanReadAsync(employee, userId))
			return NotFound();

		var today = await _siteClock.TodayForAsync(employee.Id);
		
		var schedules = await _db.WorkSchedules
			.AsNoTracking()
			.Where(s => s.EmployeeId == employeeId)
			.OrderByDescending(s => s.EffectiveFrom)
			.Select(s => new WorkScheduleResponse
			{
				Id = s.Id,
				EffectiveFrom = s.EffectiveFrom,
				CreatedAt = s.CreatedAt,
				CreatedByFirstName = s.CreatedBy.FirstName,
				CreatedByLastName = s.CreatedBy.LastName,
				IsEditable = s.EffectiveFrom >= today,
				Days = s.Days.Select(d => new WorkScheduleDayDto
					{
						DayOfWeek = d.DayOfWeek,
						StartTime = d.StartTime,
						EndTime = d.EndTime,
						BreakMinutes = d.BreakMinutes
					}).ToList()
			})
			.ToListAsync();

		foreach (var schedule in schedules)
			schedule.Days = schedule.Days.OrderBy(d => ((int)d.DayOfWeek + 6) % 7).ToList();
		
		return Ok(schedules);
	}

	[HttpPost]
	public async Task<ActionResult<WorkScheduleResponse>> Create(int employeeId, SaveWorkScheduleRequest request)
	{
		var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null || !employee.IsActive)
            return NotFound();

		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var userId))
            return Unauthorized();

		if (!await CanManageAsync(employee, userId))
			return NotFound();
		
		var today = await _siteClock.TodayForAsync(employee.Id);
		if (request.EffectiveFrom < today)
			return BadRequest(new { message = "Un horaire ne peut pas commencer dans le passé." });

		var days = request.Days.Select(d => new WorkScheduleDay
		{
			DayOfWeek = d.DayOfWeek,
			StartTime = d.StartTime,
			EndTime = d.EndTime,
			BreakMinutes = d.BreakMinutes
		}).ToList();

		var error = WorkScheduleRules.Validate(days);
		if (error is not null)
			return BadRequest(new { message = error });

		var duplicate = await _db.WorkSchedules.AnyAsync(s =>
			s.EmployeeId == employee.Id && s.EffectiveFrom == request.EffectiveFrom);

		if (duplicate)
			return Conflict(new { message = "Cet employé a déjà un horaire qui commence à cette date." });
	
		var caller = await _db.Employees.FindAsync(userId);

		if (caller is null)
			return NotFound();

		var schedule = new WorkSchedule
		{
			EmployeeId = employeeId,
			EffectiveFrom = request.EffectiveFrom!.Value,
			CreatedAt = DateTime.UtcNow,
			CreatedById = userId,
			Days = days
		};

		_db.WorkSchedules.Add(schedule);
		await _db.SaveChangesAsync();

		var response = new WorkScheduleResponse
		{
			Id = schedule.Id,
			EffectiveFrom = schedule.EffectiveFrom,
			CreatedAt = schedule.CreatedAt,
			CreatedByFirstName = caller.FirstName,
			CreatedByLastName = caller.LastName,
			IsEditable = schedule.EffectiveFrom >= today,
			Days = schedule.Days.Select(d => new WorkScheduleDayDto
				{
					DayOfWeek = d.DayOfWeek,
					StartTime = d.StartTime,
					EndTime = d.EndTime,
					BreakMinutes = d.BreakMinutes
				})
				.OrderBy(d => ((int)d.DayOfWeek + 6) % 7)
				.ToList()
		};

		return Created($"api/employees/{employeeId}/schedules/{response.Id}", response);
	}

	[HttpPut("{id:int}")]
	public async Task<IActionResult> Put(int employeeId, int id, SaveWorkScheduleRequest request)
	{
		var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null || !employee.IsActive)
            return NotFound();
		
		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var userId))
            return Unauthorized();
		
		if (!await CanManageAsync(employee, userId))
			return NotFound();
		
		var today = await _siteClock.TodayForAsync(employee.Id);

		var days = request.Days.Select(d => new WorkScheduleDay
		{
			DayOfWeek = d.DayOfWeek,
			StartTime = d.StartTime,
			EndTime = d.EndTime,
			BreakMinutes = d.BreakMinutes
		}).ToList();

		var error = WorkScheduleRules.Validate(days);
		if (error is not null)
			return BadRequest(new { message = error });

		var duplicate = await _db.WorkSchedules.AnyAsync(s =>
			s.EmployeeId == employee.Id && s.EffectiveFrom == request.EffectiveFrom && s.Id != id);
	
		if (duplicate)
			return Conflict(new { message = "Cet employé a déjà un horaire qui commence à cette date." });
	
		var schedule = await _db.WorkSchedules
				.Include(s => s.Days)
				.FirstOrDefaultAsync(s => s.Id == id && s.EmployeeId == employeeId);

		if (schedule is null)
			return NotFound();

		if (schedule.EffectiveFrom < today)
			return BadRequest(new { message = "Cet horaire est déjà en vigueur." });

		if (request.EffectiveFrom < today)
			return BadRequest(new { message = "Un horaire ne peut pas commencer dans le passé." });

		await using var transaction = await _db.Database.BeginTransactionAsync();

		schedule.Days.Clear();
		await _db.SaveChangesAsync();

		schedule.EffectiveFrom = request.EffectiveFrom!.Value;
		schedule.Days.AddRange(days);
		await _db.SaveChangesAsync();

		await transaction.CommitAsync();

		return NoContent();
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> Delete(int employeeId, int id)
	{
		var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null || !employee.IsActive)
            return NotFound();
		
		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var userId))
            return Unauthorized();
		
		if (!await CanManageAsync(employee, userId))
			return NotFound();
		
		var today = await _siteClock.TodayForAsync(employee.Id);
		
		var schedule = await _db.WorkSchedules
				.FirstOrDefaultAsync(s => s.Id == id && s.EmployeeId == employeeId);

		if (schedule is null)
			return NotFound();

		if (schedule.EffectiveFrom < today)
			return BadRequest(new { message = "Cet horaire est déjà en vigueur : crée plutôt un nouvel horaire." });

		_db.WorkSchedules.Remove(schedule);
		await _db.SaveChangesAsync();

		return NoContent();
	}

	private async Task<bool> CanManageAsync(Employee employee, int userId) =>
		await _scope.CanAdministerSiteAsync(employee.SiteId) || employee.ManagerId == userId;


	private async Task<bool> CanReadAsync(Employee employee, int userId) =>
		employee.Id == userId || await CanManageAsync(employee, userId);
}