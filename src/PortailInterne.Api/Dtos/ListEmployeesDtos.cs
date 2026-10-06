using PortailInterne.Api.Models;

namespace PortailInterne.Api.Dtos;

public class EmployeeListItemResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public Departement Departement { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
}

public class EmployeeDetailResponse : EmployeeListItemResponse
{
    public int? ManagerId { get; set; }
    public string? ManagerFirstName { get; set; }
    public string? ManagerLastName { get; set; }
}

public class AdminEmployeeResponse : EmployeeDetailResponse
{
    public Role Role { get; set; }
}

public class EmployeeLookupItem
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public Departement Departement { get; set; }
}

public class EmployeeLookupResponse
{
    public List<EmployeeLookupItem> Items { get; set; } = new();
    public bool HasMore { get; set; }
}

