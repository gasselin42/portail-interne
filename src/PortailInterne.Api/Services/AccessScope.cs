using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

/// <summary>Droits d'administration de la personne connectée, selon les sites (R23).</summary>
public class AccessScope
{
	private readonly AppDbContext _db;
	private readonly IHttpContextAccessor _http;
	private int? _callerSiteId; // lu une seule fois par requête

	public AccessScope(AppDbContext db, IHttpContextAccessor http)
	{
		_db = db;
		_http = http;
	}

	private ClaimsPrincipal User => _http.HttpContext!.User;

	public int CallerEmployeeId => int.Parse(
		User.FindFirstValue("employeeId")
			?? throw new InvalidOperationException("AccessScope s'utilise seulement dans un endpoint protégé par [Authorize]."));

	public bool IsGlobalAdmin => User.IsInRole("GlobalAdmin");
	public bool IsAdmin => IsGlobalAdmin || User.IsInRole("Admin");

	/// <summary>Le site de la personne connectée.</summary>
	public async Task<int> CallerSiteIdAsync()
	{
		// '??=' n'exécute la requête que si _callerSiteId est encore null.
		// Le site est donc lu une seule fois par requête, même si plusieurs vérifications en ont besoin.
		_callerSiteId ??= await _db.Employees
				.Where(e => e.Id == CallerEmployeeId)
				.Select(e => e.SiteId)
				.FirstAsync();
		return _callerSiteId.Value;
	}

	/// <summary>Admin global : tous les sites. Admin : seulement le sien.</summary>
	public async Task<bool> CanAdministerSiteAsync(int siteId) =>
		IsGlobalAdmin || (IsAdmin && await CallerSiteIdAsync() == siteId);

	/// <summary>Admin global : tous les employés. Admin : tous les employés de son site. Ou tout User qui est Manager de l'Employee.</summary>
	public async Task<bool> CanManageAsync(Employee employee) =>
		employee.ManagerId == CallerEmployeeId || await CanAdministerSiteAsync(employee.SiteId);

	/// <summary>Admin global : tous les employés. Admin : tous les employés de son site. Ou tout User sur ses propres données.</summary>
	public async Task<bool> CanReadAsync(Employee employee) =>
		employee.Id == CallerEmployeeId || await CanManageAsync(employee);

	public async Task<bool> CanSeeTeamAsync()
	{
		if (IsAdmin) return true;

		var hasSubordinates = await _db.Employees.AnyAsync(e => e.ManagerId == CallerEmployeeId && e.IsActive);
		if (hasSubordinates) return true;

		var siteId = await CallerSiteIdAsync();
		var visibility = await _db.Sites
			.Where(s => s.Id == siteId)
			.Select(s => s.TeamVisibility)
			.FirstAsync();

		if (visibility == TeamVisibility.Team)
		{
			var managerId = await _db.Employees
				.Where(e => e.Id == CallerEmployeeId)
				.Select(e => e.ManagerId)
				.FirstAsync();

			if (managerId is null) return false;

			return await _db.Employees.AnyAsync(e => e.IsActive
				&& e.Id != CallerEmployeeId
				&& (e.Id == managerId || e.ManagerId == managerId));

		}

		return visibility != TeamVisibility.Nobody;
	}
}