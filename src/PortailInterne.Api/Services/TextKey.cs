using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PortailInterne.Api.Services;

public static class TextKey
{
	private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

	private static readonly (string From, string To)[] Ligatures =
	[
		("œ", "oe"),
		("æ", "ae"),
		("ß", "ss"),
	];

	public static string From(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return "";

		var text = value.ToLowerInvariant();
		
		foreach (var (from, to) in Ligatures)
			text = text.Replace(from, to);

		var decomposed = text.Normalize(NormalizationForm.FormD);
		var builder = new StringBuilder(decomposed.Length);

		foreach (var c in decomposed)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
				builder.Append(c);
		}

		text = builder.ToString().Normalize(NormalizationForm.FormC);

		text = text
			.Replace("\u2019", "'")   // ’ apostrophe typographique
			.Replace("\u2018", "'")   // ‘ guillemet simple ouvrant
			.Replace("\u02BC", "'");  // ʼ lettre apostrophe
		
		text = text.Replace("-", " ");

		text = Spaces.Replace(text, " ").Trim();

		return text;
	}
}