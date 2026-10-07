using System.ComponentModel.DataAnnotations;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Dtos;

public class AccountListItemResponse
{
    public int UserAccountId { get; set; }
    public int EmployeeId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Role Role { get; set; }
    public Departement Departement { get; set; }
    public bool AccountIsActive { get; set; }
    public bool EmployeeIsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateAccountStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}

