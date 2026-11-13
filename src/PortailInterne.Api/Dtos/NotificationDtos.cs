using PortailInterne.Api.Models;

namespace PortailInterne.Api.Dtos;

public class NotificationResponse
{
	public int Id { get; set; }
	public NotificationType Type { get; set; }
	public string Message { get; set; } = string.Empty;
	public string? Link { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? ReadAt { get; set; }
}

public class NotificationListResponse
{
	public List<NotificationResponse> Items { get; set; } = [];
	public bool HasMore { get; set; }
}