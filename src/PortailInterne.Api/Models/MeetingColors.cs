namespace PortailInterne.Api.Models;

public static class MeetingColors
{
	public const string Default = "blue";

	public static IReadOnlySet<string> All { get; } = new HashSet<string>
	{
		"blue",
		"indigo",
		"cyan",
		"teal",
		"fuchsia",
		"pink",
		"orange",
		"stone"
	};
}