using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Services;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/approvals")]
public class ApprovalsController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly AccessScope _scope;
	private readonly SiteClock _clock;

	public ApprovalsController(AppDbContext db, AccessScope scope, SiteClock clock)
	{
		_db = db;
		_scope = scope;
		_clock = clock;
	}

	[HttpGet]
	public async Task<ActionResult<ApprovalQueueResponse>> Get()
	{
		var employeeIdClaim = User.FindFirstValue("employeeId");

        if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized();

		var callerSiteId = await _scope.CallerSiteIdAsync();
		var isGlobalAdmin = _scope.IsGlobalAdmin;
		var isAdmin = _scope.IsAdmin;

		var adjustments = await _db.ScheduleAdjustments
            .AsNoTracking()
            .Where(a => a.Status == LeaveStatus.EnAttente
				&& (isGlobalAdmin
				|| (isAdmin && a.Employee.SiteId == callerSiteId)
				|| a.Employee.ManagerId == employeeId)
				&& a.EmployeeId != employeeId
				&& a.Employee.IsActive)
            .OrderBy(a => a.Date)
            .Select(a => new PendingAdjustmentItem
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeFirstName = a.Employee.FirstName,
                EmployeeLastName = a.Employee.LastName,
                Date = a.Date,
				StartTime = a.StartTime,
				EndTime = a.EndTime,
				BreakMinutes = a.BreakMinutes,
                Reason = a.Reason,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

		var absences = await _db.Absences
            .AsNoTracking()
            .Where(a => a.Status == LeaveStatus.EnAttente
				&& (isGlobalAdmin
				|| (isAdmin && a.Employee.SiteId == callerSiteId)
				|| a.Employee.ManagerId == employeeId)
				&& a.EmployeeId != employeeId
				&& a.Employee.IsActive)
            .OrderBy(a => a.Date)
			.ThenBy(a => a.PlannedStart)
            .Select(a => new PendingAbsenceItem
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                EmployeeFirstName = a.Employee.FirstName,
                EmployeeLastName = a.Employee.LastName,
                Date = a.Date,
				PlannedStart = a.PlannedStart,
				PlannedEnd = a.PlannedEnd,
                Reason = a.Reason,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

		var nowByEmployee = new Dictionary<int, (DateOnly Today, TimeOnly Now)>();
		foreach (var id in adjustments.Select(a => a.EmployeeId).Concat(absences.Select(a => a.EmployeeId)).Distinct())
			nowByEmployee[id] = await _clock.NowForAsync(id);

		foreach (var adjustment in adjustments)
			adjustment.CanApprove = adjustment.Date >= nowByEmployee[adjustment.EmployeeId].Today;

		foreach (var absence in absences)
		{
			var (today, now) = nowByEmployee[absence.EmployeeId];
			absence.CanApprove = absence.Date > today || (absence.Date == today && absence.PlannedEnd > now);
		}

		return Ok(new ApprovalQueueResponse { Adjustments = adjustments, Absences = absences });
	}
}