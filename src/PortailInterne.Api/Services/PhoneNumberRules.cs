using PhoneNumbers;

namespace PortailInterne.Api.Services;

public static class PhoneNumberRules
{
	public static (string? Number, string? Error) Normalize(string? input, string countryCode)
	{
		var phoneUtil = PhoneNumberUtil.GetInstance();
		PhoneNumber number;

		if (string.IsNullOrWhiteSpace(input))
			return (null, null);
		
		try
		{
			number = phoneUtil.Parse(input, countryCode);
		}
		catch (NumberParseException)
		{
			return (null, "Numéro de téléphone invalide pour ce pays.");
		}

		if (!phoneUtil.IsValidNumber(number))
			return (null, "Numéro de téléphone invalide pour ce pays.");

		return (phoneUtil.Format(number, PhoneNumberFormat.E164), null);
	}
}