using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;

    public EmployeesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<EmployeeListItemResponse>>> Lister([FromQuery] string? search)
    {
        var query = _db.Employees
            .AsNoTracking()
            .Where(e => e.IsActive);

        query = ApplySearch(query, search);

        var photosBase = $"{Request.Scheme}://{Request.Host}/photos/";

        var employees = await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Select(e => new EmployeeListItemResponse
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                JobTitle = e.JobTitle,
                Departement = e.Departement,
                PhoneNumber = e.PhoneNumber ?? string.Empty,
                PhotoUrl = e.PhotoFileName == null ? null : photosBase + e.PhotoFileName,
            })
            .ToListAsync();

        return Ok(employees);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmployeeDetailResponse>> GetEmployeeById(int id)
    {
        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null || !employee.IsActive)
            return NotFound();

        var employeeDetails = new EmployeeDetailResponse
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            JobTitle = employee.JobTitle,
            Departement = employee.Departement,
            PhoneNumber = employee.PhoneNumber ?? string.Empty,
            ManagerId = employee.ManagerId,
            PhotoUrl = employee.PhotoFileName == null
                ? null
                : $"{Request.Scheme}://{Request.Host}/photos/{employee.PhotoFileName}",
        };

        if (employee.ManagerId is not null)
        {
            var manager = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employee.ManagerId);

            if (manager is not null)
            {
                employeeDetails.ManagerFirstName = manager.FirstName;
                employeeDetails.ManagerLastName = manager.LastName;
            }
        }

        return Ok(employeeDetails);
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<EmployeeLookupResponse>> Lookup(
        [FromQuery] string? search,
        [FromQuery] int limit = 10,
        [FromQuery] int? excludeTeamOf = null)
    {
        limit = Math.Clamp(limit, 1, 50);

        var query = _db.Employees
            .AsNoTracking()
            .Where(e => e.IsActive);

        query = ApplySearch(query, search);

        if (excludeTeamOf is > 0)
        {
            var team = await GetTeamIdsAsync(excludeTeamOf.Value);
            query = query.Where(e => !team.Contains(e.Id));
        }

        var items = await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ThenBy(e => e.Id)
            .Take(limit + 1)
            .Select(e => new EmployeeLookupItem
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                JobTitle = e.JobTitle,
                Departement = e.Departement,
            })
            .ToListAsync();

        var hasMore = items.Count > limit;
        if (hasMore) items.RemoveAt(items.Count - 1);

        return Ok(new EmployeeLookupResponse { Items = items, HasMore = hasMore });
    }

    private static IQueryable<Employee> ApplySearch(IQueryable<Employee> query, string? search)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            string filter = search.Trim().ToLowerInvariant();

            Departement? depFilter = Enum.TryParse<Departement>(filter, true, out var d) ? d : null;

            query = query.Where(e =>
                e.FirstName.ToLower().Contains(filter)
                || e.LastName.ToLower().Contains(filter)
                || e.Email.ToLower().Contains(filter)
                || e.JobTitle.ToLower().Contains(filter)
                || (depFilter != null && e.Departement == depFilter));
        }

        return query;
    }

    private async Task<HashSet<int>> GetTeamIdsAsync(int rootId)
    {
        // Toutes les paires (Id, ManagerId), y compris les employés INACTIFS :
        // un inactif peut avoir des subordonnés actifs, la chaîne doit passer par lui.
        var links = await _db.Employees
            .AsNoTracking()
            .Select(e => new { e.Id, e.ManagerId })
            .ToListAsync();

        // Pour chaque manager, la liste de ses subordonnés directs.
        var reportsByManager = links
            .Where(l => l.ManagerId is not null)
            .ToLookup(l => l.ManagerId!.Value, l => l.Id);

        // Parcours en largeur; le HashSet évite de tourner en rond si la base contenait une boucle.
        var team = new HashSet<int> { rootId };

        var queue = new Queue<int>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            var subordinates = reportsByManager[id];
            foreach (var sub in subordinates)
            {
                if (team.Add(sub))
                    queue.Enqueue(sub);
            }
        }

        return team;
    }
}

