using System.ComponentModel.DataAnnotations;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Dtos;

public class SaveAdjustmentRequest
{
	[Required(ErrorMessage = "La date de l'ajustement d'horaire est obligatoire.")]
	public DateOnly? Date { get; set; }
	
	public TimeOnly? StartTime { get; set; }
	public TimeOnly? EndTime { get; set; }
	public int BreakMinutes { get; set; }

	[MaxLength(500, ErrorMessage = "Le nom doit avoir 500 caractères au maximum.")]
	public string? Reason { get; set; }
}

public class AdjustmentResponse
{
	public int Id { get; set; }
	public DateOnly Date { get; set; }
	public TimeOnly? StartTime { get; set; }
	public TimeOnly? EndTime { get; set; }
	public int BreakMinutes { get; set; }
	public string? Reason { get; set; } = string.Empty;
	public LeaveStatus Status { get; set; }
	public string CreatedByFirstName { get; set; } = string.Empty;
	public string CreatedByLastName { get; set; } = string.Empty;
	public bool CanCancel { get; set; }
}