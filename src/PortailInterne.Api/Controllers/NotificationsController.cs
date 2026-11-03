using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Services;

namespace PortailInterne.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly AccessScope _scope;
	private readonly TimeProvider _time;

	public NotificationsController(AppDbContext db, AccessScope scope, TimeProvider time)
	{
		_db = db;
		_scope = scope;
		_time = time;
	}

	[HttpGet]
	public async Task<ActionResult<NotificationListResponse>> List(
		[FromQuery] int limit = 20,
		[FromQuery] int? beforeId = null)
	{
		var employeeId = _scope.CallerEmployeeId;

		if (limit < 1 || limit > 50)
			return BadRequest(new { message = "Le nombre doit être entre 1 et 50." });
		
		var notifs = await _db.Notifications
			.Where(n => n.RecipientId == employeeId && (beforeId == null || n.Id < beforeId))
			.OrderByDescending(n => n.Id)
			.Take(limit + 1)
			.ToListAsync();

		var hasMore = notifs.Count > limit;
		if (hasMore)
			notifs.RemoveAt(limit);

		var response = notifs.Select(n => new NotificationResponse
		{
			Id = n.Id,
			Type = n.Type,
			Message = n.Message,
			Link = n.Link,
			CreatedAt = n.CreatedAt,
			ReadAt = n.ReadAt
		}).ToList();

		return Ok(new NotificationListResponse { Items = response, HasMore = hasMore });
	}

	[HttpGet("unread-count")]
	public async Task<ActionResult> UnreadCount()
	{
		var employeeId = _scope.CallerEmployeeId;
		var count = await _db.Notifications.Where(n => n.RecipientId == employeeId && n.ReadAt == null).CountAsync();
		return Ok(new { count });
	}

	[HttpPost("{id:int}/read")]
	public async Task<IActionResult> Read(int id)
	{
		var employeeId = _scope.CallerEmployeeId;
		
		var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id);

		if (notif is null)
			return NotFound();

		if (notif.RecipientId != employeeId)
			return NotFound();

		if (notif.ReadAt is null)
			notif.ReadAt = _time.GetUtcNow().UtcDateTime;

		await _db.SaveChangesAsync();

		return NoContent();
	}

	[HttpPost("read-all")]
	public async Task<IActionResult> ReadAll()
	{
		var employeeId = _scope.CallerEmployeeId;
		
		await _db.Notifications
			.Where(n => n.RecipientId == employeeId && n.ReadAt == null)
			.ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, _time.GetUtcNow().UtcDateTime));

		return NoContent();
	}
}