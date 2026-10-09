using System.ComponentModel.DataAnnotations;

namespace PortailInterne.Api.Models;

public enum Departement
{
    Direction,
    Ventes,
    RH,
    TI,
}

public static class DepartementLabels
{
	// Renommer dans admin.ts en cas de changement
	public static readonly IReadOnlyDictionary<Departement, string> All = new Dictionary<Departement, string>
	{
		[Departement.Direction] = "Direction",
		[Departement.Ventes] = "Ventes",
        [Departement.RH] = "Ressources humaines",
        [Departement.TI] = "Technologies de l'information",	
	};
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

	/// Clés de tri et de recherche (voir TextKey).
	/// Calculées automatiquement à l'enregistrement :
	/// ne jamais les modifier à la main.
	public string FirstNameKey { get; set; } = string.Empty;
	public string LastNameKey { get; set; } = string.Empty;
	public string JobTitleKey { get; set; } = string.Empty;
}
