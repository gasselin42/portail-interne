using System.ComponentModel.DataAnnotations;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Dtos;

public class CreateEmployeeRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public Departement? Departement { get; set; }

    [Required]
    public Role? Role { get; set; }

    public string? JobTitle { get; set; }

    public string? PhoneNumber { get; set; }

    public int? ManagerId { get; set; }
}

public class UpdateEmployeeRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public Departement? Departement { get; set; }

    [Required]
    public Role? Role { get; set; }

    public string? JobTitle { get; set; }

    public string? PhoneNumber { get; set; }

    public int? ManagerId { get; set; }
}

public class CreateEmployeeResponse
{
    public int EmployeeId { get; set; }
    public int UserAccountId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool MustChangePassword { get; set; } = true;
    public string TemporaryPassword { get; set; } = string.Empty;
}

