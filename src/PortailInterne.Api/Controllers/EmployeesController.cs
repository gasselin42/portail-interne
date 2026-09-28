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
                Departement = e.Departement!.Value,
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
            Departement = employee.Departement!.Value,
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
}

