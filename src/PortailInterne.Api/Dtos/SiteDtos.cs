using System.ComponentModel.DataAnnotations;

namespace PortailInterne.Api.Dtos;

public class SaveSiteRequest
{
	[Required(ErrorMessage = "Le nom du site est obligatoire.")]
	[MaxLength(100, ErrorMessage = "Le nom du site doit avoir 100 caractères au maximum.")]
	public string Name { get; set; } = string.Empty;

	[Required(ErrorMessage = "Le fuseau horaire est obligatoire.")]
	public string TimeZoneId { get; set; } = string.Empty;
}