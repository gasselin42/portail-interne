using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/me/preferences")]
public class PreferencesController : ControllerBase
{
	private readonly AppDbContext _db;

	public PreferencesController(AppDbContext db)
	{
		_db = db;
	}

	[HttpGet]
	public async Task<ActionResult<PreferencesDto>> Get()
	{
		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var userId))
            return Unauthorized();

		var userPrefs = await _db.EmployeePreferences.FindAsync(userId);
		
		return Ok(new PreferencesDto { CalendarHourRange = userPrefs?.CalendarHourRange ?? CalendarHourRange.WorkHours, CalendarDensity = userPrefs?.CalendarDensity ?? CalendarDensity.Normal });
	}

	[HttpPut]
	public async Task<ActionResult<PreferencesDto>> Put(PreferencesDto request)
	{
		if (!Enum.IsDefined(request.CalendarHourRange))
			return BadRequest(new { message = "Plage d'heures inconnue." });

		if (!Enum.IsDefined(request.CalendarDensity))
			return BadRequest(new { message = "Densité inconnue." });

		var employeeIdClaim = User.FindFirstValue("employeeId");

		if (employeeIdClaim is null || !int.TryParse(employeeIdClaim, out var userId))
            return Unauthorized();

		var userPrefs = await _db.EmployeePreferences.FindAsync(userId);

		if (userPrefs is null)
			_db.EmployeePreferences.Add(new EmployeePreferences { EmployeeId = userId, CalendarHourRange = request.CalendarHourRange, CalendarDensity = request.CalendarDensity });
		else
		{
			userPrefs.CalendarHourRange = request.CalendarHourRange;
			userPrefs.CalendarDensity = request.CalendarDensity;
		}

		await _db.SaveChangesAsync();

		return Ok(new PreferencesDto { CalendarHourRange = request.CalendarHourRange, CalendarDensity = request.CalendarDensity });
	}
}