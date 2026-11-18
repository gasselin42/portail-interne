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
	
	[MaxLength(100, ErrorMessage = "L'emplacement du bureau doit avoir 100 caractères au maximum.")]
	public string? OfficeLocation { get; set; }

    [Required]
    public Role? Role { get; set; }

    public string? JobTitle { get; set; }

    public string? WorkPhone { get; set; }
	public string? PhoneExtension { get; set; }

    public int? ManagerId { get; set; }

	[Required(ErrorMessage = "Le nombre de jours de congé est obligatoire.")]
	[Range(0, 60, ErrorMessage = "Le nombre de jours de congé doit être entre 0 et 60.")]
	public int? AnnualLeaveDays { get; set; }

	[Required(ErrorMessage = "Le site est obligatoire.")]
	public int? SiteId { get; set; }

	[Required(ErrorMessage = "La date d'embauche est obligatoire.")]
	public DateOnly? HireDate { get; set; }

	public UpdateProfileRequest? Profile { get; set; }
}

public class UpdateEmployeeRequest
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public Departement? Departement { get; set; }

	[MaxLength(100, ErrorMessage = "L'emplacement du bureau doit avoir 100 caractères au maximum.")]
	public string? OfficeLocation { get; set; }

    [Required]
    public Role? Role { get; set; }

    public string? JobTitle { get; set; }

    public string? WorkPhone { get; set; }
	public string? PhoneExtension { get; set; }

    public int? ManagerId { get; set; }

	[Required(ErrorMessage = "Le nombre de jours de congé est obligatoire.")]
	[Range(0, 60, ErrorMessage = "Le nombre de jours de congé doit être entre 0 et 60.")]
	public int? AnnualLeaveDays { get; set; }

	[Required(ErrorMessage = "Le site est obligatoire.")]
	public int? SiteId { get; set; }

	[Required(ErrorMessage = "La date d'embauche est obligatoire.")]
	public DateOnly? HireDate { get; set; }
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

