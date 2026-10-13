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

    private const int MaxSickLeaveBackdateDays = 14;

	private static readonly CultureInfo FrenchCanada = new("fr-CA");

    public LeavesController(AppDbContext db, IAppClock clock)
    {
        _db = db;
        _clock = clock;
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestResponse>> Create(CreateLeaveRequest request)
    {
        var employeeIdClaim = User.FindFirstValue("employeeId");

        if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized();

        if (request.EndDate.Date < request.StartDate.Date)
            return BadRequest(new { message = "La date de fin ne peut pas être avant la date de début." });

		var minStartDate = (request.Type == LeaveType.Maladie) ? _clock.Today.AddDays(-MaxSickLeaveBackdateDays) : _clock.Today;

		if (request.StartDate.Date < minStartDate)
		{
			if (request.Type == LeaveType.Maladie)
            	return BadRequest(new { message = $"Un congé maladie peut être rétroactif de {MaxSickLeaveBackdateDays} jours au maximum." });
			else
            	return BadRequest(new { message = "Un congé ne peut pas commencer dans le passé." });
		}

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

        return Created($"/api/leaves/{leave.Id}", new LeaveRequestResponse
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
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

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

        var isAdmin = User.IsInRole("Admin");

        var leaves = await _db.LeaveRequests
            .AsNoTracking()
            .Where(l => l.Status == LeaveStatus.EnAttente)
            .Where(l => isAdmin || l.Employee.ManagerId == employeeId)
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

        var isAdmin = User.IsInRole("Admin");
        var isManager = leave.Employee.ManagerId == employeeId;

        if (!isAdmin && !isManager)
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

    [HttpGet("calendar")]
    public async Task<ActionResult<List<LeaveRequestResponse>>> Calendar(
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

        return Ok(leaves);
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
}

