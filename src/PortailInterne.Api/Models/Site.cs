using System.ComponentModel.DataAnnotations;

namespace PortailInterne.Api.Models;

public class Site
{
	public int Id { get; set; }

	[Required]
	public string Name { get; set; } = string.Empty;

	[Required]
	public string TimeZoneId { get; set; } = string.Empty;
}