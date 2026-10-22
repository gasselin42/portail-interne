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
[Route("api/employees/{employeeId:int}/adjustments")]
public class AdjustmentsController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly SiteClock _siteClock;
	private readonly AccessScope _scope;

	public AdjustmentsController(AppDbContext db, SiteClock clock, AccessScope scope)
	{
		_db = db;
		_siteClock = clock;
		_scope = scope;
	}

	[HttpGet]
	public async Task<ActionResult<List<AdjustmentResponse>>> List(int employeeId)
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
		var isSelf = employee.Id == userId;

		var adjustments = (await _db.ScheduleAdjustments
					.AsNoTracking()
					.Include(a => a.CreatedBy)
					.Where(a => a.EmployeeId == employeeId && a.Date >= today)
					.OrderBy(a => a.Date)
					.ToListAsync())
				.Select(a => ToResponse(a, a.CreatedBy, isSelf, today))
				.ToList();

		return Ok(adjustments);
	}

	[HttpPost]
	public async Task<ActionResult<AdjustmentResponse>> Create(int employeeId, SaveAdjustmentRequest request)
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
		if (request.StartTime is null && !autoApproved)
			return BadRequest(new { message = "Seul un manager ou un Admin peut retirer une journée de travail." });

		var today = await _siteClock.TodayForAsync(employee.Id);
		var date = request.Date!.Value;

		if (date < today)
			return BadRequest(new { message = "Cette date est passée : choisis une date ultérieure." });

		var adjustment = new ScheduleAdjustment
		{
			EmployeeId = employee.Id,
			Date = date,
			StartTime = request.StartTime,
			EndTime = request.EndTime,
			BreakMinutes = request.BreakMinutes,
			Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
			Status = autoApproved ? LeaveStatus.Approuve : LeaveStatus.EnAttente,
			CreatedById = userId,
			ReviewedById = autoApproved ? userId : null,
			ReviewedAt = autoApproved ? DateTime.UtcNow : null,
			CreatedAt = DateTime.UtcNow,
		};

		var error = AdjustmentRules.Validate(adjustment);
		if (error is not null)
			return BadRequest(new { message = error });

		var duplicate = await _db.ScheduleAdjustments.AnyAsync(a =>
			a.EmployeeId == employee.Id
			&& a.Date == date
			&& (a.Status == LeaveStatus.EnAttente || a.Status == LeaveStatus.Approuve));

		if (duplicate)
			return Conflict(new { message = "Cet employé a déjà un ajustement pour cette date : annule-le d'abord." });

		_db.ScheduleAdjustments.Add(adjustment);
		await _db.SaveChangesAsync();

		var creator = await _db.Employees.FindAsync(userId);
		return Created($"api/employees/{employee.Id}/adjustments/{adjustment.Id}", ToResponse(adjustment, creator!, isSelf, today));
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

		var adjustment = await _db.ScheduleAdjustments
			.FirstOrDefaultAsync(a => a.Id == id && a.EmployeeId == employeeId);
		
		if (adjustment is null)
			return NotFound();

		if (adjustment.Status == LeaveStatus.Refuse || adjustment.Status == LeaveStatus.Annule)
			return BadRequest(new { message = "Cet ajustement n'est plus actif." });
		
		var today = await _siteClock.TodayForAsync(employee.Id);
		
		if (adjustment.Date < today)
			return BadRequest(new { message = "Cet ajustement est passé : il ne peut plus être annulé." });
		
		if (isSelf && adjustment.Status == LeaveStatus.Approuve)
			return BadRequest(new { message = "Un ajustement approuvé ne peut être annulé que par ton manager." });

		adjustment.Status = LeaveStatus.Annule;
		await _db.SaveChangesAsync();

		return NoContent();
	}

	[HttpPost("{id:int}/review")]
	public async Task<IActionResult> Review(int employeeId, int id, ReviewLeaveRequest request)
	{
		if (request.Status is not (LeaveStatus.Approuve or LeaveStatus.Refuse))
            return BadRequest(new { message = "Le statut doit être approuvé ou refusé" });
		
		var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee is null || !employee.IsActive)
            return NotFound();

		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var userId))
            return Unauthorized();

		if (employee.Id == userId)
			return BadRequest(new { message = "Tu ne peux pas approuver ou refuser ta propre demande." });
		
		if (!await CanManageAsync(employee, userId))
			return NotFound();

		var adjustment = await _db.ScheduleAdjustments
			.FirstOrDefaultAsync(a => a.Id == id && a.EmployeeId == employeeId);

		if (adjustment is null)
			return NotFound();

		if (adjustment.Status != LeaveStatus.EnAttente)
			return BadRequest(new { message = "Cet ajustement n'est pas en attente." });

		var today = await _siteClock.TodayForAsync(employee.Id);
		
		if (request.Status == LeaveStatus.Approuve && adjustment.Date < today)
			return BadRequest(new { message = "Cet ajustement est passé : il ne peut plus être approuvé." });

		adjustment.Status = request.Status;
		adjustment.ReviewedById = userId;
		adjustment.ReviewedAt = DateTime.UtcNow;

		await _db.SaveChangesAsync();

		return NoContent();
	}

	private async Task<bool> CanManageAsync(Employee employee, int userId) =>
		await _scope.CanAdministerSiteAsync(employee.SiteId) || employee.ManagerId == userId;


	private async Task<bool> CanReadAsync(Employee employee, int userId) =>
		employee.Id == userId || await CanManageAsync(employee, userId);

	/// <summary>Annulable : actif, pas passé, et pas « soi-même avec un ajustement déjà approuvé ».</summary>
	private static bool CanCancel(ScheduleAdjustment adjustment, bool isSelf, DateOnly today) =>
		(adjustment.Status == LeaveStatus.EnAttente || adjustment.Status == LeaveStatus.Approuve)
		&& adjustment.Date >= today
		&& !(isSelf && adjustment.Status == LeaveStatus.Approuve);

	private static AdjustmentResponse ToResponse(ScheduleAdjustment adjustment, Employee creator, bool isSelf, DateOnly today) => new()
	{
		Id = adjustment.Id,
		Date = adjustment.Date,
		StartTime = adjustment.StartTime,
		EndTime = adjustment.EndTime,
		BreakMinutes = adjustment.BreakMinutes,
		Reason = adjustment.Reason,
		Status = adjustment.Status,
		CreatedByFirstName = creator.FirstName,
		CreatedByLastName = creator.LastName,
		CanCancel = CanCancel(adjustment, isSelf, today),
	};
}