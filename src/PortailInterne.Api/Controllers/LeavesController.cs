using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using System.Globalization;

using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Controllers;

[ApiController]
[Authorize]
[Route("/api/leaves")]
public class LeavesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAppClock _clock;
	private readonly SiteClock _siteClock;
	private readonly AccessScope _scope;
	private readonly LeaveUsageService _leaveUsage;
	private readonly EffectiveScheduleService _schedule;

    private const int MaxSickLeaveBackdateDays = 14;

	private static readonly CultureInfo FrenchCanada = new("fr-CA");

    public LeavesController(AppDbContext db, IAppClock clock, SiteClock siteClock, AccessScope scope, LeaveUsageService leaveUsage, EffectiveScheduleService schedule)
    {
        _db = db;
        _clock = clock;
		_siteClock = siteClock;
		_scope = scope;
		_leaveUsage = leaveUsage;
		_schedule = schedule;
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestResponse>> Create(CreateLeaveRequest request)
    {
        var employeeIdClaim = User.FindFirstValue("employeeId");

        if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized();
		
		var today = await _siteClock.TodayForAsync(employeeId);

		var validate = ValidateDates(request.StartDate, request.EndDate, request.Type, today);

		if (validate is not null)
			return BadRequest(new { message = validate });

		if ((await _schedule.WorkedDaysAsync(employeeId, DateOnly.FromDateTime(request.StartDate), DateOnly.FromDateTime(request.EndDate))).Count == 0)
			return BadRequest(new { message = "Aucun jour travaillé durant cette période." });

		var overlap = await FindOverlapAsync(employeeId, request.StartDate, request.EndDate, [LeaveStatus.EnAttente, LeaveStatus.Approuve]);

		if (overlap is not null)
			return Conflict(new { message = $"Tu as déjà une demande {DescribePeriod(overlap.StartDate, overlap.EndDate)} qui chevauche cette période." });

        var leave = new LeaveRequest
        {
            EmployeeId = employeeId,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            Type = request.Type,
            Status = LeaveStatus.EnAttente,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _db.LeaveRequests.Add(leave);
        await _db.SaveChangesAsync();

		var response = new LeaveRequestResponse
        {
            Id = leave.Id,
            EmployeeId = leave.EmployeeId,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            Type = leave.Type,
            Status = leave.Status,
            Reason = leave.Reason,
            CreatedAt = leave.CreatedAt
        };

		await _leaveUsage.AddUsageAsync([response]);

        return Created($"/api/leaves/{leave.Id}", response);
    }

	[HttpPost("declare")]
	public async Task<ActionResult<LeaveRequestResponse>> DeclareSickLeave(DeclareSickLeaveRequest request)
	{
		var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId);

        if (employee is null || !employee.IsActive)
            return NotFound();

		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized();

		var caller = await _db.Employees.FindAsync(employeeId);

		if (employeeId == employee.Id)
			return BadRequest(new { message = "Pour toi-même, utilise une demande normale." });

		if (!await _scope.CanManageAsync(employee))
			return NotFound();

		var today = await _siteClock.TodayForAsync(employee.Id);

		var validate = ValidateDates(request.StartDate, request.EndDate, LeaveType.Maladie, today);

		if (validate is not null)
			return BadRequest(new { message = validate });

		if ((await _schedule.WorkedDaysAsync(employee.Id, DateOnly.FromDateTime(request.StartDate), DateOnly.FromDateTime(request.EndDate))).Count == 0)
			return BadRequest(new { message = $"Aucun jour travaillé par {employee.FirstName} durant cette période." });
		
		var overlap = await FindOverlapAsync(request.EmployeeId, request.StartDate, request.EndDate, [LeaveStatus.EnAttente, LeaveStatus.Approuve]);

		if (overlap is not null)
			return Conflict(new { message = $"{employee.FirstName} a déjà une demande {DescribePeriod(overlap.StartDate, overlap.EndDate)} qui chevauche cette période." });
	
		var leave = new LeaveRequest
        {
            EmployeeId = employee.Id,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate.Date,
            Type = LeaveType.Maladie,
            Status = LeaveStatus.Approuve,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
            CreatedAt = DateTime.UtcNow,
			CreatedById = employeeId,
			ReviewedById = employeeId,
			ReviewedAt = DateTime.UtcNow
        };

        _db.LeaveRequests.Add(leave);
        await _db.SaveChangesAsync();

		var response = new LeaveRequestResponse
        {
            Id = leave.Id,
            EmployeeId = leave.EmployeeId,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            Type = leave.Type,
            Status = leave.Status,
            Reason = leave.Reason,
            CreatedAt = leave.CreatedAt,
			CreatedByFirstName = caller!.FirstName,
			CreatedByLastName = caller!.LastName
        };

		await _leaveUsage.AddUsageAsync([response]);

		return Created($"/api/leaves/{leave.Id}", response); 
	}

    [HttpGet]
    public async Task<ActionResult<List<LeaveRequestResponse>>> ListMine()
    {
        var employeeIdClaim = User.FindFirstValue("employeeId");

        if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized();

        var leaves = await _db.LeaveRequests
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId)
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
                CreatedAt = l.CreatedAt,
				CreatedByFirstName = l.CreatedBy != null ? l.CreatedBy.FirstName : null,
				CreatedByLastName = l.CreatedBy != null ? l.CreatedBy.LastName : null
            })
            .ToListAsync();

		await _leaveUsage.AddUsageAsync(leaves);

        return Ok(leaves);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeaveRequestResponse>> GetMine(int id)
    {
        var employeeIdClaim = User.FindFirstValue("employeeId");

        if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized();

        var leave = await _db.LeaveRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id && l.EmployeeId == employeeId);

        if (leave is null)
            return NotFound();

        return Ok(new LeaveRequestResponse
        {
            Id = leave.Id,
            EmployeeId = leave.EmployeeId,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            Type = leave.Type,
            Status = leave.Status,
            Reason = leave.Reason,
            CreatedAt = leave.CreatedAt
        });
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<LeaveRequestResponse>>> Pending()
    {
        var employeeIdClaim = User.FindFirstValue("employeeId");

        if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized();

		var callerSiteId = await _scope.CallerSiteIdAsync();
		var isGlobalAdmin = _scope.IsGlobalAdmin;
		var isAdmin = _scope.IsAdmin;

        var leaves = await _db.LeaveRequests
            .AsNoTracking()
            .Where(l => l.Status == LeaveStatus.EnAttente)
            .Where(l => isGlobalAdmin
				|| (isAdmin && l.Employee.SiteId == callerSiteId)
				|| l.Employee.ManagerId == employeeId)
            .Where(l => l.EmployeeId != employeeId)
            .OrderBy(l => l.StartDate)
            .ThenBy(l => l.EndDate)
            .Select(l => new LeaveRequestResponse
            {
                Id = l.Id,
                EmployeeId = l.EmployeeId,
                EmployeeFirstName = l.Employee.FirstName,
                EmployeeLastName = l.Employee.LastName,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Type = l.Type,
                Status = l.Status,
                Reason = l.Reason,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

		await _leaveUsage.AddUsageAsync(leaves);

        return Ok(leaves);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var employeeIdClaim = User.FindFirstValue("employeeId");

        if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized();

        var leave = await _db.LeaveRequests
            .FirstOrDefaultAsync(l => l.Id == id && l.EmployeeId == employeeId);

        if (leave is null)
            return NotFound();

        if (leave.Status != LeaveStatus.EnAttente)
            return BadRequest(new { message = "Seule une demande en attente peut être annulée." });

        leave.Status = LeaveStatus.Annule;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/review")]
    public async Task<IActionResult> Review(int id, ReviewLeaveRequest request)
    {
        if (request.Status is not (LeaveStatus.Approuve or LeaveStatus.Refuse))
            return BadRequest(new { message = "Le statut doit être approuvé ou refusé" });

        var employeeIdClaim = User.FindFirstValue("employeeId");

        if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized();

        var leave = await _db.LeaveRequests
            .Include(l => l.Employee)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (leave is null)
            return NotFound();

        if (leave.EmployeeId == employeeId)
            return BadRequest(new { message = "Vous ne pouvez pas traiter votre propre demande." });

        if (!await _scope.CanManageAsync(leave.Employee))
            return NotFound();

        if (leave.Status != LeaveStatus.EnAttente)
            return BadRequest(new { message = "Seule une demande en attente peut être traitée." });

		if (request.Status == LeaveStatus.Approuve)
		{
			var overlap = await FindOverlapAsync(leave.EmployeeId, leave.StartDate, leave.EndDate, [LeaveStatus.Approuve], leave.Id);

			if (overlap is not null)
				return Conflict(new { message = $"Cet employé a déjà un congé approuvé {DescribePeriod(overlap.StartDate, overlap.EndDate)} qui chevauche cette demande." });
		}

        leave.Status = request.Status;
        leave.ReviewedById = employeeId;
        leave.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

	[HttpGet("balance")]
	public async Task<ActionResult<LeaveBalanceResponse>> Balance([FromQuery] int? year)
	{
		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized();

		var y = year ?? (await _siteClock.TodayForAsync(employeeId)).Year;

		if (y < 2000 || y > 2100)
			return BadRequest(new { message = "Année invalide." });

		var annualDays = await _db.Employees
			.Where(e => e.Id == employeeId)
			.Select(e => e.AnnualLeaveDays)
			.FirstAsync();
		
		// Congés qui consomment le solde et touchent l'année demandée
		var leaves = await _db.LeaveRequests
			.AsNoTracking()
			.Where(l => l.EmployeeId == employeeId
					&& l.Type == LeaveType.Conge
					&& (l.Status == LeaveStatus.EnAttente || l.Status == LeaveStatus.Approuve)
					&& l.StartDate.Year <= y && l.EndDate.Year >= y)
			.Select(l => new { l.Id, l.StartDate, l.EndDate, l.Status })
			.ToListAsync();
		
		var usage = await _leaveUsage.UsageForAsync([employeeId]);

		int approved = 0, pending = 0, unpaid = 0;

		foreach (var leave in leaves)
		{
			if (!usage.TryGetValue(leave.Id, out var u))
				continue;

			var unpaidInYear = u.UnpaidDates.Count(d => d.Year == y);
			var paidInYear = u.WorkedDates.Count(d => d.Year == y) - unpaidInYear;

			if (leave.Status == LeaveStatus.Approuve)
				approved += paidInYear;
			else
				pending += paidInYear;

			unpaid += unpaidInYear;
		}

		return Ok(new LeaveBalanceResponse
		{
			Year = y,
			AnnualDays = annualDays,
			ApprovedDays = approved,
			PendingDays = pending,
			RemainingDays = Math.Max(0, annualDays - approved - pending),
			UnpaidDays = unpaid,
		});
	}

	[HttpGet("balance/preview")]
	public async Task<ActionResult<LeavePreviewResponse>> BalancePreview(
		[FromQuery, BindRequired] DateOnly startDate,
		[FromQuery, BindRequired] DateOnly endDate)
	{
		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
			return Unauthorized();

		if (endDate < startDate)
			return BadRequest(new { message = "La date de fin ne peut pas être avant la date de début." });

		var annualDays = await _db.Employees
			.Where(e => e.Id == employeeId)
			.Select(e => e.AnnualLeaveDays)
			.FirstAsync();
		
		var leaves = (await _leaveUsage.LoadBalanceLeavesAsync([employeeId])).GetValueOrDefault(employeeId, []);

		// La demande envisagée, traitée en dernier comme si elle était créée maintenant
		const int PreviewId = 0;
		leaves.Add(new LeaveBalance.BalanceLeave(
			PreviewId,
			startDate.ToDateTime(TimeOnly.MinValue),
			endDate.ToDateTime(TimeOnly.MinValue),
			DateTime.MaxValue
		));

		var minDate = DateOnly.FromDateTime(leaves.Min(l => l.StartDate));
		var maxDate = DateOnly.FromDateTime(leaves.Max(l => l.EndDate));
		var worked = await _schedule.WorkedDaysAsync(employeeId, minDate, maxDate);
		var usage = LeaveBalance.ComputeUsage(annualDays, leaves, worked.Contains)[PreviewId];

		return Ok(new LeavePreviewResponse
		{
			WorkingDays = usage.WorkingDays,
			UnpaidDates = usage.UnpaidDates,
		});
	}

	/// <summary>
	/// Première demande de l'employé, parmi les statuts donnés, qui chevauche [start, end].
	/// excludeId : la demande elle-même (à l'approbation), pour qu'elle ne se trouve pas.
	/// </summary>
	private Task<LeaveRequest?> FindOverlapAsync(int employeeId, DateTime start, DateTime end, LeaveStatus[] statuses, int? excludeId = null)
	{
		return _db.LeaveRequests
			.AsNoTracking()
			.Where(l => l.EmployeeId == employeeId
				&& statuses.Contains(l.Status)
				&& l.Id != excludeId
				&& l.StartDate <= end.Date && l.EndDate >= start.Date)
			.OrderBy(l => l.StartDate)
			.ThenBy(l => l.EndDate)
			.ThenBy(l => l.Id)
			.FirstOrDefaultAsync();
	}

	/// <summary>« le 14 oct. 2026 » ou « du 14 oct. 2026 au 16 oct. 2026 ».</summary>
	private static string DescribePeriod(DateTime start, DateTime end)
	{
		string Format(DateTime d) => d.ToString("d MMM yyyy", FrenchCanada);

		return start.Date == end.Date
			? $"le {Format(start)}"
			: $"du {Format(start)} au {Format(end)}";
	}

	private string? ValidateDates(DateTime start, DateTime end, LeaveType type, DateOnly today)
	{
		if (end.Date < start.Date)
            return "La date de fin ne peut pas être avant la date de début.";

		var minStartDate = (type == LeaveType.Maladie) ? today.AddDays(-MaxSickLeaveBackdateDays) : today;

		if (DateOnly.FromDateTime(start) < minStartDate)
		{
			if (type == LeaveType.Maladie)
            	return $"Un congé maladie peut être rétroactif de {MaxSickLeaveBackdateDays} jours au maximum.";
			else
            	return "Un congé ne peut pas commencer dans le passé.";
		}

		return null;
	}
}

