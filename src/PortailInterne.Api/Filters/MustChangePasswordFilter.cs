using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;

namespace PortailInterne.Api.Filters;

public class MustChangePasswordFilter : IAsyncActionFilter
{
	public async Task OnActionExecutionAsync(
		ActionExecutingContext context,
		ActionExecutionDelegate next)
	{
		var user = context.HttpContext.User;

		if (user.Identity?.IsAuthenticated != true)
		{
			await next();
			return;
		}

		var path = context.HttpContext.Request.Path.Value ?? string.Empty;

		if (path.Equals("/api/auth/change-password", StringComparison.OrdinalIgnoreCase)
			|| path.Equals("/api/auth/me", StringComparison.OrdinalIgnoreCase))
		{
			await next();
			return;
		}

		var userAccountIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);

		if (userAccountIdClaim is null || !int.TryParse(userAccountIdClaim, out var userAccountId))
		{
			await next();
			return;
		}

		var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

		var account = await db.UserAccounts
			.AsNoTracking()
			.Where(u => u.Id == userAccountId)
			.Select(u => new { u.MustChangePassword, u.Role })
			.FirstOrDefaultAsync();

		if (account is null || account.Role is null)
		{
			await next();
			return;
		}

		var roleClaim = user.FindFirstValue(ClaimTypes.Role);
		if (roleClaim is null || account.Role.Value.ToString() != roleClaim)
		{
			context.Result = new ObjectResult(new
			{
				message = "Votre rôle a changé. Reconnectez-vous."
			})
			{
				StatusCode = StatusCodes.Status401Unauthorized
			};
			return;
		}

		if (path.Equals("/api/auth/change-password", StringComparison.OrdinalIgnoreCase)
			|| path.Equals("/api/auth/me", StringComparison.OrdinalIgnoreCase))
		{
			await next();
			return;
		}

		if (account.MustChangePassword)
		{
			context.Result = new ObjectResult(new
			{
				message = "Vous devez changer votre mot de passe avant de continuer."
			})
			{
				StatusCode = StatusCodes.Status403Forbidden
			};
			return;
		}

		await next();
	}
}
