using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Services;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/team/week")]
public class TeamController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly EffectiveScheduleService _schedule;
	private readonly SiteClock _clock;
	private readonly AccessScope _scope;

	public TeamController(AppDbContext db, EffectiveScheduleService schedule, SiteClock clock, AccessScope scope)
	{
		_db = db;
		_schedule = schedule;
		_clock = clock;
		_scope = scope;
	}

	[HttpGet]
	public async Task<ActionResult<List<TeamMemberResponse>>> List(
		[FromQuery, BindRequired] DateOnly from,
		[FromQuery, BindRequired] DateOnly to,
		[FromQuery] int? siteId
	)
	{
		if (to < from)
			return BadRequest(new { message = "La date de début doit être avant la date de fin." });

		if (to.DayNumber - from.DayNumber >= 7)
			return BadRequest(new { message = "Sélectionne une plage de 7 jours au maximum."});

		if (!await _scope.CanSeeTeamAsync())
			return Forbid();

		if (siteId is not null && !await _scope.CanAdministerSiteAsync(siteId.Value))
			return Forbid();

		if (siteId is not null && !await _db.Sites.AnyAsync(s => s.Id == siteId))
			return NotFound();
		
		var site = siteId ?? await _scope.CallerSiteIdAsync();

		Dictionary<int, bool> members = [];
		var callerId = _scope.CallerEmployeeId;

		if (await _scope.CanAdministerSiteAsync(site))
		{
			members = await _db.Employees
				.Where(e => e.SiteId == site && e.IsActive)
				.ToDictionaryAsync(e => e.Id, e => true);
		}
		else
		{
			members.Add(callerId, true);
			
			await _db.Employees
				.Where(e => e.ManagerId == callerId && e.IsActive)
				.ForEachAsync(e => members.Add(e.Id, true));
			
			var visibility = await _db.Sites
				.Where(s => s.Id == site)
				.Select(s => s.TeamVisibility)
				.FirstAsync();
			
			if (visibility == TeamVisibility.Team)
			{
				var managerId = await _db.Employees
					.Where(e => e.Id == callerId)
					.Select(e => e.ManagerId)
					.FirstAsync();
				
				if (managerId is not null)
				{
					await _db.Employees
						.Where(e => e.IsActive
							&& (e.Id == managerId
								|| e.ManagerId == managerId))
						.ForEachAsync(e => members.TryAdd(e.Id, false));
				}
			}
			else if (visibility == TeamVisibility.Site)
			{
				await _db.Employees
					.Where(e => e.IsActive && e.SiteId == site)
					.ForEachAsync(e => members.TryAdd(e.Id, false));
			}
		}

		var ids = members.Keys.ToList();
		var sorted = await _db.Employees
			.Where(e => ids.Contains(e.Id))
			.OrderBy(e => e.Id != callerId)
			.ThenBy(e => e.LastName)
			.ThenBy(e => e.FirstName)
			.ToListAsync();

		List<TeamMemberResponse> liste = [];
		foreach (var member in sorted)
		{
			var (today, now) = await _clock.NowForAsync(member.Id);
			var effectiveDays = await _schedule.ForAsync(member.Id, from, to, applyLeaves: true);
			var details = members[member.Id];
			var dayResponses = effectiveDays.Select(d => CalendarMapping.ToDayResponse(d, today, now, details)).ToList();
			liste.Add(new TeamMemberResponse
			{
				EmployeeId = member.Id,
				FirstName = member.FirstName,
				LastName = member.LastName,
				JobTitle = member.JobTitle,
				Details = details,
				Days = dayResponses
			});
		}

		return Ok(liste);
	}
}