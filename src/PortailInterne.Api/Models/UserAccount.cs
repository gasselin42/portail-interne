using System.ComponentModel.DataAnnotations;

namespace PortailInterne.Api.Models;

public enum Role
{
    Admin,
    Employee
}

public static class RoleLabels
{
	// Renommer dans admin.ts en cas de changement
	public static readonly IReadOnlyDictionary<Role, string> All = new Dictionary<Role, string>
	{
		[Role.Admin] = "Admin",
		[Role.Employee] = "Employé",
	};
}

public class UserAccount
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee Employee { get; set; } = null!;  // navigation

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public Role? Role { get; set; }

    public bool MustChangePassword { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
}