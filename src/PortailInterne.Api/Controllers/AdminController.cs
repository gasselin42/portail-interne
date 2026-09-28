using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

using PortailInterne.Api.Dtos;
using PortailInterne.Api.Data;
using PortailInterne.Api.Models;
using PortailInterne.Api.Services;
using System.Security.Claims;

namespace PortailInterne.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly PasswordService _passwords;
	private readonly IWebHostEnvironment _env;

	public AdminController(AppDbContext db, PasswordService passwords, IWebHostEnvironment env)
	{
		_db = db;
		_passwords = passwords;
		_env = env;
	}

	[HttpGet("ping")]
	public ActionResult Ping()
	{
		return Ok(new { message = "pong-admin" });
	}

	[HttpPost("employees")]
	public async Task<ActionResult<CreateEmployeeResponse>> CreateEmployee(
		[FromForm] CreateEmployeeRequest request,
		IFormFile? photo)
	{
		string email = request.Email.Trim().ToLowerInvariant();

        if (await _db.UserAccounts.AnyAsync(u => u.Email == email)
			|| await _db.Employees.AnyAsync(e => e.Email == email))
            return Conflict(new { message = "Ce email est déjà utilisé." });

		var temporaryPassword = PasswordService.GenerateTemporaryPassword();
		var passwordHash = _passwords.Hash(temporaryPassword);

		string? photoFileName = null;

		if (photo is not null && photo.Length > 0)
		{
			var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
			var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();

			if (!allowed.Contains(ext))
				return BadRequest(new { message = "Format d'image non supporté (jpeg, png webp)." });

			if (photo.Length > 2 * 1024 * 1024)
				return BadRequest(new { message = "L'image ne doit pas dépasser 2 Mo." });

			photoFileName = $"{Guid.NewGuid()}{ext}";
			var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
			var folder = Path.Combine(webRoot, "photos");
			Directory.CreateDirectory(folder);

			await using var stream = System.IO.File.Create(Path.Combine(folder, photoFileName));
			await photo.CopyToAsync(stream);
		}

		var employee = new Employee
		{
			FirstName = request.FirstName,
			LastName = request.LastName,
			Email = email,
			JobTitle = request.JobTitle,
			Departement = request.Departement,
			PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber,
			ManagerId = request.ManagerId > 0 ? request.ManagerId : null,
			IsActive = true,
			CreatedAt = DateTime.UtcNow,
			PhotoFileName = photoFileName,
			UserAccount = new UserAccount
			{
				Email = email,
				PasswordHash = passwordHash,
				Role = request.Role,
				MustChangePassword = true,
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			}
		};

		_db.Employees.Add(employee);
		await _db.SaveChangesAsync();

		return Created($"/api/admin/employees/{employee.Id}", new CreateEmployeeResponse
		{
			EmployeeId = employee.Id,
			UserAccountId = employee.UserAccount.Id,
			Email = email,
			FirstName = employee.FirstName,
			LastName = employee.LastName,
			Role = employee.UserAccount!.Role!.Value,
			MustChangePassword = true,
			TemporaryPassword = temporaryPassword
		});
    }

    [HttpGet("employees/{id:int}")]
	public async Task<ActionResult<AdminEmployeeResponse>> ListAccountFields(int id)
	{
        var employee = await _db.Employees
			.AsNoTracking()
			.Include(e => e.UserAccount)
			.FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null || employee.UserAccount is null || employee.UserAccount.Role is null)
            return NotFound();

        var employeeDetails = new AdminEmployeeResponse
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            JobTitle = employee.JobTitle,
            Departement = employee.Departement!.Value,
            PhoneNumber = employee.PhoneNumber ?? string.Empty,
            ManagerId = employee.ManagerId,
			Role = employee.UserAccount.Role.Value,
            PhotoUrl = employee.PhotoFileName == null
                ? null
                : $"{Request.Scheme}://{Request.Host}/photos/{employee.PhotoFileName}",
        };

        return Ok(employeeDetails);
    }

	[HttpPatch("employees/{id:int}")]
	public async Task<IActionResult> UpdateEmployee(
		int id,
		[FromForm] UpdateEmployeeRequest request,
		IFormFile? photo)
	{
		var employee = await _db.Employees
			.Include(e => e.UserAccount)
			.FirstOrDefaultAsync(e => e.Id == id);

		if (employee is null)
			return NotFound();

		if (employee.UserAccount is null || request.Role is null)
			return BadRequest(new { message = "Rôle manquant." });

		if (photo is not null && photo.Length > 0)
		{
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();

            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Format d'image non supporté (jpeg, png, webp)." });

            if (photo.Length > 2 * 1024 * 1024)
                return BadRequest(new { message = "L'image ne doit pas dépasser 2 Mo." });

            var photoFileName = $"{Guid.NewGuid()}{ext}";
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var folder = Path.Combine(webRoot, "photos");
            Directory.CreateDirectory(folder);

            await using var stream = System.IO.File.Create(Path.Combine(folder, photoFileName));
            await photo.CopyToAsync(stream);

			if (!string.IsNullOrEmpty(employee.PhotoFileName))
			{
				var oldPath = Path.Combine(folder, employee.PhotoFileName);
				if (System.IO.File.Exists(oldPath))
					System.IO.File.Delete(oldPath);
			}

			employee.PhotoFileName = photoFileName;
        }

		employee.FirstName = request.FirstName.Trim();
		employee.LastName = request.LastName.Trim();
		employee.JobTitle = request.JobTitle;
		employee.Departement = request.Departement;
		employee.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber;
		employee.ManagerId = request.ManagerId > 0 ? request.ManagerId : null;
		employee.UserAccount.Role = request.Role;

		await _db.SaveChangesAsync();

		return NoContent();
	}

    [HttpGet("accounts")]
	public async Task<ActionResult<List<AccountListItemResponse>>> ListAccounts()
	{
		var accounts = await _db.UserAccounts
			.AsNoTracking()
			.Include(u => u.Employee)
			.OrderBy(u => u.Employee.LastName)
			.ThenBy(u => u.Employee.FirstName)
			.Select(u => new AccountListItemResponse
			{
				UserAccountId = u.Id,
				EmployeeId = u.EmployeeId,
				Email = u.Email,
				FirstName = u.Employee.FirstName,
				LastName = u.Employee.LastName,
				Role = u.Role!.Value,
				Departement = u.Employee.Departement,
				AccountIsActive = u.IsActive,
				EmployeeIsActive = u.Employee.IsActive,
				MustChangePassword = u.MustChangePassword,
				CreatedAt = u.CreatedAt
			})
			.ToListAsync();

		return Ok(accounts);
	}

	[HttpPatch("accounts/{id:int}")]
	public async Task<IActionResult> PatchAccount(int id, UpdateAccountStatusRequest request)
	{
		var account = await _db.UserAccounts.Include(u => u.Employee).FirstOrDefaultAsync(u => u.Id == id);

		if (account is null)
			return NotFound();

        var userAccountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userAccountIdClaim is null || !int.TryParse(userAccountIdClaim, out var userAccountId))
            return Unauthorized();

		if (!request.IsActive && id == userAccountId)
			return BadRequest();

		account.IsActive = request.IsActive;
		account.Employee!.IsActive = request.IsActive;

		await _db.SaveChangesAsync();

		return Ok();
    }

	[HttpPost("accounts/{id:int}/reset-password")]
	public async Task<ActionResult<object>> ResetPassword(int id)
	{
		var account = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Id == id);

		if (account is null)
			return NotFound();

        var userAccountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userAccountIdClaim is null || !int.TryParse(userAccountIdClaim, out var userAccountId))
            return Unauthorized();

		if (id == userAccountId)
			return BadRequest(new { message = "Vous ne pouvez pas réinitialiser votre propre mot de passe" });

        var temp = PasswordService.GenerateTemporaryPassword();
		account.PasswordHash = _passwords.Hash(temp);
		account.MustChangePassword = true;

		await _db.SaveChangesAsync();

		return Ok(new { temporaryPassword = temp });
    }

}

