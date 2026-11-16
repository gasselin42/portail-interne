namespace PortailInterne.Api.Models;

public static class LanguageLabels
{
	public static readonly IReadOnlyDictionary<string, string> All = new Dictionary<string, string>
	{
		["fr"] = "Français",
		["en"] = "Anglais",
        ["es"] = "Espagnol",
        ["pt"] = "Portugais",
        ["it"] = "Italien",
        ["de"] = "Allemand",
        ["ar"] = "Arabe",
        ["zh"] = "Chinois",
        ["ja"] = "Japonais",
        ["ru"] = "Russe",
        ["vi"] = "Vietnamien",
        ["ht"] = "Créole haïtien",
	};

	public static (List<string> Codes, string? Error) Normalize(IEnumerable<string>? codes)
	{
		if (codes is null || !codes.Any())
			return ([], null);

		var cleanCodes = codes
			.Where(c => !string.IsNullOrWhiteSpace(c))
			.Select(c => c.Trim().ToLowerInvariant())
			.Distinct()
			.ToList();

		var unknown = cleanCodes.FirstOrDefault(c => !All.ContainsKey(c));

		if (unknown != null)
			return ([], $"Langue inconnue : {unknown}.");
		
		return (cleanCodes, null);
	}
}
