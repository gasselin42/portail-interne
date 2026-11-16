namespace PortailInterne.Api.Services;

public static class BirthdayRules
{
	public static string? Validate(int? month, int? day)
	{
		if (month is null && day is null)
			return null;

		if (month is null || day is null)
			return "Indique le mois et le jour, ou ni l'un ni l'autre.";

		if (month < 1 || month > 12)
			return "Date d'anniversaire invalide.";

		if (day < 1 || day > 31)
			return "Date d'anniversaire invalide.";

		if (DateTime.DaysInMonth(2024, month.Value) < day)
			return "Date d'anniversaire invalide.";

		return null;
	}

	public static bool OccursOn(int month, int day, DateOnly date)
	{
		var isLeapYear = DateTime.IsLeapYear(date.Year);
		if (day == 29 && month == 2 && date.Day == 28 && date.Month == 2 && !isLeapYear)
			return true;

		return (month == date.Month && day == date.Day);
	}
}