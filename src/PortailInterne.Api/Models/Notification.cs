namespace PortailInterne.Api.Models;

public enum NotificationType
{
	MeetingInvitation = 0,
	MeetingUpdated = 1,
	MeetingCancelled = 2,
	MeetingConflict = 3,
	FlexibleBlockOverlapped = 4,
	AbsenceOverlapsMeeting = 5
}

public class Notification
{
	public int Id { get; set; }
	public int RecipientId { get; set; }
	public Employee Recipient { get; set; } = null!;
	public NotificationType Type { get; set; }
	public string Message { get; set; } = string.Empty;
	public string? Link { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime? ReadAt { get; set; }
}