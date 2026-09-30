using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Services;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordService _passwords;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, PasswordService passwords, IConfiguration config)
    {
        _db = db;
        _passwords = passwords;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var account = await _db.UserAccounts
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (account is null || !account.IsActive || account.Employee is null || !account.Employee.IsActive || account.Role is null)
            return Unauthorized();

        if (!_passwords.Verify(account.PasswordHash, request.Password))
            return Unauthorized();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Email, account.Email),
            new(ClaimTypes.Role, account.Role.Value.ToString()),
            new("employeeId", account.EmployeeId.ToString()),
            new("mustChangePassword", account.MustChangePassword.ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresMinutes = int.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new LoginResponse
        {
            Token = tokenString,
            Email = account.Email,
            UserAccountId = account.Id,
            EmployeeId = account.EmployeeId,
            Role = account.Role,
            MustChangePassword = account.MustChangePassword
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var userAccountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userAccountIdClaim is null || !int.TryParse(userAccountIdClaim, out var userAccountId))
            return Unauthorized();

        var account = await _db.UserAccounts
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Id == userAccountId);

        if (account is null || !account.IsActive || account.Employee is null || !account.Employee.IsActive || account.Role is null)
            return Unauthorized();

        var isManager = await _db.Employees.AnyAsync(e => e.ManagerId == account.EmployeeId);

        return Ok(new MeResponse
        {
            EmployeeId = account.EmployeeId,
            FirstName = account.Employee.FirstName,
            LastName = account.Employee.LastName,
            Email = account.Email,
            JobTitle = account.Employee.JobTitle,
            PhoneNumber = account.Employee.PhoneNumber,
            Departement = account.Employee.Departement,
            Role = account.Role.Value,
            IsManager = isManager,
            MustChangePassword = account.MustChangePassword,
            IsActive = account.IsActive && account.Employee.IsActive
        });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userAccountIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userAccountIdClaim is null || !int.TryParse(userAccountIdClaim, out var userAccountId))
            return Unauthorized();

        var account = await _db.UserAccounts.FirstOrDefaultAsync(u => u.Id == userAccountId);

        if (account is null || !account.IsActive)
            return Unauthorized();

        if (!_passwords.Verify(account.PasswordHash, request.ActualPassword))
            return Unauthorized();

        if (request.NewPassword == request.ActualPassword)
            return BadRequest();

        string newHash = _passwords.Hash(request.NewPassword);
        account.PasswordHash = newHash;
        account.MustChangePassword = false;

        await _db.SaveChangesAsync();

        return Ok();
    }
}

