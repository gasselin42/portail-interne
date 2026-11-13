using System.ComponentModel.DataAnnotations;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Dtos;

public enum OutsideHoursReason
{
	NotWorking,    // jour non travaillé (horaire ou ajustement sans heures)
	Holiday,       // férié du site
	OutsideHours,  // jour travaillé, mais le meeting dépasse les heures
	Absence,       // une absence approuvée chevauche le meeting
}

public class MeetingFormRequest
{
	[Required(ErrorMessage = "Le titre est obligatoire.")]
	[MaxLength(200, ErrorMessage = "Le titre doit avoir 200 caractères au maximum.")]
	public string Title { get; set; } = string.Empty;

	[MaxLength(2000, ErrorMessage = "La description doit avoir 2000 caractères au maximum.")]
	public string? Description { get; set; }

	[Required(ErrorMessage = "La date et l'heure du début sont obligatoires.")]
	public DateTime? StartAt { get; set; }

	[Required(ErrorMessage = "La date et l'heure de la fin sont obligatoires.")]
	public DateTime? EndAt { get; set; }

	public bool IsFlexible { get; set; }

	public List<int> ParticipantIds { get; set; } = [];
	public bool ConfirmConflicts { get; set; }
}

public class CreateMeetingRequest : MeetingFormRequest
{
	[Required(ErrorMessage = "Le type est obligatoire.")]
	public MeetingKind? Kind { get; set; }
	public string? Color { get; set; }
}

public class UpdateMeetingRequest : MeetingFormRequest { }

public class SaveMeetingColorRequest
{
	public string? Color { get; set; }
}

public class SaveMeetingResponseRequest
{
	public ParticipantResponse Response { get; set; }
}

public class MeetingResponse
{
	public int Id { get; set; }
	public MeetingKind Kind { get; set; }
	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }
	public DateTime StartAt { get; set; }
	public DateTime EndAt { get; set; }
	public int OrganizerId { get; set; }
	public bool IsTeamMeeting { get; set; }
	public bool IsFlexible { get; set; }
	public string Color { get; set; } = string.Empty;
	public ParticipantResponse? MyResponse { get; set; }
}

public class MeetingDetailResponse
{
	public MeetingResponse Meeting { get; set; } = null!;
	public MeetingPerson Organizer { get; set; } = null!;
	public List<MeetingPerson> Participants { get; set; } = [];
	public bool CanEdit { get; set; }
}

public class MeetingPerson
{
	public int EmployeeId { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public ParticipantResponse Response { get; set; }
}

public class MeetingConflict
{
	public int EmployeeId { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public bool WithTeamMeeting { get; set; }
}

public class MeetingOutsideHours
{
	public int EmployeeId { get; set; }
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public OutsideHoursReason Reason { get; set; }
}

public class MeetingConflictResponse
{
	public string Message { get; set; } = string.Empty;
	public List<MeetingConflict> Conflicts { get; set; } = [];
	public List<MeetingOutsideHours> OutsideHours { get; set; } = [];
}
