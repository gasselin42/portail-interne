using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Controllers;

[ApiController]
[Authorize]
[Route("/api/leaves")]
public class LeavesController : ControllerBase
{
    private readonly AppDbContext _db;

    private const int MaxSickLeaveBackdateDays = 14;

    public LeavesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestResponse>> Create(CreateLeaveRequest request)
    {
        var employeeIdClaim = User.FindFirstValue("employeeId");

        if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized();

        if (request.EndDate.Date < request.StartDate.Date)
            return BadRequest(new { message = "La date de fin ne peut pas être avant la date de début." });

		var minStartDate = (request.Type == LeaveType.Maladie) ? DateTime.Today.AddDays(-MaxSickLeaveBackdateDays) : DateTime.Today;

		if (request.StartDate.Date < minStartDate)
		{
			if (request.Type == LeaveType.Maladie)
            	return BadRequest(new { message = $"Un congé maladie peut être rétroactif de {MaxSickLeaveBackdateDays} jours au maximum." });
			else
            	return BadRequest(new { message = "Un congé ne peut pas commencer dans le passé." });
		}

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
            .OrderByDescending(l => l.StartDate)
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

        leave.Status = request.Status;
        leave.ReviewedById = employeeId;
        leave.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("calendar")]
    public async Task<ActionResult<List<LeaveRequestResponse>>> Calendar(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var employeeIdClaim = User.FindFirstValue("employeeId");

        if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var employeeId))
            return Unauthorized();

        if (to.Date < from.Date)
            return BadRequest(new { message = "La date de fin doit être après la date de début." });

        List<LeaveRequestResponse> leaves = await _db.LeaveRequests
            .AsNoTracking()
            .Where(l => l.EmployeeId == employeeId)
            .Where(l => l.StartDate.Date <= to.Date && l.EndDate.Date >= from.Date)
            .Where(l => l.Status == LeaveStatus.EnAttente || l.Status == LeaveStatus.Approuve)
            .OrderByDescending(l => l.StartDate)
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
}

