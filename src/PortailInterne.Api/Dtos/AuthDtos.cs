using System.ComponentModel.DataAnnotations;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Dtos;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int UserAccountId { get; set; }
    public int EmployeeId { get; set; }
    public Role? Role { get; set; }
    public bool MustChangePassword { get; set; }
}

public class MeResponse
{
    public int EmployeeId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
    public Departement Departement { get; set; }
    public Role Role { get; set; }
    public bool IsManager { get; set; }
    public bool MustChangePassword { get; set; }
    public bool IsActive { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string ActualPassword { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, MinLength(8)]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
