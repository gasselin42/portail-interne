using System.Text.RegularExpressions;

namespace PortailInterne.Api.Services;

public static class PostalCodeRules
{
	private static readonly IReadOnlyDictionary<string, (Regex Pattern, string Separator)> Patterns = new Dictionary<string, (Regex, string)>
	{
		["CA"] = (new Regex("^([ABCEGHJ-NPRSTVXY][0-9][ABCEGHJ-NPRSTV-Z]) ?([0-9][ABCEGHJ-NPRSTV-Z][0-9])$"), " "),
		["US"] = (new Regex("^([0-9]{5})(?:[- ]?([0-9]{4}))?$"), "-"),
		["FR"] = (new Regex("^([0-9]{5})$"), "")
	};

	public static (string? PostalCode, string? Error) Normalize(string? input, string countryCode)
	{
		if (string.IsNullOrWhiteSpace(input))
			return (null, null);

		string code = input.Trim().ToUpperInvariant();
		if (Patterns.TryGetValue(countryCode, out var pattern) == false)
			return (code, null);

		var match = pattern.Pattern.Match(code);

		if (!match.Success)
			return (null, "Code postal invalide pour ce pays.");
		
		if (!match.Groups[2].Success)
			return (match.Groups[1].Value, null);
		else
			return (match.Groups[1].Value + pattern.Separator + match.Groups[2].Value, null);
	}

}