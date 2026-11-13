using System.ComponentModel.DataAnnotations;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Dtos;

public class SaveSiteRequest
{
	[Required(ErrorMessage = "Le nom du site est obligatoire.")]
	[MaxLength(100, ErrorMessage = "Le nom du site doit avoir 100 caractères au maximum.")]
	public string Name { get; set; } = string.Empty;

	[Required(ErrorMessage = "Le fuseau horaire est obligatoire.")]
	public string TimeZoneId { get; set; } = string.Empty;

	[Required(ErrorMessage = "Le code du pays est obligatoire.")]
	[MaxLength(2, ErrorMessage = "Le code du pays doit avoir 2 caractères au maximum.")]
	public string CountryCode { get; set; } = string.Empty;

	[MaxLength(20, ErrorMessage = "Le numéro de téléphone du site doit avoir 20 caractères au maximum.")]
	public string? MainPhoneNumber { get; set; }
}

public class SaveTeamVisibilityRequest
{
	public TeamVisibility TeamVisibility { get; set; }
}