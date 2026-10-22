using Microsoft.EntityFrameworkCore;
using PortailInterne.Api.Data;
using PortailInterne.Api.Dtos;
using PortailInterne.Api.Models;

namespace PortailInterne.Api.Services;

public class LeaveUsageService
{
	private readonly AppDbContext _db;
	private readonly EffectiveScheduleService _schedule;

	public LeaveUsageService(AppDbContext db, EffectiveScheduleService schedule)
	{
		_db = db;
		_schedule = schedule;
	}

	public async Task<Dictionary<int, LeaveBalance.LeaveUsage>> UsageForAsync(IEnumerable<int> employeeIds)
	{
		var annualDays = await _db.Employees
			.Where(e => employeeIds.Contains(e.Id))
			.ToDictionaryAsync(e => e.Id, e => e.AnnualLeaveDays);

		var balanceLeaves = await LoadBalanceLeavesAsync(employeeIds);

		Dictionary<int, LeaveBalance.LeaveUsage> result = [];

		foreach (var (employeeId, days) in annualDays)
		{
			var leaves = balanceLeaves.GetValueOrDefault(employeeId, []);
			if (leaves is null || leaves.Count == 0)
				continue;
			
			var minDate = DateOnly.FromDateTime(leaves.Min(l => l.StartDate));
			var maxDate = DateOnly.FromDateTime(leaves.Max(l => l.EndDate));
			var worked = await _schedule.WorkedDaysAsync(employeeId, minDate, maxDate);

			foreach (var (leaveId, usage) in LeaveBalance.ComputeUsage(days, leaves, worked.Contains))
				result.Add(leaveId, usage);
		}

		return result;
	}

	/// <summary>Complète WorkingDays et UnpaidDates des congés qui consomment le solde.</summary>
	public async Task AddUsageAsync(List<LeaveRequestResponse> leaves)
	{
		var employeeIds = leaves.Select(l => l.EmployeeId).Distinct();
		var usage = await UsageForAsync(employeeIds);

		foreach (var leave in leaves)
		{
			if (usage.TryGetValue(leave.Id, out var u))
			{
				leave.WorkingDays = u.WorkingDays;
				leave.UnpaidDates = u.UnpaidDates;
				leave.WorkedDates = u.WorkedDates;
			}
		}
	}

	/// <summary>Congés qui consomment le solde (Congé, en attente ou approuvé), regroupés par employé.</summary>
	public async Task<Dictionary<int, List<LeaveBalance.BalanceLeave>>> LoadBalanceLeavesAsync(IEnumerable<int> employeeIds)
	{
		return (await _db.LeaveRequests
				.AsNoTracking()
				.Where(l => employeeIds.Contains(l.EmployeeId)
						&& l.Type == LeaveType.Conge
						&& (l.Status == LeaveStatus.EnAttente || l.Status == LeaveStatus.Approuve))
				.ToListAsync())
			.GroupBy(l => l.EmployeeId)
			.ToDictionary(
				g => g.Key,
				g => g.Select(l => new LeaveBalance.BalanceLeave(l.Id, l.StartDate, l.EndDate, l.CreatedAt)).ToList()
			);
	}
}