using System.ComponentModel.DataAnnotations;

namespace PortailInterne.Api.Dtos;

public class EmergencyContactRequest
{
	[Required(ErrorMessage = "Le nom du contact d'urgence est obligatoire.")]
	[MaxLength(100, ErrorMessage = "Le nom du contact d'urgence doit avoir 100 caractères au maximum.")]
	public string Name { get; set; } = string.Empty;

	[Required(ErrorMessage = "La relation avec le contact d'urgence est obligatoire.")]
	[MaxLength(50, ErrorMessage = "La relation avec le contact d'urgence doit avoir 50 caractères au maximum.")]
	public string Relationship { get; set; } = string.Empty;

	[Required(ErrorMessage = "Le numéro de téléphone du contact d'urgence est obligatoire.")]
	[MaxLength(20, ErrorMessage = "Le numéro de téléphone du contact d'urgence doit avoir 20 caractères au maximum.")]
	public string PhoneNumber { get; set; } = string.Empty;
}

public class EmergencyContactResponse
{
	public string Name { get; set; } = string.Empty;
	public string Relationship { get; set; } = string.Empty;
	public string PhoneNumber { get; set; } = string.Empty;
	public int SortOrder { get; set; }
}

public class PrivateDetailsRequest
{
	[MaxLength(20, ErrorMessage = "Le cellulaire personnel doit avoir 20 caractères au maximum.")]
	public string? PersonalMobilePhone { get; set; }
	
	[MaxLength(254, ErrorMessage = "L'email personnel doit avoir 254 caractères au maximum.")]
	public string? PersonalEmail { get; set; }

	[MaxLength(100, ErrorMessage = "La ligne 1 de l'adresse doit avoir 100 caractères au maximum.")]
	public string? AddressLine1 { get; set; }

	[MaxLength(100, ErrorMessage = "La ligne 2 de l'adresse doit avoir 100 caractères au maximum.")]
	public string? AddressLine2 { get; set; }

	[MaxLength(100, ErrorMessage = "La ville doit avoir 100 caractères au maximum.")]
	public string? City { get; set; }

	[MaxLength(100, ErrorMessage = "La région doit avoir 100 caractères au maximum.")]
	public string? Region { get; set; }

	[MaxLength(10, ErrorMessage = "Le code postal doit avoir 10 caractères au maximum.")]
	public string? PostalCode { get; set; }

	[MaxLength(2, ErrorMessage = "Le code du pays doit avoir 2 caractères au maximum.")]
	public string? Country { get; set; }

	public int? BirthMonth { get; set; }
	public int? BirthDay { get; set; }
	public bool ShowBirthday { get; set; }
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

public class UpdateProfileRequest
{
	[MaxLength(100, ErrorMessage = "Le prénom d'usage doit avoir 100 caractères au maximum.")]
	public string? PreferredName { get; set; }

	public List<string> Languages { get; set; } = [];

	[Required]
	public PrivateDetailsRequest? Details { get; set; }

	[MaxLength(2, ErrorMessage = "Seulement 2 contacts d'urgence sont permis.")]
	public List<EmergencyContactRequest> EmergencyContacts { get; set; } = [];
}