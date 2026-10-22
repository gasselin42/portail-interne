using System.ComponentModel.DataAnnotations;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Dtos;

public class SaveAbsenceRequest
{
	[Required(ErrorMessage = "La date d'une absence est obligatoire.")]
	public DateOnly? Date { get; set; }

	[Required(ErrorMessage = "L'heure de début d'une absence est obligatoire.")]
	public TimeOnly? PlannedStart { get; set; }

	[Required(ErrorMessage = "L'heure de fin d'une absence est obligatoire.")]
	public TimeOnly? PlannedEnd { get; set; }

	[MaxLength(500, ErrorMessage = "Le motif doit avoir 500 caractères au maximum.")]
	public string? Reason { get; set; }
}

public class DepartRequest
{
	[MaxLength(500, ErrorMessage = "Le motif doit avoir 500 caractères au maximum.")]
	public string? Reason { get; set; }
}

public class AbsenceResponse
{
	public int Id { get; set; }
	public DateOnly Date { get; set; }
	public TimeOnly? PlannedStart { get; set; }
	public TimeOnly? PlannedEnd { get; set; }
	public string? Reason { get; set; }
	public LeaveStatus Status { get; set; }
	public TimeOnly Start { get; set; }
	public TimeOnly? End { get; set; }
	public bool InProgress { get; set; }
	public bool ReturnConfirmed { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public int? CreatedById { get; set; }
	public string? CreatedByFirstName { get; set; }
	public string? CreatedByLastName { get; set; }
	public bool CanCancel { get; set; }
}
