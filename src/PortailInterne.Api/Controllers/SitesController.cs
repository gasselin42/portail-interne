using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/sites")]
public class SitesController : ControllerBase
{
	private readonly AppDbContext _db;

	public SitesController(AppDbContext db)
	{
		_db = db;
	}

	[HttpGet]
	public async Task<ActionResult<List<Site>>> List()
	{
		return Ok(await _db.Sites.OrderBy(s => s.Name).ToListAsync());
	}

	[Authorize(Roles = Roles.GlobalAdmin)]
	[HttpPost]
	public async Task<ActionResult<Site>> Create(SaveSiteRequest request)
	{
		if (!TimeZoneInfo.TryFindSystemTimeZoneById(request.TimeZoneId.Trim(), out _))
			return BadRequest(new { message = "Fuseau horaire inconnu. Utilise un identifiant IANA, par exemple « Europe/Paris »." });

		var duplicate = await _db.Sites
			.AnyAsync(s => s.Name.ToLower() == request.Name.Trim().ToLower());

		if (duplicate)
			return Conflict(new { message = "Un site existe déjà avec ce nom." });

		var site = new Site
		{
			Name = request.Name.Trim(),
			TimeZoneId = request.TimeZoneId.Trim()	
		};

		_db.Sites.Add(site);
		await _db.SaveChangesAsync();

		return Created($"api/sites/{site.Id}", site);
	}

	[Authorize(Roles = Roles.GlobalAdmin)]
	[HttpPut("{id:int}")]
	public async Task<IActionResult> Modify(int id, SaveSiteRequest request)
	{
		var site = await _db.Sites.FirstOrDefaultAsync(s => s.Id == id);

		if (site is null)
			return NotFound();

		if (!TimeZoneInfo.TryFindSystemTimeZoneById(request.TimeZoneId.Trim(), out _))
			return BadRequest(new { message = "Fuseau horaire inconnu. Utilise un identifiant IANA, par exemple « Europe/Paris »." });

		var duplicate = await _db.Sites
			.AnyAsync(s => s.Name.ToLower() == request.Name.Trim().ToLower() && s.Id != id);

		if (duplicate)
			return Conflict(new { message = "Un site existe déjà avec ce nom." });

		site.Name = request.Name.Trim();
		site.TimeZoneId = request.TimeZoneId.Trim();

		await _db.SaveChangesAsync();

		return NoContent();
	}
}