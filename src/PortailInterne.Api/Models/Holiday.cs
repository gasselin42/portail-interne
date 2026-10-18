using System.ComponentModel.DataAnnotations;

namespace PortailInterne.Api.Models;

public class Holiday
{
	public int Id { get; set; }
	public int SiteId { get; set; }
	public Site Site { get; set; } = null!;
	public DateOnly Date { get; set; }
	[Required] public string Name { get; set; } = string.Empty;
}