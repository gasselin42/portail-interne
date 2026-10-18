using System.ComponentModel.DataAnnotations;

namespace PortailInterne.Api.Dtos;

public class SaveHolidayRequest
{
	[Required(ErrorMessage = "La date du jour férié est obligatoire.")]
	public DateOnly? Date { get; set; }

	[Required(ErrorMessage = "Le nom du jour férié est obligatoire.")]
	[MaxLength(100, ErrorMessage = "Le nom doit avoir 100 caractères au maximum.")]
	public string Name { get; set; } = string.Empty;
}

public class HolidayResponse
{
	public int Id { get; set; }
	public DateOnly Date { get; set; }
	public string Name { get; set; } = string.Empty;
	public bool IsEditable { get; set; }
}