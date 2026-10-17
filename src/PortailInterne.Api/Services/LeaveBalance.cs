namespace PortailInterne.Api.Services;

public static class LeaveBalance
{
	/// <summary>Un congé qui consomme le solde (type Congé, en attente ou approuvé).</summary>
	public record BalanceLeave(int Id, DateTime StartDate, DateTime EndDate, DateTime CreatedAt);

	/// <summary>Ce qu'un congé consomme : ses jours ouvrables, et ceux qui sont sans solde.</summary>
	public record LeaveUsage(int WorkingDays, List<DateOnly> UnpaidDates);

	public static Dictionary<int, LeaveUsage> ComputeUsage(int annualDays, IEnumerable<BalanceLeave> leaves)
	{
		var remaining = new Dictionary<int, int>(); // année → jours restants
		var result = new Dictionary<int, LeaveUsage>();

		var sortedLeaves = leaves
			.OrderBy((l) => l.CreatedAt)
			.ThenBy((l) => l.Id)
			.ToList();

		foreach (BalanceLeave leave in sortedLeaves)
		{
			var end = DateOnly.FromDateTime(leave.EndDate);
			var unpaidDates = new List<DateOnly>();
			int workingDays = 0;

			for (var day = DateOnly.FromDateTime(leave.StartDate); day <= end; day = day.AddDays(1))
			{
				if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
					continue;

				workingDays++;
				
				remaining.TryAdd(day.Year, annualDays);
								
				if (remaining[day.Year] > 0)
					remaining[day.Year]--;
				else
					unpaidDates.Add(day);
			}

			result.Add(leave.Id, new(workingDays, unpaidDates));
		}

		return result;
	}

	public static int CountWorkingDays(DateOnly from, DateOnly to, int year)
	{
		int count = 0;
		for (var day = from; day <= to; day = day.AddDays(1))
		{
			if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && day.Year == year)
				count++;
		}
		return count;
	}
}