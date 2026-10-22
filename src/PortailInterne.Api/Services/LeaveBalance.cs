namespace PortailInterne.Api.Services;

public static class LeaveBalance
{
	/// <summary>Un congé qui consomme le solde (type Congé, en attente ou approuvé).</summary>
	public record BalanceLeave(int Id, DateTime StartDate, DateTime EndDate, DateTime CreatedAt);

	/// <summary>Ce qu'un congé consomme : ses jours ouvrables, et ceux qui sont sans solde.</summary>
	public record LeaveUsage(List<DateOnly> WorkedDates, List<DateOnly> UnpaidDates)
	{
		public int WorkingDays => WorkedDates.Count;
	}

	public static Dictionary<int, LeaveUsage> ComputeUsage(int annualDays, IEnumerable<BalanceLeave> leaves, Func<DateOnly, bool> isWorkedDay)
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
			List<DateOnly> workedDates = [];

			for (var day = DateOnly.FromDateTime(leave.StartDate); day <= end; day = day.AddDays(1))
			{
				if (!isWorkedDay(day))
					continue;

				workedDates.Add(day);
				
				remaining.TryAdd(day.Year, annualDays);
								
				if (remaining[day.Year] > 0)
					remaining[day.Year]--;
				else
					unpaidDates.Add(day);
			}

			result.Add(leave.Id, new(workedDates, unpaidDates));
		}

		return result;
	}
}