using System.ComponentModel.DataAnnotations;

namespace PortailInterne.Api.Models;

public enum TeamVisibility
{
	Team = 0,
	Site = 1,
	Nobody = 2
}

public class Site
{
	public int Id { get; set; }

	[Required]
	public string Name { get; set; } = string.Empty;

	[Required]
	public string TimeZoneId { get; set; } = string.Empty;

	public TeamVisibility TeamVisibility { get; set; }
}