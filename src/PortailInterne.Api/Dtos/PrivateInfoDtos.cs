namespace PortailInterne.Api.Dtos;

public class EmergencyContactResponse
{
	public string Name { get; set; } = string.Empty;
	public string Relationship { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public int SortOrder { get; set; }
}

public class PrivateDetailsResponse
{
	public string? PersonalMobilePhone { get; set; }
	public string? PersonalEmail { get; set; }
	public string? AddressLine1 { get; set; }
	public string? AddressLine2 { get; set; }
	public string? City { get; set; }
	public string? Region { get; set; }
	public string? PostalCode { get; set; }
	public string? Country { get; set; }
	public int? BirthMonth { get; set; }
	public int? BirthDay { get; set; }
	public bool ShowBirthday { get; set; }
}

public class PrivateInfoResponse
{
	public PrivateDetailsResponse? Details { get; set; }
	public List<EmergencyContactResponse> EmergencyContacts { get; set; } = [];
}