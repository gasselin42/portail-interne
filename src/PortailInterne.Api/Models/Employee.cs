using System.ComponentModel.DataAnnotations;

namespace PortailInterne.Api.Models;

public enum Departement
{
    Direction,
    Ventes,
    RH,
    TI,
}

public class Employee
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le prénom est obligatoire.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le nom de famille est obligatoire.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'email est obligatoire.")]
    public string Email { get; set; } = string.Empty;

    public string JobTitle { get; set; } = string.Empty;

    public Departement Departement { get; set; }

    public string? PhoneNumber { get; set; }

    public int? ManagerId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public string? PhotoFileName { get; set; }

    public UserAccount? UserAccount { get; set; }
}
