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
[Route("api/sites/{siteId:int}/holidays")]
public class HolidaysController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly SiteClock _siteClock;

    public HolidaysController(AppDbContext db, SiteClock siteClock)
    {
        _db = db;
		_siteClock = siteClock;
    }

	[HttpGet]
	public async Task<ActionResult<List<HolidayResponse>>> List(int siteId, [FromQuery] int? year)
	{
		var site = await _db.Sites.AsNoTracking().FirstOrDefaultAsync(e => e.Id == siteId);

		if (site is null)
			return NotFound();

		if (year != null && (year < 2000 || year > 2100))
			return BadRequest(new { message = "Année invalide." });

		var today = await _siteClock.TodayForSiteAsync(siteId);

		var y = year ?? today.Year;

		var holidays = await _db.Holidays
			.AsNoTracking()
			.Where(h => h.Date.Year == y && h.SiteId == siteId)
			.OrderBy(h => h.Date)
			.Select(h => new HolidayResponse
			{
				Id = h.Id,
				Date = h.Date,
				Name = h.Name,
				IsEditable = (h.Date >= today)
			})
			.ToListAsync();

		return Ok(holidays);
	}

	[HttpPost]
	[Authorize(Roles = "Admin")]
	public async Task<ActionResult<HolidayResponse>> Create(int siteId, SaveHolidayRequest request)
	{
		var site = await _db.Sites.AsNoTracking().FirstOrDefaultAsync(e => e.Id == siteId);

		if (site is null)
			return NotFound();

		if (!await CanManageSiteAsync(siteId))
			return NotFound();

		var today = await _siteClock.TodayForSiteAsync(siteId);
		if (request.Date < today)
			return BadRequest(new { message = "Cette date est passée : choisis une date ultérieure." });

		var duplicate = await _db.Holidays
			.AnyAsync(h => h.SiteId == siteId && h.Date == request.Date);

		if (duplicate)
			return Conflict(new { message = "Un jour férié existe déjà pour cette date." });

		var holiday = new Holiday
		{
			SiteId = siteId,
			Date = request.Date!.Value,
			Name = request.Name.Trim()
		};

		_db.Holidays.Add(holiday);
		await _db.SaveChangesAsync();

		var response = new HolidayResponse
		{
			Id = holiday.Id,
			Date = holiday.Date,
			Name = holiday.Name,
			IsEditable = true
		};

		return Created($"api/sites/{siteId}/holidays/{response.Id}", response);
	}

	[HttpPut("{id:int}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Put(int siteId, int id, SaveHolidayRequest request)
	{
		var site = await _db.Sites.AsNoTracking().FirstOrDefaultAsync(e => e.Id == siteId);

		if (site is null)
			return NotFound();

		if (!await CanManageSiteAsync(siteId))
			return NotFound();

		var holiday = await _db.Holidays
			.FirstOrDefaultAsync(h => h.Id == id && h.SiteId == siteId);

		if (holiday is null)
			return NotFound();

		var today = await _siteClock.TodayForSiteAsync(siteId);
		
		if (holiday.Date < today)
			return BadRequest(new { message = "Ce férié est passé : il ne peut plus changer." });

		if (request.Date < today)
			return BadRequest(new { message = "Cette date est passée : choisis une date ultérieure." });

		var duplicate = await _db.Holidays
			.AnyAsync(h => h.SiteId == siteId && h.Date == request.Date && h.Id != id);

		if (duplicate)
			return Conflict(new { message = "Un jour férié existe déjà pour cette date." });

		holiday.Date = request.Date!.Value;
		holiday.Name = request.Name.Trim();

		await _db.SaveChangesAsync();

		return NoContent();
	}

	[HttpDelete("{id:int}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Delete(int siteId, int id)
	{
		var site = await _db.Sites.AsNoTracking().FirstOrDefaultAsync(e => e.Id == siteId);

		if (site is null)
			return NotFound();

		if (!await CanManageSiteAsync(siteId))
			return NotFound();

		var holiday = await _db.Holidays
			.FirstOrDefaultAsync(h => h.Id == id && h.SiteId == siteId);

		if (holiday is null)
			return NotFound();

		var today = await _siteClock.TodayForSiteAsync(siteId);
		
		if (holiday.Date < today)
			return BadRequest(new { message = "Ce férié est passé : il ne peut plus changer." });

		_db.Holidays.Remove(holiday);
		await _db.SaveChangesAsync();

		return NoContent();
	}

	/// <summary>Un Admin ne gère que les fériés de son propre site (R23).</summary>
	private async Task<bool> CanManageSiteAsync(int siteId)
	{
		var employeeId = int.Parse(User.FindFirstValue("employeeId")!);
		var callerSiteId = await _db.Employees.Where(e => e.Id == employeeId).Select(e => e.SiteId).FirstAsync();
		return callerSiteId == siteId;
	}
}